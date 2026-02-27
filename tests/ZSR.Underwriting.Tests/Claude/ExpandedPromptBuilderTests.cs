using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Claude;

public class ExpandedPromptBuilderTests
{
    private readonly UnderwritingPromptBuilder _builder = new();

    private static ProseGenerationContext CreateContext(PropertyType type, int units = 100, int? beds = null)
    {
        var deal = new Deal("Test Deal")
        {
            PropertyName = "Test Property",
            Address = "123 Main St, Austin TX",
            PurchasePrice = 10_000_000m,
            PropertyType = type,
            UnitCount = units,
            LicensedBeds = beds,
        };
        return new ProseGenerationContext { Deal = deal, UserId = "test" };
    }

    // === System role prompts contain type-specific keywords ===

    [Fact]
    public void Bridge_PromptContainsValueAddKeywords()
    {
        var ctx = CreateContext(PropertyType.Bridge, units: 48);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("bridge", prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Hospitality_PromptContainsHotelKeywords()
    {
        var ctx = CreateContext(PropertyType.Hospitality, units: 85);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("hospitality", prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Commercial_PromptContainsTenantKeywords()
    {
        var ctx = CreateContext(PropertyType.Commercial, units: 0);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("commercial", prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LIHTC_PromptContainsAffordableKeywords()
    {
        var ctx = CreateContext(PropertyType.LIHTC, units: 60);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("affordable", prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BoardAndCare_PromptContainsSeniorKeywords()
    {
        var ctx = CreateContext(PropertyType.BoardAndCare, beds: 16);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("senior", prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IndependentLiving_PromptContainsSeniorKeywords()
    {
        var ctx = CreateContext(PropertyType.IndependentLiving, beds: 100);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("senior", prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeniorApartment_PromptContainsSeniorKeywords()
    {
        var ctx = CreateContext(PropertyType.SeniorApartment, beds: 80);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("senior", prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    // === All 12 types produce valid prompts without errors ===

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.AssistedLiving)]
    [InlineData(PropertyType.SkilledNursing)]
    [InlineData(PropertyType.MemoryCare)]
    [InlineData(PropertyType.CCRC)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    [InlineData(PropertyType.BoardAndCare)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    public void AllPropertyTypes_ProduceValidPrompts(PropertyType type)
    {
        var ctx = CreateContext(type, units: 50, beds: 50);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.NotNull(prompt);
        Assert.NotEmpty(prompt.SystemPrompt);
        Assert.NotEmpty(prompt.UserMessage);
        Assert.True(prompt.MaxTokens > 0);
    }

    // === Asset type labels for new types ===

    [Fact]
    public void Bridge_UserMessage_ContainsBridgeLoan()
    {
        var ctx = CreateContext(PropertyType.Bridge, units: 24);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("bridge", prompt.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Hospitality_UserMessage_ContainsHotel()
    {
        var ctx = CreateContext(PropertyType.Hospitality, units: 100);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("hotel", prompt.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Commercial_UserMessage_ContainsCommercial()
    {
        var ctx = CreateContext(PropertyType.Commercial, units: 0);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("commercial", prompt.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LIHTC_UserMessage_ContainsAffordable()
    {
        var ctx = CreateContext(PropertyType.LIHTC, units: 60);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("affordable", prompt.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    // === Property header labels ===

    [Fact]
    public void Hospitality_PropertyHeader_ShowsRooms()
    {
        var ctx = CreateContext(PropertyType.Hospitality, units: 85);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("Rooms:", prompt.UserMessage);
    }

    [Fact]
    public void Multifamily_PropertyHeader_ShowsUnits()
    {
        var ctx = CreateContext(PropertyType.Multifamily, units: 100);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("Units:", prompt.UserMessage);
    }

    [Fact]
    public void SeniorHousing_PropertyHeader_ShowsBeds()
    {
        var ctx = CreateContext(PropertyType.AssistedLiving, beds: 120);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);
        Assert.Contains("Licensed Beds:", prompt.UserMessage);
    }
}
