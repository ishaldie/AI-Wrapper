using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests.Domain;

public class FannieChecklistTests
{
    // === Task 1: FannieProductType property on ChecklistTemplate ===

    [Fact]
    public void ChecklistTemplate_FannieProductType_DefaultsToNull()
    {
        var template = new ChecklistTemplate(
            "Test Section", 1, "Test Item", 1,
            ExecutionType.FannieMae, "All");

        Assert.Null(template.FannieProductType);
    }

    [Fact]
    public void ChecklistTemplate_FannieProductType_CanBeSet()
    {
        var template = new ChecklistTemplate(
            "Cooperative Due Diligence", 18, "Operating Reserve Verification", 1,
            ExecutionType.FannieMae, "All");

        template.FannieProductType = FannieProductType.Cooperative;

        Assert.Equal(FannieProductType.Cooperative, template.FannieProductType);
    }

    // === Task 2: Seed data includes product-specific items ===

    [Fact]
    public async Task Seed_IncludesSeniorsProductItems()
    {
        await using var db = await CreateAndSeedDb();

        var seniorsItems = await db.ChecklistTemplates
            .Where(t => t.FannieProductType == FannieProductType.SeniorsAL
                     || t.FannieProductType == FannieProductType.SeniorsIL
                     || t.FannieProductType == FannieProductType.SeniorsALZ)
            .ToListAsync();

        Assert.NotEmpty(seniorsItems);
        Assert.Contains(seniorsItems, t => t.ItemName.Contains("Management") || t.ItemName.Contains("Operations"));
    }

    [Fact]
    public async Task Seed_IncludesCooperativeProductItems()
    {
        await using var db = await CreateAndSeedDb();

        var coopItems = await db.ChecklistTemplates
            .Where(t => t.FannieProductType == FannieProductType.Cooperative)
            .ToListAsync();

        Assert.NotEmpty(coopItems);
        Assert.Contains(coopItems, t => t.ItemName.Contains("Operating Reserve") || t.ItemName.Contains("Governance"));
    }

    [Fact]
    public async Task Seed_IncludesGreenProductItems()
    {
        await using var db = await CreateAndSeedDb();

        var greenItems = await db.ChecklistTemplates
            .Where(t => t.FannieProductType == FannieProductType.GreenRewards)
            .ToListAsync();

        Assert.NotEmpty(greenItems);
        Assert.Contains(greenItems, t => t.ItemName.Contains("HPB") || t.ItemName.Contains("High Performance"));
    }

    [Fact]
    public async Task Seed_IncludesMhcProductItems()
    {
        await using var db = await CreateAndSeedDb();

        var mhcItems = await db.ChecklistTemplates
            .Where(t => t.FannieProductType == FannieProductType.ManufacturedHousing)
            .ToListAsync();

        Assert.NotEmpty(mhcItems);
        Assert.Contains(mhcItems, t => t.ItemName.Contains("Flood") || t.ItemName.Contains("Pad Site"));
    }

    [Fact]
    public async Task Seed_IncludesStudentProductItems()
    {
        await using var db = await CreateAndSeedDb();

        var studentItems = await db.ChecklistTemplates
            .Where(t => t.FannieProductType == FannieProductType.StudentHousing)
            .ToListAsync();

        Assert.NotEmpty(studentItems);
        Assert.Contains(studentItems, t => t.ItemName.Contains("Enrollment"));
    }

    // === Task 3: Checklist filtering includes FannieProductType ===

    [Fact]
    public async Task Filtering_NullFannieProductType_MatchesAllDeals()
    {
        await using var db = await CreateAndSeedDb();

        // Templates with null FannieProductType should match any deal
        var nullFannieTemplates = await db.ChecklistTemplates
            .Where(t => t.FannieProductType == null)
            .CountAsync();

        Assert.True(nullFannieTemplates > 0, "Should have templates without FannieProductType filter");
    }

    [Fact]
    public async Task Filtering_SpecificProductType_OnlyMatchesMatchingDeals()
    {
        await using var db = await CreateAndSeedDb();

        var allTemplates = await db.ChecklistTemplates.CountAsync();
        var coopOnly = await db.ChecklistTemplates
            .Where(t => t.FannieProductType == FannieProductType.Cooperative)
            .CountAsync();

        // Cooperative-specific templates should be a small subset of all templates
        Assert.True(coopOnly > 0, "Should have Cooperative-specific templates");
        Assert.True(coopOnly < allTemplates, "Cooperative templates should be subset");
    }

    [Fact]
    public async Task Filtering_MatchesCorrectTemplatesForFannieDeal()
    {
        await using var db = await CreateAndSeedDb();

        var fannieProductType = FannieProductType.Cooperative;
        var executionType = ExecutionType.FannieMae;

        // Simulate the DealTabs filtering logic with FannieProductType
        var matchedTemplates = await db.ChecklistTemplates
            .Where(t => t.ExecutionType == ExecutionType.All || t.ExecutionType == executionType)
            .Where(t => t.TransactionType == "All")
            .Where(t => t.FannieProductType == null || t.FannieProductType == fannieProductType)
            .ToListAsync();

        // Should include generic items (null FannieProductType) plus Cooperative-specific items
        var genericCount = matchedTemplates.Count(t => t.FannieProductType == null);
        var coopCount = matchedTemplates.Count(t => t.FannieProductType == FannieProductType.Cooperative);

        Assert.True(genericCount > 0, "Should include generic templates");
        Assert.True(coopCount > 0, "Should include Cooperative-specific templates");

        // Should NOT include items for other product types
        Assert.DoesNotContain(matchedTemplates, t => t.FannieProductType == FannieProductType.SeniorsAL);
        Assert.DoesNotContain(matchedTemplates, t => t.FannieProductType == FannieProductType.GreenRewards);
    }

    // === Task 5: EF Core persists FannieProductType ===

    [Fact]
    public async Task EfCore_PersistsFannieProductType()
    {
        await using var db = CreateDb();

        var template = new ChecklistTemplate(
            "Fannie Due Diligence", 18, "Product-Specific Doc", 1,
            ExecutionType.FannieMae, "All");
        template.FannieProductType = FannieProductType.SARM;

        db.ChecklistTemplates.Add(template);
        await db.SaveChangesAsync();

        var loaded = await db.ChecklistTemplates.FindAsync(template.Id);
        Assert.NotNull(loaded);
        Assert.Equal(FannieProductType.SARM, loaded.FannieProductType);
    }

    [Fact]
    public async Task EfCore_PersistsNullFannieProductType()
    {
        await using var db = CreateDb();

        var template = new ChecklistTemplate(
            "General Section", 1, "General Doc", 1,
            ExecutionType.All, "All");

        db.ChecklistTemplates.Add(template);
        await db.SaveChangesAsync();

        var loaded = await db.ChecklistTemplates.FindAsync(template.Id);
        Assert.NotNull(loaded);
        Assert.Null(loaded.FannieProductType);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"FannieChecklist_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<AppDbContext> CreateAndSeedDb()
    {
        var db = CreateDb();
        var templates = ChecklistTemplateSeed.GetTemplates();
        db.ChecklistTemplates.AddRange(templates);
        await db.SaveChangesAsync();
        return db;
    }
}
