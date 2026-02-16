using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using MudBlazor.Services;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Parsing;
using ZSR.Underwriting.Infrastructure.Repositories;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Web.Components;

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

    // Configure cookie authentication
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

    // Require authentication on all routes by default
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

    // Add RealAI client with Polly retry policy
    builder.Services.Configure<RealAiOptions>(
        builder.Configuration.GetSection(RealAiOptions.SectionName));

    builder.Services.AddHttpClient<IRealAiClient, RealAiClient>((sp, client) =>
    {
        var options = builder.Configuration
            .GetSection(RealAiOptions.SectionName)
            .Get<RealAiOptions>() ?? new RealAiOptions();
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    })
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

    // Add Claude API client with Polly retry policy
    builder.Services.Configure<ClaudeOptions>(
        builder.Configuration.GetSection(ClaudeOptions.SectionName));

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

    // Add RealAI cache service (24-hour TTL per deal)
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<RealAiCacheService>();

    // Add repository layer
    builder.Services.AddScoped<IDealRepository, DealRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Add report services
    builder.Services.AddScoped<IReportAssembler, ReportAssembler>();
    builder.Services.AddSingleton<IReportPdfExporter, ReportPdfExporter>();
    builder.Services.AddScoped<IPromptBuilder, ZSR.Underwriting.Application.Services.UnderwritingPromptBuilder>();
    builder.Services.AddScoped<IReportProseGenerator, ZSR.Underwriting.Application.Services.ReportProseGenerator>();

    // Add application services
    builder.Services.AddScoped<IDealService, DealService>();
    builder.Services.AddScoped<IUserManagementService, UserManagementService>();
    builder.Services.AddScoped<ZSR.Underwriting.Domain.Interfaces.IFileStorageService>(sp =>
        new LocalFileStorageService(Path.Combine(builder.Environment.ContentRootPath, "uploads")));
    builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();

    // Add document parsers
    builder.Services.AddScoped<IDocumentParser, RentRollParser>();
    builder.Services.AddScoped<IDocumentParser, T12Parser>();
    builder.Services.AddScoped<IDocumentParser, LoanTermSheetParser>();
    builder.Services.AddScoped<IDocumentParsingService, DocumentParsingService>();
    builder.Services.AddScoped<IOverrideService, OverrideService>();

    // Add Blazor services
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
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    app.MapStaticAssets();
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
