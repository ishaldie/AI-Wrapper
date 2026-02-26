using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Domain;

public class DealLifecycleTests
{
    // === DealPhase computed property ===

    [Theory]
    [InlineData(DealStatus.Draft, DealPhase.Acquisition)]
    [InlineData(DealStatus.InProgress, DealPhase.Acquisition)]
    [InlineData(DealStatus.Screening, DealPhase.Acquisition)]
    [InlineData(DealStatus.Complete, DealPhase.Acquisition)]
    [InlineData(DealStatus.UnderContract, DealPhase.Contract)]
    [InlineData(DealStatus.Closed, DealPhase.Ownership)]
    [InlineData(DealStatus.Active, DealPhase.Ownership)]
    [InlineData(DealStatus.Disposition, DealPhase.Exit)]
    [InlineData(DealStatus.Sold, DealPhase.Exit)]
    public void Phase_Returns_Correct_Value_For_Status(DealStatus status, DealPhase expectedPhase)
    {
        var deal = new Deal("Test");
        deal.UpdateStatus(status);
        Assert.Equal(expectedPhase, deal.Phase);
    }

    [Fact]
    public void Archived_Is_Acquisition_Phase()
    {
        var deal = new Deal("Test");
        deal.UpdateStatus(DealStatus.Archived);
        Assert.Equal(DealPhase.Acquisition, deal.Phase);
    }

    // === DealStatus enum values ===

    [Fact]
    public void DealStatus_Has_All_Lifecycle_Values()
    {
        var values = Enum.GetNames<DealStatus>();
        Assert.Contains("Draft", values);
        Assert.Contains("InProgress", values);
        Assert.Contains("Screening", values);
        Assert.Contains("Complete", values);
        Assert.Contains("Archived", values);
        Assert.Contains("UnderContract", values);
        Assert.Contains("Closed", values);
        Assert.Contains("Active", values);
        Assert.Contains("Disposition", values);
        Assert.Contains("Sold", values);
    }

    [Fact]
    public void DealPhase_Has_Four_Values()
    {
        var values = Enum.GetValues<DealPhase>();
        Assert.Equal(4, values.Length);
    }

    // === Deal closing fields ===

    [Fact]
    public void New_Deal_Has_Null_Closing_Fields()
    {
        var deal = new Deal("Test");
        Assert.Null(deal.ClosedDate);
        Assert.Null(deal.ActualPurchasePrice);
    }

    [Fact]
    public void Deal_Can_Set_Closing_Fields()
    {
        var deal = new Deal("Test");
        deal.ClosedDate = new DateTime(2026, 3, 1);
        deal.ActualPurchasePrice = 5_500_000m;

        Assert.Equal(new DateTime(2026, 3, 1), deal.ClosedDate);
        Assert.Equal(5_500_000m, deal.ActualPurchasePrice);
    }

    // === Status transition validation ===

    [Theory]
    [InlineData(DealStatus.Draft, DealStatus.Screening, true)]
    [InlineData(DealStatus.Draft, DealStatus.InProgress, true)]
    [InlineData(DealStatus.InProgress, DealStatus.Complete, true)]
    [InlineData(DealStatus.Screening, DealStatus.Complete, true)]
    [InlineData(DealStatus.Complete, DealStatus.UnderContract, true)]
    [InlineData(DealStatus.UnderContract, DealStatus.Closed, true)]
    [InlineData(DealStatus.Closed, DealStatus.Active, true)]
    [InlineData(DealStatus.Active, DealStatus.Disposition, true)]
    [InlineData(DealStatus.Disposition, DealStatus.Sold, true)]
    [InlineData(DealStatus.Archived, DealStatus.Draft, true)]
    public void Valid_Transitions_Are_Allowed(DealStatus from, DealStatus to, bool expected)
    {
        Assert.Equal(expected, DealService.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(DealStatus.Draft, DealStatus.Active)]
    [InlineData(DealStatus.Draft, DealStatus.Closed)]
    [InlineData(DealStatus.Draft, DealStatus.Sold)]
    [InlineData(DealStatus.Complete, DealStatus.Draft)]
    [InlineData(DealStatus.Active, DealStatus.Screening)]
    [InlineData(DealStatus.Sold, DealStatus.Active)]
    public void Invalid_Transitions_Are_Blocked(DealStatus from, DealStatus to)
    {
        Assert.False(DealService.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(DealStatus.Draft)]
    [InlineData(DealStatus.Screening)]
    [InlineData(DealStatus.Complete)]
    [InlineData(DealStatus.UnderContract)]
    [InlineData(DealStatus.Closed)]
    [InlineData(DealStatus.Active)]
    [InlineData(DealStatus.Disposition)]
    public void Archived_Can_Be_Reached_From_Any_Status(DealStatus from)
    {
        Assert.True(DealService.IsValidTransition(from, DealStatus.Archived));
    }

    // === Backward compatibility ===

    [Fact]
    public void InProgress_And_Screening_Both_Map_To_Acquisition()
    {
        var deal1 = new Deal("Test 1");
        deal1.UpdateStatus(DealStatus.InProgress);

        var deal2 = new Deal("Test 2");
        deal2.UpdateStatus(DealStatus.Screening);

        Assert.Equal(deal1.Phase, deal2.Phase);
        Assert.Equal(DealPhase.Acquisition, deal1.Phase);
    }

    [Fact]
    public void InProgress_Can_Transition_To_Screening()
    {
        Assert.True(DealService.IsValidTransition(DealStatus.InProgress, DealStatus.Screening));
    }

    // === ActivityEventType has StatusChanged ===

    [Fact]
    public void ActivityEventType_Has_StatusChanged()
    {
        Assert.True(Enum.IsDefined(typeof(ActivityEventType), ActivityEventType.StatusChanged));
    }
}
