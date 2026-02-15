using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;
    private IDealRepository? _deals;

    public UnitOfWork(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public IDealRepository Deals => _deals ??= new DealRepository(_ctx);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _ctx.SaveChangesAsync(cancellationToken);
    }
}
