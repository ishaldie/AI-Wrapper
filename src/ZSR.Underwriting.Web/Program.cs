using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using MudBlazor.Services;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Parsing;
using ZSR.Underwriting.Infrastructure.Repositories;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Web.Components;
using ZSR.Underwriting.Web.Endpoints;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Add MudBlazor services
    builder.Services.AddMudServices();

    // Add EF Core with SQLite for development
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=zsr_underwriting.db"));

    // Data Protection — persist keys to filesystem with app isolation
    var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("ZSR.Underwriting");

    // Add ASP.NET Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password requirements
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;

        // Lockout policy
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Configure cookie authentication — hardened for SOC 2
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.HttpOnly = true;
    });

    // Register external OAuth providers (Google + Microsoft)
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        })
        .AddMicrosoftAccount(options =>
        {
            options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "";
            options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "";
        });

    // Require authentication on all routes by default
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

    // Add Claude client — supports "cli" mode (your subscription) or "api" mode (API key)
    builder.Services.Configure<ClaudeOptions>(
        builder.Configuration.GetSection(ClaudeOptions.SectionName));

    var claudeSection = builder.Configuration.GetSection(ClaudeOptions.SectionName);
    var claudeMode = claudeSection["Mode"];

    // Smart default: if no Mode is explicitly set, use "cli" when no API key is available
    if (string.IsNullOrWhiteSpace(claudeMode))
    {
        var configApiKey = claudeSection["ApiKey"];
        var envApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        claudeMode = !string.IsNullOrWhiteSpace(configApiKey) || !string.IsNullOrWhiteSpace(envApiKey)
            ? "api"
            : "cli";
    }

    if (claudeMode.Equals("cli", StringComparison.OrdinalIgnoreCase))
    {
        // Use Claude Code CLI — runs against your subscription, no API key needed
        builder.Services.AddSingleton<IClaudeClient, ClaudeCliClient>();
    }
    else
    {
        // Use Anthropic HTTP API — requires API key in user secrets
        builder.Services.AddHttpClient<IClaudeClient, ClaudeClient>((sp, client) =>
        {
            var options = builder.Configuration
                .GetSection(ClaudeOptions.SectionName)
                .Get<ClaudeOptions>() ?? new ClaudeOptions();
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
    }

    builder.Services.AddMemoryCache();

    // SMTP configuration for email delivery
    builder.Services.Configure<SmtpOptions>(
        builder.Configuration.GetSection(SmtpOptions.SectionName));

    // Email code service for passwordless auth
    builder.Services.AddSingleton<IEmailCodeService, EmailCodeService>();

    // Add repository layer
    builder.Services.AddScoped<IDealRepository, DealRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Add report services
    builder.Services.AddScoped<IReportAssembler, ReportAssembler>();
    builder.Services.AddSingleton<IReportPdfExporter, ReportPdfExporter>();
    builder.Services.AddScoped<IPromptBuilder, ZSR.Underwriting.Application.Services.UnderwritingPromptBuilder>();
    builder.Services.AddScoped<IReportProseGenerator, ZSR.Underwriting.Application.Services.ReportProseGenerator>();
    builder.Services.AddSingleton<ISensitivityCalculator, ZSR.Underwriting.Application.Calculations.SensitivityCalculatorService>();

    // Add market data service
    builder.Services.AddScoped<IMarketDataService, MarketDataService>();

    // Add sales comp extractor (Claude-powered structured extraction)
    builder.Services.AddScoped<ISalesCompExtractor, SalesCompExtractor>();

    // Add public data API clients (Census, BLS, FRED)
    builder.Services.AddHttpClient<CensusApiClient>(client =>
        client.BaseAddress = new Uri("https://api.census.gov/"));
    builder.Services.AddHttpClient<BlsApiClient>(client =>
        client.BaseAddress = new Uri("https://api.bls.gov/"));
    builder.Services.AddHttpClient<FredApiClient>(client =>
        client.BaseAddress = new Uri("https://api.stlouisfed.org/"));
    builder.Services.AddScoped<IPublicDataService, PublicDataService>();

    // Add HUD Income Limits API client (affordability calculator)
    builder.Services.AddHttpClient<IHudApiClient, HudApiClient>(client =>
    {
        client.BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/");
        var hudToken = builder.Configuration["HudApi:Token"];
        if (!string.IsNullOrEmpty(hudToken))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", hudToken);
    });

    // Add quick analysis service (singleton — uses IServiceScopeFactory internally)
    builder.Services.AddSingleton<IQuickAnalysisService, QuickAnalysisService>();

    // Add activity tracking (IHttpContextAccessor required for IP capture in audit events)
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IActivityTracker, ActivityTracker>();

    // Add application services
    builder.Services.AddScoped<IDealService, DealService>();
    builder.Services.AddScoped<IUserManagementService, UserManagementService>();
    builder.Services.AddScoped<ZSR.Underwriting.Domain.Interfaces.IFileStorageService>(sp =>
        new LocalFileStorageService(Path.Combine(builder.Environment.ContentRootPath, "uploads")));
    builder.Services.AddScoped<IFileContentValidator, FileContentValidator>();
    builder.Services.AddScoped<IVirusScanService, WindowsDefenderScanService>();
    builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();
    builder.Services.AddSingleton<IDocumentMatchingService, DocumentMatchingService>();
    builder.Services.AddScoped<IAuthorizedSenderService, ZSR.Underwriting.Infrastructure.Services.AuthorizedSenderService>();
    builder.Services.AddScoped<IEmailIngestionService, ZSR.Underwriting.Infrastructure.Services.EmailIngestionService>();

    // Rate limiting — per-user upload throttle + auth endpoint protection
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Global limiter: rate-limit auth form submissions by IP
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            if (httpContext.Request.Method == "POST" &&
                (httpContext.Request.Path.StartsWithSegments("/verify-code") ||
                 httpContext.Request.Path.StartsWithSegments("/login")))
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"auth:{ip}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    });
            }
            return RateLimitPartition.GetNoLimiter("");
        });

        options.AddPolicy("upload", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                }));
    });

    // Add document parsers
    builder.Services.AddScoped<IDocumentParser, RentRollParser>();
    builder.Services.AddScoped<IDocumentParser, T12Parser>();
    builder.Services.AddScoped<IDocumentParser, LoanTermSheetParser>();
    builder.Services.AddScoped<IDocumentParsingService, DocumentParsingService>();
    builder.Services.AddScoped<IOverrideService, OverrideService>();

    // HSTS — 1-year max-age with includeSubDomains
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });

    // Add Blazor services
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    // Auto-migrate database in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    // Seed roles and default admin user
    await SeedData.SeedAsync(app.Services);

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseMiddleware<ZSR.Underwriting.Web.Middleware.SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.UseMiddleware<ZSR.Underwriting.Web.Middleware.TosEnforcementMiddleware>();

    app.UseAntiforgery();

    app.MapExternalAuthEndpoints();
    app.MapDocumentEndpoints();
    app.MapEmailIngestEndpoints();

    app.MapStaticAssets().AllowAnonymous();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program { }
