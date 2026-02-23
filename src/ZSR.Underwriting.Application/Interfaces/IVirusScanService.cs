using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Interfaces;

public record VirusScanResult(VirusScanStatus Status, string? ThreatName = null);

public interface IVirusScanService
{
    Task<VirusScanResult> ScanAsync(Stream fileStream, CancellationToken ct = default);
}
