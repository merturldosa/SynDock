using MediatR;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Recommendations.Queries;

public record GetPopularProductsQuery(int Count = 6)
    : IRequest<Result<IReadOnlyList<RecommendedProduct>>>;

public class GetPopularProductsQueryHandler
    : IRequestHandler<GetPopularProductsQuery, Result<IReadOnlyList<RecommendedProduct>>>
{
    private readonly IRecommendationEngine _engine;

    public GetPopularProductsQueryHandler(IRecommendationEngine engine)
    {
        _engine = engine;
    }

    public async Task<Result<IReadOnlyList<RecommendedProduct>>> Handle(
        GetPopularProductsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await _engine.GetPopularProductsAsync(
            request.Count, cancellationToken);

        return Result<IReadOnlyList<RecommendedProduct>>.Success(results);
    }
}
