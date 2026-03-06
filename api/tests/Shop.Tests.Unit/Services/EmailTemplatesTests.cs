using FluentAssertions;
using Shop.Infrastructure.Services;

namespace Shop.Tests.Unit.Services;

public class EmailTemplatesTests
{
    [Fact]
    public void OrderConfirmation_ContainsOrderNumber()
    {
        var html = EmailTemplates.OrderConfirmation("ORD-20260306-ABC12345", "Test Product x 2", "20,000원");

        html.Should().Contain("ORD-20260306-ABC12345");
        html.Should().Contain("20,000원");
        html.Should().Contain("주문 확인");
    }

    [Fact]
    public void ShippingNotification_ContainsTrackingInfo()
    {
        var html = EmailTemplates.ShippingNotification("ORD-001", "CJ대한통운", "123456789");

        html.Should().Contain("ORD-001");
        html.Should().Contain("CJ대한통운");
        html.Should().Contain("123456789");
        html.Should().Contain("배송 시작");
    }

    [Fact]
    public void ShippingNotification_WithoutTracking_OmitsTrackingSection()
    {
        var html = EmailTemplates.ShippingNotification("ORD-001", null, null);

        html.Should().Contain("ORD-001");
        html.Should().NotContain("운송장번호");
    }

    [Fact]
    public void MarketingBroadcast_ContainsTitleAndContent()
    {
        var html = EmailTemplates.MarketingBroadcast("Big Sale", "50% off all items!");

        html.Should().Contain("Big Sale");
        html.Should().Contain("50% off all items!");
    }

    [Fact]
    public void WelcomeEmail_ContainsNameAndStoreName()
    {
        var html = EmailTemplates.WelcomeEmail("John", "Catholia");

        html.Should().Contain("John");
        html.Should().Contain("Catholia");
        html.Should().Contain("Welcome");
    }

    [Fact]
    public void SettlementCompleted_ContainsAllDetails()
    {
        var html = EmailTemplates.SettlementCompleted(
            "TestShop", DateTime.Parse("2026-03-01"), DateTime.Parse("2026-03-31"),
            150, 5000000m, 250000m, 4750000m, "TXN-12345");

        html.Should().Contain("TestShop");
        html.Should().Contain("150건");
        html.Should().Contain("5,000,000원");
        html.Should().Contain("250,000원");
        html.Should().Contain("4,750,000원");
        html.Should().Contain("TXN-12345");
        html.Should().Contain("정산 완료");
    }

    [Fact]
    public void SettlementCompleted_WithoutTransactionId_OmitsTransactionSection()
    {
        var html = EmailTemplates.SettlementCompleted(
            "TestShop", DateTime.Parse("2026-03-01"), DateTime.Parse("2026-03-31"),
            10, 100000m, 5000m, 95000m, null);

        html.Should().NotContain("이체 번호");
    }

    [Fact]
    public void AllTemplates_ProduceValidHtml()
    {
        var templates = new[]
        {
            EmailTemplates.OrderConfirmation("ORD-001", "item", "1000"),
            EmailTemplates.ShippingNotification("ORD-001", "DHL", "123"),
            EmailTemplates.MarketingBroadcast("Title", "Content"),
            EmailTemplates.WelcomeEmail("User", "Shop"),
        };

        foreach (var html in templates)
        {
            html.Should().StartWith("<!DOCTYPE html>");
            html.Should().Contain("</html>");
            html.Should().Contain("</body>");
        }
    }
}
