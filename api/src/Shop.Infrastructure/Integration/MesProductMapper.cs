using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Integration;

public class MesProductMapper : IMesProductMapper
{
    private readonly IShopDbContext _db;
    private readonly IDistributedCache _cache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    private const string CachePrefix = "mes:productmap:";

    public MesProductMapper(IShopDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<string?> GetMesProductCodeAsync(int productId, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}shop:{productId}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return cached == "" ? null : cached;

        var sourceId = await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => p.SourceId)
            .FirstOrDefaultAsync(ct);

        await _cache.SetStringAsync(cacheKey, sourceId ?? "",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, ct);

        return sourceId;
    }

    public async Task<int?> GetShopProductIdAsync(string mesProductCode, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}mes:{mesProductCode}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return cached == "" ? null : int.Parse(cached);

        var productId = await _db.Products.AsNoTracking()
            .Where(p => p.SourceId == mesProductCode)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        await _cache.SetStringAsync(cacheKey, productId?.ToString() ?? "",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, ct);

        return productId;
    }

    public async Task<Dictionary<int, string>> GetAllMappingsAsync(CancellationToken ct = default)
    {
        return await _db.Products.AsNoTracking()
            .Where(p => p.SourceId != null && p.SourceId != "")
            .ToDictionaryAsync(p => p.Id, p => p.SourceId!, ct);
    }
}
