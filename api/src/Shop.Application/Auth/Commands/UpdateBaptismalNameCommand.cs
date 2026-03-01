using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record UpdateBaptismalNameCommand(
    int UserId,
    string BaptismalName
) : IRequest<Result<BaptismalNameDto>>;

public class UpdateBaptismalNameCommandHandler : IRequestHandler<UpdateBaptismalNameCommand, Result<BaptismalNameDto>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBaptismalNameCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BaptismalNameDto>> Handle(UpdateBaptismalNameCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<BaptismalNameDto>.Failure("사용자를 찾을 수 없습니다.");

        // Find matching patron saint by Korean name
        var patronSaint = await _db.Saints
            .AsNoTracking()
            .Where(s => s.IsActive && s.KoreanName.ToLower().Contains(request.BaptismalName.ToLower()))
            .FirstOrDefaultAsync(cancellationToken);

        // Parse existing custom fields or create new
        var customFields = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(user.CustomFieldsJson))
        {
            try
            {
                customFields = JsonSerializer.Deserialize<Dictionary<string, object>>(user.CustomFieldsJson) ?? new();
            }
            catch
            {
                customFields = new();
            }
        }

        customFields["baptismalName"] = request.BaptismalName;
        if (patronSaint is not null)
            customFields["patronSaintId"] = patronSaint.Id;
        else
            customFields.Remove("patronSaintId");

        user.CustomFieldsJson = JsonSerializer.Serialize(customFields);
        user.UpdatedBy = "user";
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        SaintSummaryDto? saintDto = patronSaint is not null
            ? new SaintSummaryDto(patronSaint.Id, patronSaint.KoreanName, patronSaint.LatinName, patronSaint.FeastDay, patronSaint.Patronage)
            : null;

        return Result<BaptismalNameDto>.Success(
            new BaptismalNameDto(request.BaptismalName, patronSaint?.Id, saintDto));
    }
}
