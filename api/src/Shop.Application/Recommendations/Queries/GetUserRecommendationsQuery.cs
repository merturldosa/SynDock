using MediatR;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Recommendations.Queries;

public record GetUserRecommendationsQuery(int Count = 6)
    : IRequest<Result<IReadOnlyList<RecommendedProduct>>>;

public class GetUserRecommendationsQueryHandler
    : IRequestHandler<GetUserRecommendationsQuery, Result<IReadOnlyList<RecommendedProduct>>>
{
    private readonly IRecommendationEngine _engine;
    private readonly ICurrentUserService _currentUser;

    public GetUserRecommendationsQueryHandler(IRecommendationEngine engine, ICurrentUserService currentUser)
    {
        _engine = engine;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<RecommendedProduct>>> Handle(
        GetUserRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId is null or 0)
            return Result<IReadOnlyList<RecommendedProduct>>.Success([]);

        var results = await _engine.GetRecommendationsForUserAsync(
            userId.Value, request.Count, cancellationToken);

        return Result<IReadOnlyList<RecommendedProduct>>.Success(results);
    }
}
