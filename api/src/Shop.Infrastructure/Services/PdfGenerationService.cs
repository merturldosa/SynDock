using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class PdfGenerationService : IPdfService
{
    public byte[] GenerateOrderReceipt(OrderDto order, string tenantName, string? tenantLogoUrl = null)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, order, tenantName));
                page.Content().Element(c => ComposeContent(c, order));
                page.Footer().Element(c => ComposeFooter(c, tenantName));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, OrderDto order, string tenantName)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(tenantName).FontSize(18).Bold().FontColor("#333333");
                    left.Item().PaddingTop(4).Text("주문 영수증 / Order Receipt").FontSize(9).FontColor("#888888");
                });
                row.ConstantItem(160).AlignRight().Column(right =>
                {
                    right.Item().Text($"#{order.OrderNumber}").FontSize(12).Bold().FontColor("#333333");
                    right.Item().PaddingTop(2).Text(order.CreatedAt.ToString("yyyy-MM-dd HH:mm")).FontSize(9).FontColor("#888888");
                    right.Item().PaddingTop(2).Text(text =>
                    {
                        text.Span("상태: ").FontSize(9).FontColor("#888888");
                        text.Span(TranslateStatus(order.Status)).FontSize(9).Bold().FontColor(GetStatusColor(order.Status));
                    });
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#DDDDDD");
        });
    }

    private static void ComposeContent(IContainer container, OrderDto order)
    {
        container.PaddingTop(15).Column(col =>
        {
            // Shipping Address
            if (order.ShippingAddress is not null)
            {
                col.Item().Element(c => ComposeShippingAddress(c, order.ShippingAddress));
                col.Item().PaddingTop(15);
            }

            // Order Items Table
            col.Item().Element(c => ComposeItemsTable(c, order));

            // Price Summary
            col.Item().PaddingTop(15).Element(c => ComposePriceSummary(c, order));

            // Note
            if (!string.IsNullOrWhiteSpace(order.Note))
            {
                col.Item().PaddingTop(15).Background("#F9FAFB").Padding(10).Column(noteCol =>
                {
                    noteCol.Item().Text("주문 메모").FontSize(9).Bold().FontColor("#555555");
                    noteCol.Item().PaddingTop(4).Text(order.Note).FontSize(9).FontColor("#666666");
                });
            }
        });
    }

    private static void ComposeShippingAddress(IContainer container, AddressDto address)
    {
        container.Background("#F9FAFB").Padding(12).Column(col =>
        {
            col.Item().Text("배송지 정보").FontSize(10).Bold().FontColor("#333333");
            col.Item().PaddingTop(6).Text($"{address.RecipientName}  |  {address.Phone}").FontSize(9).FontColor("#555555");
            col.Item().PaddingTop(2).Text($"({address.ZipCode}) {address.Address1}").FontSize(9).FontColor("#555555");
            if (!string.IsNullOrWhiteSpace(address.Address2))
                col.Item().Text(address.Address2).FontSize(9).FontColor("#555555");
        });
    }

    private static void ComposeItemsTable(IContainer container, OrderDto order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);   // Product name
                columns.ConstantColumn(60);  // Variant
                columns.ConstantColumn(50);  // Qty
                columns.ConstantColumn(80);  // Unit price
                columns.ConstantColumn(90);  // Total
            });

            // Header
            table.Header(header =>
            {
                header.Cell().BorderBottom(1).BorderColor("#DDDDDD").Padding(6)
                    .Text("상품명").FontSize(9).Bold().FontColor("#555555");
                header.Cell().BorderBottom(1).BorderColor("#DDDDDD").Padding(6)
                    .Text("옵션").FontSize(9).Bold().FontColor("#555555");
                header.Cell().BorderBottom(1).BorderColor("#DDDDDD").Padding(6).AlignCenter()
                    .Text("수량").FontSize(9).Bold().FontColor("#555555");
                header.Cell().BorderBottom(1).BorderColor("#DDDDDD").Padding(6).AlignRight()
                    .Text("단가").FontSize(9).Bold().FontColor("#555555");
                header.Cell().BorderBottom(1).BorderColor("#DDDDDD").Padding(6).AlignRight()
                    .Text("합계").FontSize(9).Bold().FontColor("#555555");
            });

            // Rows
            foreach (var item in order.Items)
            {
                table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(6)
                    .Text(item.ProductName).FontSize(9).FontColor("#333333");
                table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(6)
                    .Text(item.VariantName ?? "-").FontSize(9).FontColor("#666666");
                table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(6).AlignCenter()
                    .Text(item.Quantity.ToString()).FontSize(9).FontColor("#333333");
                table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(6).AlignRight()
                    .Text(FormatPrice(item.UnitPrice)).FontSize(9).FontColor("#333333");
                table.Cell().BorderBottom(1).BorderColor("#F3F4F6").Padding(6).AlignRight()
                    .Text(FormatPrice(item.TotalPrice)).FontSize(9).Bold().FontColor("#333333");
            }
        });
    }

    private static void ComposePriceSummary(IContainer container, OrderDto order)
    {
        var subtotal = order.Items.Sum(i => i.TotalPrice);

        container.AlignRight().Width(250).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("상품 합계").FontSize(9).FontColor("#666666");
                row.ConstantItem(100).AlignRight().Text(FormatPrice(subtotal)).FontSize(9).FontColor("#333333");
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("배송비").FontSize(9).FontColor("#666666");
                row.ConstantItem(100).AlignRight().Text(
                    order.ShippingFee == 0 ? "무료" : FormatPrice(order.ShippingFee)
                ).FontSize(9).FontColor("#333333");
            });

            if (order.DiscountAmount > 0)
            {
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("할인").FontSize(9).FontColor("#E53E3E");
                    row.ConstantItem(100).AlignRight().Text($"-{FormatPrice(order.DiscountAmount)}").FontSize(9).FontColor("#E53E3E");
                });
            }

            if (order.PointsUsed > 0)
            {
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("포인트 사용").FontSize(9).FontColor("#E53E3E");
                    row.ConstantItem(100).AlignRight().Text($"-{FormatPrice(order.PointsUsed)}").FontSize(9).FontColor("#E53E3E");
                });
            }

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#DDDDDD");
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text("총 결제금액").FontSize(12).Bold().FontColor("#333333");
                row.ConstantItem(100).AlignRight().Text(FormatPrice(order.TotalAmount)).FontSize(12).Bold().FontColor("#2563EB");
            });
        });
    }

    private static void ComposeFooter(IContainer container, string tenantName)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#EEEEEE");
            col.Item().PaddingTop(8).AlignCenter()
                .Text($"© {DateTime.UtcNow.Year} {tenantName}. All rights reserved.")
                .FontSize(8).FontColor("#AAAAAA");
        });
    }

    private static string FormatPrice(decimal amount)
        => amount.ToString("N0") + "원";

    private static string TranslateStatus(string status) => status switch
    {
        "Pending" => "주문 대기",
        "Confirmed" => "주문 확인",
        "Processing" => "처리 중",
        "Shipped" => "배송 중",
        "Delivered" => "배송 완료",
        "Cancelled" => "취소됨",
        "Refunded" => "환불됨",
        _ => status
    };

    private static string GetStatusColor(string status) => status switch
    {
        "Confirmed" or "Delivered" => "#16A34A",
        "Shipped" or "Processing" => "#2563EB",
        "Cancelled" or "Refunded" => "#DC2626",
        _ => "#666666"
    };
}
