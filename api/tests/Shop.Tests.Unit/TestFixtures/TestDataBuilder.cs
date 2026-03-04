using Shop.Domain.Entities;

namespace Shop.Tests.Unit.TestFixtures;

public static class TestDataBuilder
{
    public static Tenant CreateTenant(int id = 1, string slug = "catholia", string name = "Catholia", bool isActive = true)
    {
        return new Tenant
        {
            Id = id,
            Slug = slug,
            Name = name,
            IsActive = isActive,
            Subdomain = slug,
            CustomDomain = null
        };
    }

    public static User CreateUser(
        int id = 1,
        int tenantId = 1,
        string username = "testuser",
        string email = "test@test.com",
        string role = "Admin",
        string name = "Test User")
    {
        return new User
        {
            Id = id,
            TenantId = tenantId,
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Name = name,
            Role = role,
            IsActive = true
        };
    }

    public static Product CreateProduct(
        int id = 1,
        int tenantId = 1,
        string name = "Test Product",
        decimal price = 10000m,
        int categoryId = 1,
        bool isActive = true)
    {
        return new Product
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            Price = price,
            CategoryId = categoryId,
            IsActive = isActive
        };
    }

    public static ProductVariant CreateVariant(
        int id = 1,
        int productId = 1,
        int stock = 100,
        bool isActive = true)
    {
        return new ProductVariant
        {
            Id = id,
            ProductId = productId,
            Stock = stock,
            IsActive = isActive,
            Name = "Default",
            Price = 0
        };
    }

    public static Category CreateCategory(int id = 1, int tenantId = 1, string name = "Test Category")
    {
        return new Category
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            IsActive = true
        };
    }

    public static Order CreateOrder(
        int id = 1,
        int tenantId = 1,
        int userId = 1,
        DateTime? createdAt = null)
    {
        return new Order
        {
            Id = id,
            TenantId = tenantId,
            UserId = userId,
            OrderNumber = $"ORD-{id:D6}",
            Status = "Confirmed",
            TotalAmount = 10000m,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    public static OrderItem CreateOrderItem(
        int id = 1,
        int orderId = 1,
        int productId = 1,
        int quantity = 5)
    {
        return new OrderItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = 10000m
        };
    }

    public static ForecastAccuracy CreateForecastAccuracy(
        int id = 1,
        int productId = 1,
        int tenantId = 1,
        DateTime? forecastDate = null,
        DateTime? targetDate = null,
        double predictedQuantity = 10,
        double? actualQuantity = null)
    {
        return new ForecastAccuracy
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            ForecastDate = forecastDate ?? DateTime.UtcNow.AddDays(-7),
            TargetDate = targetDate ?? DateTime.UtcNow.AddDays(-1),
            PredictedQuantity = predictedQuantity,
            ActualQuantity = actualQuantity,
            AbsoluteError = actualQuantity.HasValue ? Math.Abs(predictedQuantity - actualQuantity.Value) : null,
            PercentageError = actualQuantity.HasValue && actualQuantity.Value > 0
                ? Math.Abs(predictedQuantity - actualQuantity.Value) / actualQuantity.Value * 100
                : null
        };
    }
}
