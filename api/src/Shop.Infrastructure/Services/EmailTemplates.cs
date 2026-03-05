namespace Shop.Infrastructure.Services;

public static class EmailTemplates
{
    private static string WrapLayout(string title, string content, string primaryColor = "#D4AF37")
    {
        return $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin:0;padding:0;background:#f5f5f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:0 auto;background:#ffffff;"">
  <tr><td style=""background:{primaryColor};padding:24px 32px;"">
    <h1 style=""margin:0;color:#ffffff;font-size:20px;"">{title}</h1>
  </td></tr>
  <tr><td style=""padding:32px;"">{content}</td></tr>
  <tr><td style=""padding:16px 32px;background:#f9f9f9;text-align:center;font-size:12px;color:#999;"">
    본 메일은 발신 전용입니다.
  </td></tr>
</table>
</body>
</html>";
    }

    public static string OrderConfirmation(string orderNumber, string itemsSummary, string total)
    {
        var content = $@"
<h2 style=""color:#333;font-size:18px;"">주문이 확인되었습니다</h2>
<p style=""color:#666;line-height:1.6;"">주문번호: <strong>{orderNumber}</strong></p>
<div style=""background:#f9f9f9;padding:16px;border-radius:8px;margin:16px 0;"">
  {itemsSummary}
</div>
<p style=""font-size:18px;font-weight:bold;color:#333;"">결제 금액: {total}</p>
<p style=""color:#666;line-height:1.6;"">상품 준비 후 발송해 드리겠습니다. 감사합니다.</p>";

        return WrapLayout("주문 확인", content);
    }

    public static string ShippingNotification(string orderNumber, string? carrier, string? trackingNumber)
    {
        var trackingInfo = !string.IsNullOrEmpty(trackingNumber)
            ? $"<p style=\"color:#666;\">택배사: {carrier ?? "택배"}<br/>운송장번호: <strong>{trackingNumber}</strong></p>"
            : "";

        var content = $@"
<h2 style=""color:#333;font-size:18px;"">상품이 발송되었습니다</h2>
<p style=""color:#666;line-height:1.6;"">주문번호: <strong>{orderNumber}</strong></p>
{trackingInfo}
<p style=""color:#666;line-height:1.6;"">배송 완료까지 2~3일 정도 소요됩니다.</p>";

        return WrapLayout("배송 시작", content);
    }

    public static string MarketingBroadcast(string title, string content)
    {
        var body = $@"
<h2 style=""color:#333;font-size:18px;"">{title}</h2>
<div style=""color:#666;line-height:1.8;"">{content}</div>";

        return WrapLayout(title, body);
    }

    public static string WelcomeEmail(string name, string storeName)
    {
        var content = $@"
<h2 style=""color:#333;font-size:18px;"">Welcome to {storeName}!</h2>
<p style=""color:#666;line-height:1.6;"">Hello <strong>{name}</strong>, thank you for joining us.</p>
<p style=""color:#666;line-height:1.6;"">Start exploring our products and enjoy your shopping experience.</p>
<p style=""color:#666;line-height:1.6;"">If you have any questions, feel free to contact us anytime.</p>";
        return WrapLayout("Welcome", content);
    }

    public static string SettlementCompleted(
        string tenantName,
        DateTime periodStart,
        DateTime periodEnd,
        int orderCount,
        decimal totalOrderAmount,
        decimal totalCommission,
        decimal totalSettlementAmount,
        string? transactionId)
    {
        var content = $@"
<h2 style=""color:#333;font-size:18px;"">정산이 완료되었습니다</h2>
<p style=""color:#666;line-height:1.6;"">안녕하세요, <strong>{tenantName}</strong> 관리자님.</p>
<p style=""color:#666;line-height:1.6;"">아래 기간의 정산이 완료되어 안내드립니다.</p>

<table style=""width:100%;border-collapse:collapse;margin:16px 0;"">
  <tr style=""background:#f9f9f9;"">
    <td style=""padding:12px;border:1px solid #eee;color:#666;"">정산 기간</td>
    <td style=""padding:12px;border:1px solid #eee;font-weight:bold;"">{periodStart:yyyy-MM-dd} ~ {periodEnd:yyyy-MM-dd}</td>
  </tr>
  <tr>
    <td style=""padding:12px;border:1px solid #eee;color:#666;"">주문 건수</td>
    <td style=""padding:12px;border:1px solid #eee;font-weight:bold;"">{orderCount}건</td>
  </tr>
  <tr style=""background:#f9f9f9;"">
    <td style=""padding:12px;border:1px solid #eee;color:#666;"">총 주문 금액</td>
    <td style=""padding:12px;border:1px solid #eee;font-weight:bold;"">{totalOrderAmount:N0}원</td>
  </tr>
  <tr>
    <td style=""padding:12px;border:1px solid #eee;color:#666;"">수수료</td>
    <td style=""padding:12px;border:1px solid #eee;color:#e74c3c;"">-{totalCommission:N0}원</td>
  </tr>
  <tr style=""background:#e8f5e9;"">
    <td style=""padding:12px;border:1px solid #eee;color:#333;font-weight:bold;"">정산 금액</td>
    <td style=""padding:12px;border:1px solid #eee;font-weight:bold;color:#2e7d32;font-size:18px;"">{totalSettlementAmount:N0}원</td>
  </tr>
</table>

{(transactionId != null ? $"<p style=\"color:#666;\">이체 번호: <strong>{transactionId}</strong></p>" : "")}
<p style=""color:#999;font-size:13px;"">입금까지 영업일 기준 1~2일 소요될 수 있습니다.</p>";

        return WrapLayout("정산 완료 알림", content, "#2e7d32");
    }
}
