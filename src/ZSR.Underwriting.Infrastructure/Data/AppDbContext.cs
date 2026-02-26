using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<UnderwritingInput> UnderwritingInputs => Set<UnderwritingInput>();
    public DbSet<CalculationResult> CalculationResults => Set<CalculationResult>();
    public DbSet<UnderwritingReport> UnderwritingReports => Set<UnderwritingReport>();
    public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();
    public DbSet<FieldOverride> FieldOverrides => Set<FieldOverride>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<DealInvestor> DealInvestors => Set<DealInvestor>();
    public DbSet<CapitalStackItem> CapitalStackItems => Set<CapitalStackItem>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<DealChecklistItem> DealChecklistItems => Set<DealChecklistItem>();
    public DbSet<AuthorizedSender> AuthorizedSenders => Set<AuthorizedSender>();
    public DbSet<EmailIngestionLog> EmailIngestionLogs => Set<EmailIngestionLog>();
    public DbSet<TokenUsageRecord> TokenUsageRecords => Set<TokenUsageRecord>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<RentRollUnit> RentRollUnits => Set<RentRollUnit>();
    public DbSet<ContractTimeline> ContractTimelines => Set<ContractTimeline>();
    public DbSet<ClosingCostItem> ClosingCostItems => Set<ClosingCostItem>();
    public DbSet<MonthlyActual> MonthlyActuals => Set<MonthlyActual>();
    public DbSet<CapExProject> CapExProjects => Set<CapExProject>();
    public DbSet<CapExLineItem> CapExLineItems => Set<CapExLineItem>();
    public DbSet<AssetReport> AssetReports => Set<AssetReport>();
    public DbSet<DispositionAnalysis> DispositionAnalyses => Set<DispositionAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Deal ---
        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(e => e.Phase); // Computed from Status, not stored

            // Closing snapshot
            entity.Property(e => e.ActualPurchasePrice).HasPrecision(18, 2);

            // Temporary flat fields (backward compat with DealService)
            entity.Property(e => e.PropertyName).HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.RentRollSummary).HasPrecision(18, 2);
            entity.Property(e => e.T12Summary).HasPrecision(18, 2);
            entity.Property(e => e.LoanLtv).HasPrecision(5, 2);
            entity.Property(e => e.LoanRate).HasPrecision(5, 2);
            entity.Property(e => e.CapexBudget).HasPrecision(18, 2);
            entity.Property(e => e.TargetOccupancy).HasPrecision(5, 2);
            entity.Property(e => e.ValueAddPlans).HasMaxLength(2000);
            entity.Property(e => e.Latitude).HasPrecision(9, 6);
            entity.Property(e => e.Longitude).HasPrecision(9, 6);

            // ShortCode for email ingestion
            entity.Property(e => e.ShortCode).IsRequired().HasMaxLength(8);
            entity.HasIndex(e => e.ShortCode).IsUnique();

            // Multi-tenant ownership
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            // One-to-one: Deal → Property
            entity.HasOne(e => e.Property)
                .WithOne(p => p.Deal)
                .HasForeignKey<Property>(p => p.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one: Deal → UnderwritingInput
            entity.HasOne(e => e.UnderwritingInput)
                .WithOne(u => u.Deal)
                .HasForeignKey<UnderwritingInput>(u => u.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one: Deal → CalculationResult
            entity.HasOne(e => e.CalculationResult)
                .WithOne(c => c.Deal)
                .HasForeignKey<CalculationResult>(c => c.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one: Deal → UnderwritingReport
            entity.HasOne(e => e.Report)
                .WithOne(r => r.Deal)
                .HasForeignKey<UnderwritingReport>(r => r.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Deal → UploadedDocuments
            entity.HasMany(e => e.UploadedDocuments)
                .WithOne(d => d.Deal)
                .HasForeignKey(d => d.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Deal → FieldOverrides
            entity.HasMany(e => e.FieldOverrides)
                .WithOne(f => f.Deal)
                .HasForeignKey(f => f.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Deal → ChatMessages
            entity.HasMany(e => e.ChatMessages)
                .WithOne(m => m.Deal)
                .HasForeignKey(m => m.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Deal → DealInvestors
            entity.HasMany(e => e.DealInvestors)
                .WithOne(i => i.Deal)
                .HasForeignKey(i => i.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Deal → CapitalStackItems
            entity.HasMany(e => e.CapitalStackItems)
                .WithOne(c => c.Deal)
                .HasForeignKey(c => c.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many: Deal → DealChecklistItems
            entity.HasMany(e => e.DealChecklistItems)
                .WithOne(ci => ci.Deal)
                .HasForeignKey(ci => ci.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deal classification
            entity.Property(e => e.ExecutionType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TransactionType).HasMaxLength(50);
        });

        // --- Property ---
        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BuildingType).HasMaxLength(100);
            entity.Property(e => e.Acreage).HasPrecision(10, 2);
            entity.HasIndex(e => e.DealId).IsUnique();
        });

        // --- UnderwritingInput ---
        modelBuilder.Entity<UnderwritingInput>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.LoanLtv).HasPrecision(5, 2);
            entity.Property(e => e.LoanRate).HasPrecision(5, 2);
            entity.Property(e => e.RentRollSummary).HasPrecision(18, 2);
            entity.Property(e => e.T12Summary).HasPrecision(18, 2);
            entity.Property(e => e.CapexBudget).HasPrecision(18, 2);
            entity.Property(e => e.TargetOccupancy).HasPrecision(5, 2);
            entity.Property(e => e.ValueAddPlans).HasMaxLength(2000);
            entity.HasIndex(e => e.DealId).IsUnique();
        });

        // --- CalculationResult ---
        modelBuilder.Entity<CalculationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GrossPotentialRent).HasPrecision(18, 2);
            entity.Property(e => e.VacancyLoss).HasPrecision(18, 2);
            entity.Property(e => e.EffectiveGrossIncome).HasPrecision(18, 2);
            entity.Property(e => e.OtherIncome).HasPrecision(18, 2);
            entity.Property(e => e.OperatingExpenses).HasPrecision(18, 2);
            entity.Property(e => e.NetOperatingIncome).HasPrecision(18, 2);
            entity.Property(e => e.NoiMargin).HasPrecision(5, 2);
            entity.Property(e => e.GoingInCapRate).HasPrecision(5, 2);
            entity.Property(e => e.ExitCapRate).HasPrecision(5, 2);
            entity.Property(e => e.PricePerUnit).HasPrecision(18, 2);
            entity.Property(e => e.LoanAmount).HasPrecision(18, 2);
            entity.Property(e => e.AnnualDebtService).HasPrecision(18, 2);
            entity.Property(e => e.DebtServiceCoverageRatio).HasPrecision(8, 4);
            entity.Property(e => e.CashOnCashReturn).HasPrecision(8, 4);
            entity.Property(e => e.InternalRateOfReturn).HasPrecision(8, 4);
            entity.Property(e => e.EquityMultiple).HasPrecision(8, 4);
            entity.Property(e => e.ExitValue).HasPrecision(18, 2);
            entity.Property(e => e.TotalProfit).HasPrecision(18, 2);
            entity.HasIndex(e => e.DealId).IsUnique();
        });

        // --- UnderwritingReport ---
        modelBuilder.Entity<UnderwritingReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealId).IsUnique();
        });

        // --- UploadedDocument ---
        modelBuilder.Entity<UploadedDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.StoredPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DocumentType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.VirusScanStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.UploadedByUserId).HasMaxLength(36);
            entity.HasIndex(e => e.DealId);
        });

        // --- FieldOverride ---
        modelBuilder.Entity<FieldOverride>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OriginalValue).HasMaxLength(500);
            entity.Property(e => e.NewValue).HasMaxLength(500);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.DealId);
        });

        // --- UserSession ---
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.UserId);

            entity.HasMany(e => e.ActivityEvents)
                .WithOne()
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ActivityEvent ---
        modelBuilder.Entity<ActivityEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.PageUrl).HasMaxLength(2000);
            entity.Property(e => e.Metadata).HasMaxLength(4000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.DealId);
        });

        // --- ChatMessage ---
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.DealId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // --- DealInvestor ---
        modelBuilder.Entity<DealInvestor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Company).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.Zip).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.NetWorth).HasPrecision(18, 2);
            entity.Property(e => e.Liquidity).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.DealId);
        });

        // --- CapitalStackItem ---
        modelBuilder.Entity<CapitalStackItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Rate).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.DealId);
        });

        // --- ChecklistTemplate ---
        modelBuilder.Entity<ChecklistTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Section).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ItemName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ExecutionType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TransactionType).HasMaxLength(100);
            entity.HasIndex(e => new { e.SectionOrder, e.SortOrder });
        });

        // --- DealChecklistItem ---
        modelBuilder.Entity<DealChecklistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.DealId);
            entity.HasIndex(e => e.ChecklistTemplateId);

            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.ChecklistTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuthorizedSender>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Label).HasMaxLength(200);

            entity.HasIndex(e => new { e.UserId, e.Email }).IsUnique();
        });

        modelBuilder.Entity<EmailIngestionLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SenderEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.Deal)
                .WithMany()
                .HasForeignKey(e => e.DealId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.DealId);
        });

        // --- TokenUsageRecord ---
        modelBuilder.Entity<TokenUsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.OperationType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DealId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // --- Portfolio (Track 2) ---
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Strategy).HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.UserId);

            entity.HasMany(e => e.Deals)
                .WithOne(d => d.Portfolio)
                .HasForeignKey(d => d.PortfolioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- RentRollUnit (Track 3) ---
        modelBuilder.Entity<RentRollUnit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.MarketRent).HasPrecision(18, 2);
            entity.Property(e => e.ActualRent).HasPrecision(18, 2);
            entity.Property(e => e.SecurityDeposit).HasPrecision(18, 2);
            entity.Property(e => e.MonthlyCharges).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TenantName).HasMaxLength(200);
            entity.HasIndex(e => e.DealId);

            entity.HasOne(e => e.Deal)
                .WithMany()
                .HasForeignKey(e => e.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ContractTimeline (Track 6) ---
        modelBuilder.Entity<ContractTimeline>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EarnestMoneyDeposit).HasPrecision(18, 2);
            entity.Property(e => e.AdditionalDeposit).HasPrecision(18, 2);
            entity.Property(e => e.LenderName).HasMaxLength(200);
            entity.Property(e => e.TitleCompany).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.DealId).IsUnique();

            entity.HasOne(e => e.Deal)
                .WithOne()
                .HasForeignKey<ContractTimeline>(e => e.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ClosingCostItem (Track 6) ---
        modelBuilder.Entity<ClosingCostItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.EstimatedAmount).HasPrecision(18, 2);
            entity.Property(e => e.ActualAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.DealId);

            entity.HasOne(e => e.Deal)
                .WithMany()
                .HasForeignKey(e => e.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- MonthlyActual (Track 4) ---
        modelBuilder.Entity<MonthlyActual>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DealId, e.Year, e.Month }).IsUnique();

            entity.Property(e => e.GrossRentalIncome).HasPrecision(18, 2);
            entity.Property(e => e.VacancyLoss).HasPrecision(18, 2);
            entity.Property(e => e.OtherIncome).HasPrecision(18, 2);
            entity.Property(e => e.EffectiveGrossIncome).HasPrecision(18, 2);
            entity.Property(e => e.PropertyTaxes).HasPrecision(18, 2);
            entity.Property(e => e.Insurance).HasPrecision(18, 2);
            entity.Property(e => e.Utilities).HasPrecision(18, 2);
            entity.Property(e => e.Repairs).HasPrecision(18, 2);
            entity.Property(e => e.Management).HasPrecision(18, 2);
            entity.Property(e => e.Payroll).HasPrecision(18, 2);
            entity.Property(e => e.Marketing).HasPrecision(18, 2);
            entity.Property(e => e.Administrative).HasPrecision(18, 2);
            entity.Property(e => e.OtherExpenses).HasPrecision(18, 2);
            entity.Property(e => e.TotalOperatingExpenses).HasPrecision(18, 2);
            entity.Property(e => e.NetOperatingIncome).HasPrecision(18, 2);
            entity.Property(e => e.DebtService).HasPrecision(18, 2);
            entity.Property(e => e.CapitalExpenditures).HasPrecision(18, 2);
            entity.Property(e => e.CashFlow).HasPrecision(18, 2);
            entity.Property(e => e.OccupancyPercent).HasPrecision(5, 2);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            entity.HasOne(e => e.Deal)
                .WithMany()
                .HasForeignKey(e => e.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- CapExProject (Track 7) ---
        modelBuilder.Entity<CapExProject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.BudgetAmount).HasPrecision(18, 2);
            entity.Property(e => e.ActualSpend).HasPrecision(18, 2);
            entity.Property(e => e.ExpectedRentIncrease).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(e => e.BudgetVariance);
            entity.Ignore(e => e.BudgetUtilizationPercent);
            entity.HasIndex(e => e.DealId);

            entity.HasOne(e => e.Deal)
                .WithMany()
                .HasForeignKey(e => e.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- CapExLineItem (Track 7) ---
        modelBuilder.Entity<CapExLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Vendor).HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.CapExProjectId);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.LineItems)
                .HasForeignKey(e => e.CapExProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- AssetReport (Track 8) ---
        modelBuilder.Entity<AssetReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.DealId);

            entity.HasOne(e => e.Deal)
                .WithMany()
                .HasForeignKey(e => e.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- DispositionAnalysis (Track 9) ---
        modelBuilder.Entity<DispositionAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BrokerOpinionOfValue).HasPrecision(18, 2);
            entity.Property(e => e.CurrentMarketCapRate).HasPrecision(8, 4);
            entity.Property(e => e.TrailingTwelveMonthNoi).HasPrecision(18, 2);
            entity.Property(e => e.ImpliedValue).HasPrecision(18, 2);
            entity.HasIndex(e => e.DealId).IsUnique();

            entity.HasOne(e => e.Deal)
                .WithMany()
                .HasForeignKey(e => e.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
