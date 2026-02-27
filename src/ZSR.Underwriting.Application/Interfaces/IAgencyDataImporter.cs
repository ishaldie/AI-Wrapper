namespace ZSR.Underwriting.Application.Interfaces;

public interface IAgencyDataImporter
{
    /// <summary>
    /// Imports Fannie Mae Multifamily Loan Performance Data from a CSV stream.
    /// Returns the number of records imported.
    /// </summary>
    Task<int> ImportFannieMaeCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports Freddie Mac K-Deal loan-level data from a CSV stream.
    /// Returns the number of records imported.
    /// </summary>
    Task<int> ImportFreddieMacCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports CMBS data from SEC EDGAR (fetches and parses ABS-EE filings).
    /// Returns the number of records imported.
    /// </summary>
    Task<int> ImportEdgarCmbsAsync(int monthsBack = 36, CancellationToken cancellationToken = default);
}
