using Shop.Infrastructure.Data;
using SynDock.Core.Interfaces;

namespace Shop.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ShopDbContext _context;

    public UnitOfWork(ShopDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public void Dispose()
        => _context.Dispose();
}
