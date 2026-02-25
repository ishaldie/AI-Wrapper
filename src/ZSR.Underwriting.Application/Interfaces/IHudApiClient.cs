using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IHudApiClient
{
    /// <summary>
    /// Fetches HUD income limits for a given state. Optionally matches a specific
    /// county or metro area name within the state results.
    /// </summary>
    Task<HudIncomeLimitsDto?> GetIncomeLimitsAsync(
        string stateCode,
        string? countyOrMetro = null,
        CancellationToken cancellationToken = default);
}
