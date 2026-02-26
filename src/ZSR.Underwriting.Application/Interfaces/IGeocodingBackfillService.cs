namespace ZSR.Underwriting.Application.Interfaces;

public interface IGeocodingBackfillService
{
    Task<int> BackfillAsync(CancellationToken cancellationToken = default);
}
