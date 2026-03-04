using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Jobs;
using Shop.Infrastructure.Services;

namespace Shop.Tests.Unit.Services;

public class MesInventorySyncJobTests
{
    private readonly Mock<ILogger<MesInventorySyncJob>> _loggerMock;
    private readonly IConfiguration _configEnabled;
    private readonly IConfiguration _configDisabled;

    public MesInventorySyncJobTests()
    {
        _loggerMock = new Mock<ILogger<MesInventorySyncJob>>();

        _configEnabled = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mes:Enabled"] = "true",
                ["Mes:SyncIntervalMinutes"] = "60",
                ["Mes:SyncRetryCount"] = "1",
                ["Mes:SyncRetryDelayMs"] = "10"
            })
            .Build();

        _configDisabled = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mes:Enabled"] = "false"
            })
            .Build();
    }

    private static string DbName() => "TestSync_" + Guid.NewGuid().ToString("N");

    private (ServiceProvider sp, string dbName, Mock<IMesClient> mesClient, Mock<IMesProductMapper> mapper, Mock<IAdminDashboardNotifier> notifier) CreateServiceProvider(string? dbName = null)
    {
        dbName ??= DbName();
        var services = new ServiceCollection();

        var tenantContext = new TenantContext();
        services.AddSingleton<ITenantContext>(tenantContext);

        services.AddDbContext<ShopDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));

        services.AddDistributedMemoryCache();

        var mesClient = new Mock<IMesClient>();
        var mapper = new Mock<IMesProductMapper>();
        var notifier = new Mock<IAdminDashboardNotifier>();

        services.AddScoped(_ => mesClient.Object);
        services.AddScoped(_ => mapper.Object);
        services.AddScoped(_ => notifier.Object);

        var sp = services.BuildServiceProvider();
        return (sp, dbName, mesClient, mapper, notifier);
    }

    private static void SeedData(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.Database.EnsureCreated();
        db.Tenants.Add(new Tenant { Id = 1, Slug = "test", Name = "Test", IsActive = true });
        db.Products.Add(new Product { Id = 1, TenantId = 1, Name = "P1", Slug = "p1", Price = 100, CategoryId = 0, IsActive = true });
        db.ProductVariants.Add(new ProductVariant { Id = 1, ProductId = 1, Name = "Default", Stock = 50, Price = 0, IsActive = true });
        db.SaveChanges();
    }

    [Fact]
    public async Task Execute_WhenMesDisabled_SkipsGracefully()
    {
        // Arrange
        var (sp, _, mesClient, _, _) = CreateServiceProvider();
        var job = new MesInventorySyncJob(sp, _configDisabled, _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Act
        await job.StartAsync(cts.Token);
        await Task.Delay(500);
        await job.StopAsync(CancellationToken.None);

        // Assert
        mesClient.Verify(x => x.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("disabled")),
                null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_SuccessfulSync_SavesHistory()
    {
        // Arrange
        var (sp, dbName, mesClient, mapper, notifier) = CreateServiceProvider();
        SeedData(sp);

        mesClient.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mesClient.Setup(x => x.GetInventoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MesInventoryItem>
        {
            new("MES-001", "P1", 100, 0, "WH1", "Warehouse 1", DateTime.UtcNow)
        });
        mapper.Setup(x => x.GetShopProductIdAsync("MES-001", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        notifier.Setup(x => x.NotifyMesSyncCompleted(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = new MesInventorySyncJob(sp, _configEnabled, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        await job.StartAsync(cts.Token);
        // Wait for the first sync cycle to complete
        await Task.Delay(3000);
        await job.StopAsync(CancellationToken.None);

        // Assert
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var histories = await db.MesSyncHistories.ToListAsync();
        histories.Should().NotBeEmpty();
        histories.Last().Status.Should().Be("Completed");
        histories.Last().SuccessCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Execute_PartialFailure_RetriesAndRecords()
    {
        // Arrange
        var (sp, _, mesClient, mapper, notifier) = CreateServiceProvider();
        SeedData(sp);

        mesClient.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mesClient.Setup(x => x.GetInventoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MesInventoryItem>
        {
            new("MES-FAIL", "P_Unknown", 100, 0, "WH1", "Warehouse 1", DateTime.UtcNow)
        });

        // Map to a nonexistent product to trigger skip
        mapper.Setup(x => x.GetShopProductIdAsync("MES-FAIL", It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        notifier.Setup(x => x.NotifyMesSyncCompleted(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = new MesInventorySyncJob(sp, _configEnabled, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        await job.StartAsync(cts.Token);
        await Task.Delay(3000);
        await job.StopAsync(CancellationToken.None);

        // Assert
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var histories = await db.MesSyncHistories.ToListAsync();
        histories.Should().NotBeEmpty();
        histories.Last().SkippedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Execute_ConflictDetected_LogsConflict()
    {
        // Arrange
        var (sp, _, mesClient, mapper, notifier) = CreateServiceProvider();
        SeedData(sp);

        // Set a snapshot that differs from current stock (50) to trigger conflict
        using (var scope = sp.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
            await cache.SetStringAsync("mes:lastStockSnapshot:1", "30");
        }

        mesClient.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mesClient.Setup(x => x.GetInventoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MesInventoryItem>
        {
            new("MES-001", "P1", 100, 0, "WH1", "Warehouse 1", DateTime.UtcNow)
        });
        mapper.Setup(x => x.GetShopProductIdAsync("MES-001", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        notifier.Setup(x => x.NotifyMesSyncCompleted(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = new MesInventorySyncJob(sp, _configEnabled, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        await job.StartAsync(cts.Token);
        await Task.Delay(3000);
        await job.StopAsync(CancellationToken.None);

        // Assert
        using var scope2 = sp.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<ShopDbContext>();
        var history = await db.MesSyncHistories.OrderByDescending(h => h.Id).FirstOrDefaultAsync();
        history.Should().NotBeNull();
        history!.ConflictDetailsJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Execute_SendsSignalRNotification()
    {
        // Arrange
        var (sp, _, mesClient, mapper, notifier) = CreateServiceProvider();
        SeedData(sp);

        mesClient.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mesClient.Setup(x => x.GetInventoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MesInventoryItem>
        {
            new("MES-001", "P1", 100, 0, "WH1", "Warehouse 1", DateTime.UtcNow)
        });
        mapper.Setup(x => x.GetShopProductIdAsync("MES-001", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        notifier.Setup(x => x.NotifyMesSyncCompleted(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = new MesInventorySyncJob(sp, _configEnabled, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        await job.StartAsync(cts.Token);
        await Task.Delay(3000);
        await job.StopAsync(CancellationToken.None);

        // Assert
        notifier.Verify(x => x.NotifyMesSyncCompleted(
            0, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
