using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IMarketDataService
{
    Task<MarketContextDto> GetMarketContextForDealAsync(Guid dealId, string city, string state);
    Task<MarketContextDto> GetMarketContextAsync(string city, string state);
}
