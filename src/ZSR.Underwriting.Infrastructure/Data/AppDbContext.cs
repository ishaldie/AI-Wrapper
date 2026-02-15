using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
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
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasMany(e => e.UploadedDocuments)
                .WithOne(d => d.Deal)
                .HasForeignKey(d => d.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UploadedDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.StoredPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DocumentType).HasConversion<string>().HasMaxLength(30);
        });
    }
}
