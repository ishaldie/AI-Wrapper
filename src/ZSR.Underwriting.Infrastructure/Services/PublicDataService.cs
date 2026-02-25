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
        // Run all API calls in parallel for performance
        var censusTask = GetCensusDataAsync(zipCode, cancellationToken);
        var blsTask = GetBlsDataAsync(state, metro, cancellationToken);
        var fredTask = GetFredDataAsync(cancellationToken);
        var demographicsTask = !string.IsNullOrEmpty(zipCode)
            ? _census.GetTenantDemographicsAsync(zipCode, cancellationToken)
            : Task.FromResult<TenantDemographicsDto?>(null);

        await Task.WhenAll(censusTask, blsTask, fredTask, demographicsTask);

        return new PublicDataDto
        {
            Census = await censusTask,
            Bls = await blsTask,
            Fred = await fredTask,
            TenantDemographics = await demographicsTask,
            RetrievedAt = DateTime.UtcNow
        };
    }
}
