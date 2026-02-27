using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface ISecuritizationCompService
{
    /// <summary>
    /// Finds comparable securitized loans for a given deal, ranked by similarity.
    /// Filters by property type + state, with fallback to nationwide.
    /// </summary>
    Task<ComparisonResult> FindCompsAsync(Deal deal, int maxResults = 10, CancellationToken cancellationToken = default);
}
