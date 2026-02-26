using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface ICapExService
{
    Task<IReadOnlyList<CapExProject>> GetProjectsAsync(Guid dealId);
    Task<CapExProject?> GetProjectAsync(Guid projectId);
    Task<Guid> AddProjectAsync(CapExProject project);
    Task UpdateProjectAsync(CapExProject project);
    Task DeleteProjectAsync(Guid projectId);
    Task<Guid> AddLineItemAsync(CapExLineItem lineItem);
    Task DeleteLineItemAsync(Guid lineItemId);
    Task<decimal> GetTotalBudgetAsync(Guid dealId);
    Task<decimal> GetTotalSpendAsync(Guid dealId);
}
