using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Saints.Queries;

public record GetSaintByIdQuery(int Id) : IRequest<Result<SaintDto>>;

public class GetSaintByIdQueryHandler : IRequestHandler<GetSaintByIdQuery, Result<SaintDto>>
{
    private readonly IShopDbContext _db;

    public GetSaintByIdQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SaintDto>> Handle(GetSaintByIdQuery request, CancellationToken cancellationToken)
    {
        var saint = await _db.Saints
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
            .Select(s => new SaintDto(
                s.Id, s.KoreanName, s.LatinName, s.EnglishName,
                s.Description, s.FeastDay, s.Patronage, s.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (saint is null)
            return Result<SaintDto>.Failure("Saint not found.");

        return Result<SaintDto>.Success(saint);
    }
}
