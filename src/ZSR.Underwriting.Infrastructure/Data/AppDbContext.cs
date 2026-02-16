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
    public DbSet<RealAiData> RealAiDataSets => Set<RealAiData>();
    public DbSet<CalculationResult> CalculationResults => Set<CalculationResult>();
    public DbSet<UnderwritingReport> UnderwritingReports => Set<UnderwritingReport>();
    public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();
    public DbSet<FieldOverride> FieldOverrides => Set<FieldOverride>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Deal ---
        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

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

            // One-to-one: Deal → RealAiData
            entity.HasOne(e => e.RealAiData)
                .WithOne(r => r.Deal)
                .HasForeignKey<RealAiData>(r => r.DealId)
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

        // --- RealAiData ---
        modelBuilder.Entity<RealAiData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InPlaceRent).HasPrecision(18, 2);
            entity.Property(e => e.Occupancy).HasPrecision(5, 2);
            entity.Property(e => e.Acreage).HasPrecision(10, 2);
            entity.Property(e => e.RentToIncomeRatio).HasPrecision(5, 2);
            entity.Property(e => e.MedianHhi).HasPrecision(18, 2);
            entity.Property(e => e.MarketCapRate).HasPrecision(5, 2);
            entity.Property(e => e.RentGrowth).HasPrecision(5, 2);
            entity.Property(e => e.JobGrowth).HasPrecision(5, 2);
            entity.Property(e => e.BuildingType).HasMaxLength(100);
            entity.Property(e => e.Amenities).HasMaxLength(2000);
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
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.DealId);
        });
    }
}
