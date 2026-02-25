using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IPublicDataService
{
    Task<CensusData?> GetCensusDataAsync(string zipCode, CancellationToken cancellationToken = default);
    Task<BlsData?> GetBlsDataAsync(string state, string metro, CancellationToken cancellationToken = default);
    Task<FredData?> GetFredDataAsync(CancellationToken cancellationToken = default);
    Task<PublicDataDto> GetAllPublicDataAsync(string zipCode, string state, string metro, CancellationToken cancellationToken = default);
}
