using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shop.Infrastructure.Data;
using SynDock.Core.Entities;
using SynDock.Core.Interfaces;

namespace Shop.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ShopDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ShopDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.Where(predicate).ToListAsync(cancellationToken);

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Delete(T entity)
        => _dbSet.Remove(entity);

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(cancellationToken);
}
