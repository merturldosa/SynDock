using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Products.Queries;

public record SearchSuggestionDto(int Id, string Name, string? PrimaryImageUrl, decimal Price, decimal? SalePrice);

public record GetSearchSuggestionsQuery(string Term) : IRequest<IReadOnlyList<SearchSuggestionDto>>;

public class GetSearchSuggestionsQueryHandler : IRequestHandler<GetSearchSuggestionsQuery, IReadOnlyList<SearchSuggestionDto>>
{
    private readonly IShopDbContext _db;

    public GetSearchSuggestionsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SearchSuggestionDto>> Handle(GetSearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Term) || request.Term.Length < 1)
            return Array.Empty<SearchSuggestionDto>();

        var term = request.Term.ToLower();

        return await _db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.Name.ToLower().Contains(term))
            .OrderByDescending(p => p.ViewCount)
            .Take(10)
            .Select(p => new SearchSuggestionDto(
                p.Id,
                p.Name,
                p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                p.Price,
                p.SalePrice))
            .ToListAsync(cancellationToken);
    }
}
