using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class DealTabsEnumTests
{
    [Fact]
    public void ChecklistStatus_has_all_expected_values()
    {
        var values = Enum.GetValues<ChecklistStatus>();
        Assert.Equal(7, values.Length);
        Assert.Contains(ChecklistStatus.Outstanding, values);
        Assert.Contains(ChecklistStatus.UnderReview, values);
        Assert.Contains(ChecklistStatus.NeedAdditionalInfo, values);
        Assert.Contains(ChecklistStatus.WaiverRequested, values);
        Assert.Contains(ChecklistStatus.WaiverApproved, values);
        Assert.Contains(ChecklistStatus.Satisfied, values);
        Assert.Contains(ChecklistStatus.NotApplicable, values);
    }

    [Fact]
    public void ExecutionType_has_all_expected_values()
    {
        var values = Enum.GetValues<ExecutionType>();
        Assert.Equal(3, values.Length);
        Assert.Contains(ExecutionType.All, values);
        Assert.Contains(ExecutionType.FannieMae, values);
        Assert.Contains(ExecutionType.FreddieMac, values);
    }

    [Fact]
    public void CapitalSource_has_all_expected_values()
    {
        var values = Enum.GetValues<CapitalSource>();
        Assert.Equal(5, values.Length);
        Assert.Contains(CapitalSource.SeniorDebt, values);
        Assert.Contains(CapitalSource.Mezzanine, values);
        Assert.Contains(CapitalSource.PreferredEquity, values);
        Assert.Contains(CapitalSource.SponsorEquity, values);
        Assert.Contains(CapitalSource.Other, values);
    }

    [Fact]
    public void ChecklistStatus_Outstanding_is_default()
    {
        Assert.Equal(0, (int)ChecklistStatus.Outstanding);
    }

    [Fact]
    public void ExecutionType_All_is_default()
    {
        Assert.Equal(0, (int)ExecutionType.All);
    }

    [Fact]
    public void CapitalSource_SeniorDebt_is_default()
    {
        Assert.Equal(0, (int)CapitalSource.SeniorDebt);
    }
}
