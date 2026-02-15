using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IOverrideService
{
    /// <summary>
    /// Applies parsed document data to the deal, creating FieldOverride records for each changed field.
    /// </summary>
    Task<OverrideApplicationResult> ApplyOverridesAsync(Guid dealId, ParsedDocumentResult parsedData, CancellationToken ct = default);

    /// <summary>
    /// Gets all field overrides for a deal.
    /// </summary>
    Task<IReadOnlyList<FieldOverrideDto>> GetOverridesForDealAsync(Guid dealId, CancellationToken ct = default);
}
