namespace ZSR.Underwriting.Domain.Interfaces;

public interface ITokenBudgetService
{
    Task<(bool Allowed, int Used, int Limit)> CheckUserBudgetAsync(string userId);
    Task<(bool Allowed, int Used, int Limit, bool Warning)> CheckDealBudgetAsync(Guid dealId);
}
