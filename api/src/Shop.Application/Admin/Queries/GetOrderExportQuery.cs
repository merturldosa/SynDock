using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record GetOrderExportQuery(
    string? Status, DateTime? StartDate, DateTime? EndDate, string? Search
) : IRequest<Result<string>>;

public class GetOrderExportQueryHandler : IRequestHandler<GetOrderExportQuery, Result<string>>
{
    private readonly IShopDbContext _db;

    public GetOrderExportQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<string>> Handle(GetOrderExportQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(o => o.Status == request.Status);

        if (request.StartDate.HasValue)
            query = query.Where(o => o.CreatedAt >= request.StartDate.Value.Date);

        if (request.EndDate.HasValue)
            query = query.Where(o => o.CreatedAt < request.EndDate.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(search) ||
                o.User.Name.ToLower().Contains(search) ||
                o.User.Email.ToLower().Contains(search));
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("주문번호,상태,고객명,이메일,상품수,총액,배송비,할인,메모,주문일");

        foreach (var o in orders)
        {
            var note = o.Note?.Replace("\"", "\"\"").Replace(",", " ") ?? "";
            sb.AppendLine($"{o.OrderNumber},{o.Status},{o.User.Name},{o.User.Email},{o.Items.Count},{o.TotalAmount},{o.ShippingFee},{o.DiscountAmount},\"{note}\",{o.CreatedAt:yyyy-MM-dd HH:mm}");
        }

        return Result<string>.Success(sb.ToString());
    }
}
