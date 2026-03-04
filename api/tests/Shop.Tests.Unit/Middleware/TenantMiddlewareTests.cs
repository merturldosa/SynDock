using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shop.API.Middleware;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Services;

namespace Shop.Tests.Unit.Middleware;

public class TenantMiddlewareTests
{
    private readonly Mock<ILogger<TenantMiddleware>> _loggerMock;
    private readonly ShopDbContext _db;
    private readonly TenantContext _tenantContext;

    public TenantMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<TenantMiddleware>>();
        _tenantContext = new TenantContext();

        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase("TenantMiddlewareTests_" + Guid.NewGuid().ToString("N"))
            .Options;
        _db = new ShopDbContext(options, _tenantContext);

        // Seed tenants
        _db.Tenants.Add(new Tenant { Id = 1, Slug = "catholia", Name = "Catholia", IsActive = true, Subdomain = "catholia", CustomDomain = "catholia.com" });
        _db.Tenants.Add(new Tenant { Id = 2, Slug = "mohyun", Name = "MoHyun", IsActive = true, Subdomain = "mohyun" });
        _db.Tenants.Add(new Tenant { Id = 3, Slug = "inactive", Name = "Inactive", IsActive = false, Subdomain = "inactive" });
        _db.SaveChanges();
    }

    private (TenantMiddleware middleware, DefaultHttpContext context) CreateMiddleware(bool nextCalled = true)
    {
        var nextCallCount = 0;
        RequestDelegate next = _ =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        };

        var middleware = new TenantMiddleware(next, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        return (middleware, context);
    }

    [Fact]
    public async Task Invoke_WithTenantHeader_SetsTenantContext()
    {
        // Arrange
        var (middleware, context) = CreateMiddleware();
        context.Request.Path = "/api/products";
        context.Request.Headers["X-Tenant-Id"] = "catholia";

        // Act
        await middleware.InvokeAsync(context, _db, _tenantContext);

        // Assert
        _tenantContext.TenantId.Should().Be(1);
        _tenantContext.TenantSlug.Should().Be("catholia");
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Invoke_WithCustomDomain_ResolvesTenant()
    {
        // Arrange
        var (middleware, context) = CreateMiddleware();
        context.Request.Path = "/api/products";
        context.Request.Host = new HostString("catholia.com");

        // Act
        await middleware.InvokeAsync(context, _db, _tenantContext);

        // Assert
        _tenantContext.TenantId.Should().Be(1);
        _tenantContext.TenantSlug.Should().Be("catholia");
    }

    [Fact]
    public async Task Invoke_PlatformPath_SkipsTenantResolution()
    {
        // Arrange
        var (middleware, context) = CreateMiddleware();
        context.Request.Path = "/api/platform/tenants";

        // Act
        await middleware.InvokeAsync(context, _db, _tenantContext);

        // Assert - TenantContext should not be set
        _tenantContext.TenantId.Should().Be(0);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Invoke_MissingTenant_Returns404()
    {
        // Arrange
        var (middleware, context) = CreateMiddleware();
        context.Request.Path = "/api/products";
        context.Request.Headers["X-Tenant-Id"] = "nonexistent";

        // Act
        await middleware.InvokeAsync(context, _db, _tenantContext);

        // Assert
        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Invoke_InactiveTenant_Returns403()
    {
        // Arrange
        var (middleware, context) = CreateMiddleware();
        context.Request.Path = "/api/products";
        context.Request.Headers["X-Tenant-Id"] = "inactive";

        // Act
        await middleware.InvokeAsync(context, _db, _tenantContext);

        // Assert
        context.Response.StatusCode.Should().Be(403);
    }
}
