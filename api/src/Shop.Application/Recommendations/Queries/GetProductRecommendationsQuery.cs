using MediatR;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Recommendations.Queries;

public record GetProductRecommendationsQuery(
    int ProductId,
    int Count = 6
) : IRequest<Result<IReadOnlyList<RecommendedProduct>>>;

public class GetProductRecommendationsQueryHandler
    : IRequestHandler<GetProductRecommendationsQuery, Result<IReadOnlyList<RecommendedProduct>>>
{
    private readonly IRecommendationEngine _engine;

    public GetProductRecommendationsQueryHandler(IRecommendationEngine engine)
    {
        _engine = engine;
    }

    public async Task<Result<IReadOnlyList<RecommendedProduct>>> Handle(
        GetProductRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await _engine.GetRecommendationsForProductAsync(
            request.ProductId, request.Count, cancellationToken);

        return Result<IReadOnlyList<RecommendedProduct>>.Success(results);
    }
}
