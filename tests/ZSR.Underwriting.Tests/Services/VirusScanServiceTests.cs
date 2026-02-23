using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Services;

public class VirusScanServiceTests
{
    [Fact]
    public void VirusScanResult_Clean_HasCorrectStatus()
    {
        var result = new VirusScanResult(VirusScanStatus.Clean);
        Assert.Equal(VirusScanStatus.Clean, result.Status);
        Assert.Null(result.ThreatName);
    }

    [Fact]
    public void VirusScanResult_Infected_HasThreatName()
    {
        var result = new VirusScanResult(VirusScanStatus.Infected, "EICAR-Test");
        Assert.Equal(VirusScanStatus.Infected, result.Status);
        Assert.Equal("EICAR-Test", result.ThreatName);
    }

    [Fact]
    public void VirusScanResult_ScanFailed_HasMessage()
    {
        var result = new VirusScanResult(VirusScanStatus.ScanFailed, "Scanner not available");
        Assert.Equal(VirusScanStatus.ScanFailed, result.Status);
        Assert.Equal("Scanner not available", result.ThreatName);
    }
}

public class VirusScanStatusEnumTests
{
    [Fact]
    public void VirusScanStatus_HasAllExpectedValues()
    {
        var values = Enum.GetValues<VirusScanStatus>();
        Assert.Contains(VirusScanStatus.Pending, values);
        Assert.Contains(VirusScanStatus.Clean, values);
        Assert.Contains(VirusScanStatus.Infected, values);
        Assert.Contains(VirusScanStatus.ScanFailed, values);
        Assert.Equal(4, values.Length);
    }
}
