namespace ZSR.Underwriting.Domain.Interfaces;

public interface IUnitOfWork
{
    IDealRepository Deals { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
