using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Products.Commands;

public record ExportProductsCommand : IRequest<Result<byte[]>>;

public class ExportProductsCommandHandler : IRequestHandler<ExportProductsCommand, Result<byte[]>>
{
    private readonly IShopDbContext _db;

    public ExportProductsCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<byte[]>> Handle(ExportProductsCommand request, CancellationToken cancellationToken)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Slug,CategoryName,Price,SalePrice,PriceType,Specification,IsFeatured,IsNew,IsActive,Description");

        foreach (var p in products)
        {
            sb.AppendLine(string.Join(",",
                p.Id,
                CsvEscape(p.Name),
                CsvEscape(p.Slug),
                CsvEscape(p.Category?.Name ?? ""),
                p.Price,
                p.SalePrice?.ToString() ?? "",
                CsvEscape(p.PriceType),
                CsvEscape(p.Specification ?? ""),
                p.IsFeatured,
                p.IsNew,
                p.IsActive,
                CsvEscape(p.Description ?? "")
            ));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Result<byte[]>.Success(bytes);
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
