using FluentAssertions;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using Shop.Tests.Unit.TestFixtures;

namespace Shop.Tests.Unit.Services;

public class DomainEntityTests
{
    [Fact]
    public void Order_DefaultStatus_IsPending()
    {
        var order = new Order();
        order.Status.Should().Be(nameof(OrderStatus.Pending));
    }

    [Fact]
    public void Order_DefaultCollections_AreEmpty()
    {
        var order = new Order();
        order.Items.Should().BeEmpty();
        order.Payments.Should().BeEmpty();
        order.Histories.Should().BeEmpty();
    }

    [Fact]
    public void Coupon_DefaultDiscountType_IsFixed()
    {
        var coupon = new Coupon();
        coupon.DiscountType.Should().Be(nameof(CouponType.Fixed));
    }

    [Fact]
    public void Coupon_DefaultIsActive_IsTrue()
    {
        var coupon = new Coupon();
        coupon.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TestDataBuilder_CreateUser_SetsCorrectDefaults()
    {
        var user = TestDataBuilder.CreateUser();
        user.Id.Should().Be(1);
        user.TenantId.Should().Be(1);
        user.Username.Should().Be("testuser");
        user.Role.Should().Be("Admin");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TestDataBuilder_CreateUser_WithCustomRole()
    {
        var user = TestDataBuilder.CreateUser(role: "Member");
        user.Role.Should().Be("Member");
    }

    [Fact]
    public void TestDataBuilder_CreateProduct_SetsSlug()
    {
        var product = TestDataBuilder.CreateProduct(name: "Test Product");
        product.Slug.Should().Be("test-product");
    }

    [Fact]
    public void TestDataBuilder_CreateTenant_SetsSubdomain()
    {
        var tenant = TestDataBuilder.CreateTenant(slug: "mohyun");
        tenant.Subdomain.Should().Be("mohyun");
        tenant.Slug.Should().Be("mohyun");
    }

    [Fact]
    public void TestDataBuilder_CreateOrder_GeneratesOrderNumber()
    {
        var order = TestDataBuilder.CreateOrder(id: 42);
        order.OrderNumber.Should().Be("ORD-000042");
        order.Status.Should().Be("Confirmed");
    }

    [Fact]
    public void TestDataBuilder_CreateVariant_DefaultStock100()
    {
        var variant = TestDataBuilder.CreateVariant();
        variant.Stock.Should().Be(100);
        variant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Product_DefaultIsActive_IsTrue()
    {
        var product = TestDataBuilder.CreateProduct();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ForecastAccuracy_WithActualQuantity_CalculatesError()
    {
        var fa = TestDataBuilder.CreateForecastAccuracy(predictedQuantity: 100, actualQuantity: 80);
        fa.AbsoluteError.Should().Be(20);
        fa.PercentageError.Should().Be(25);
    }

    [Fact]
    public void ForecastAccuracy_WithoutActualQuantity_ErrorsAreNull()
    {
        var fa = TestDataBuilder.CreateForecastAccuracy(actualQuantity: null);
        fa.AbsoluteError.Should().BeNull();
        fa.PercentageError.Should().BeNull();
    }
}
