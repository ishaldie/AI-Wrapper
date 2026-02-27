using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IEdgarCmbsClient
{
    /// <summary>
    /// Fetches recent CMBS loan-level data from SEC EDGAR ABS-EE filings.
    /// Downloads EX-102 XML exhibits and parses them into SecuritizationComp entities.
    /// </summary>
    Task<IReadOnlyList<SecuritizationComp>> FetchRecentFilingsAsync(
        int monthsBack = 36,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a single EX-102 XML document into loan-level SecuritizationComp entities.
    /// </summary>
    IReadOnlyList<SecuritizationComp> ParseEx102Xml(string xml, string? dealName = null);
}
