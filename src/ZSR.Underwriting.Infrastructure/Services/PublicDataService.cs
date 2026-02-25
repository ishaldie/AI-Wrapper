using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

public class PublicDataService : IPublicDataService
{
    private readonly CensusApiClient _census;
    private readonly BlsApiClient _bls;
    private readonly FredApiClient _fred;

    public PublicDataService(CensusApiClient census, BlsApiClient bls, FredApiClient fred)
    {
        _census = census;
        _bls = bls;
        _fred = fred;
    }

    public Task<CensusData?> GetCensusDataAsync(string zipCode, CancellationToken cancellationToken = default)
        => _census.GetCensusDataAsync(zipCode, cancellationToken);

    public Task<BlsData?> GetBlsDataAsync(string state, string metro, CancellationToken cancellationToken = default)
        => _bls.GetBlsDataAsync(state, metro, cancellationToken);

    public Task<FredData?> GetFredDataAsync(CancellationToken cancellationToken = default)
        => _fred.GetFredDataAsync(cancellationToken);

    public async Task<PublicDataDto> GetAllPublicDataAsync(
        string zipCode, string state, string metro, CancellationToken cancellationToken = default)
    {
        // Run all 3 API calls in parallel for performance
        var censusTask = GetCensusDataAsync(zipCode, cancellationToken);
        var blsTask = GetBlsDataAsync(state, metro, cancellationToken);
        var fredTask = GetFredDataAsync(cancellationToken);

        await Task.WhenAll(censusTask, blsTask, fredTask);

        return new PublicDataDto
        {
            Census = await censusTask,
            Bls = await blsTask,
            Fred = await fredTask,
            RetrievedAt = DateTime.UtcNow
        };
    }
}
