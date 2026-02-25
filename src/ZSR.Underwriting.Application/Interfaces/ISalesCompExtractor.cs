using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;

namespace ZSR.Underwriting.Application.Interfaces;

public interface ISalesCompExtractor
{
    Task<SalesCompResult> ExtractCompsAsync(
        MarketContextDto marketContext,
        string subjectAddress,
        decimal subjectPricePerUnit,
        int subjectUnits,
        CancellationToken cancellationToken = default);
}

public class SalesCompResult
{
    public List<SalesCompRow> Comps { get; init; } = [];
    public List<AdjustmentRow> Adjustments { get; init; } = [];
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
