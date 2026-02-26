using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface ICmsProviderService
{
    Task<CmsProviderDto?> SearchByNameAndStateAsync(string facilityName, string state, CancellationToken ct = default);
    Task<CmsProviderDto?> GetByCcnAsync(string ccn, CancellationToken ct = default);
    Task<List<CmsDeficiencyDto>> GetDeficienciesAsync(string ccn, CancellationToken ct = default);
    Task<List<CmsPenaltyDto>> GetPenaltiesAsync(string ccn, CancellationToken ct = default);
}
