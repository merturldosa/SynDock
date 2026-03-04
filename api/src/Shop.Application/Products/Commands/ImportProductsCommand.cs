using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Products.Commands;

public record ImportProductsCommand(string CsvContent) : IRequest<Result<ImportResultDto>>;

public record ImportResultDto(int Created, int Updated, int Skipped, List<string> Errors);

public class ImportProductsCommandHandler : IRequestHandler<ImportProductsCommand, Result<ImportResultDto>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public ImportProductsCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ImportResultDto>> Handle(ImportProductsCommand request, CancellationToken cancellationToken)
    {
        var lines = request.CsvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return Result<ImportResultDto>.Failure("CSV must have a header and at least one data row");

        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoryMap = categories.ToDictionary(c => c.Name.ToLower(), c => c.Id);

        int created = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var fields = ParseCsvLine(line);
                if (fields.Count < 6)
                {
                    errors.Add($"Row {i + 1}: insufficient columns");
                    skipped++;
                    continue;
                }

                var name = fields[1];
                var slug = fields[2];
                var categoryName = fields[3];
                var price = decimal.Parse(fields[4], CultureInfo.InvariantCulture);
                var salePrice = string.IsNullOrEmpty(fields[5]) ? (decimal?)null : decimal.Parse(fields[5], CultureInfo.InvariantCulture);
                var priceType = fields.Count > 6 ? fields[6] : "Fixed";
                var specification = fields.Count > 7 ? fields[7] : null;
                var isFeatured = fields.Count > 8 && bool.TryParse(fields[8], out var f) && f;
                var isNew = fields.Count > 9 && bool.TryParse(fields[9], out var n) && n;
                var isActive = fields.Count <= 10 || !bool.TryParse(fields[10], out var a) || a;
                var description = fields.Count > 11 ? fields[11] : null;

                if (!categoryMap.TryGetValue(categoryName.ToLower(), out var categoryId))
                {
                    errors.Add($"Row {i + 1}: category '{categoryName}' not found");
                    skipped++;
                    continue;
                }

                var existing = await _db.Products.FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

                if (existing != null)
                {
                    existing.Name = name;
                    existing.Price = price;
                    existing.SalePrice = salePrice;
                    existing.PriceType = priceType;
                    existing.Specification = specification;
                    existing.CategoryId = categoryId;
                    existing.IsFeatured = isFeatured;
                    existing.IsNew = isNew;
                    existing.IsActive = isActive;
                    existing.Description = description;
                    updated++;
                }
                else
                {
                    _db.Products.Add(new Product
                    {
                        Name = name,
                        Slug = slug,
                        Price = price,
                        SalePrice = salePrice,
                        PriceType = priceType,
                        Specification = specification,
                        CategoryId = categoryId,
                        IsFeatured = isFeatured,
                        IsNew = isNew,
                        IsActive = isActive,
                        Description = description,
                        CreatedBy = "Import"
                    });
                    created++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Row {i + 1}: {ex.Message}");
                skipped++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ImportResultDto>.Success(new ImportResultDto(created, updated, skipped, errors));
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
