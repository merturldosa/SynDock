using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.Integration;

public class MesHttpClient : IMesClient
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MesHttpClient> _logger;
    private readonly bool _enabled;

    private const string TokenCacheKey = "mes:jwt:accessToken";
    private static readonly TimeSpan TokenCacheDuration = TimeSpan.FromMinutes(50);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MesHttpClient(
        HttpClient httpClient,
        IDistributedCache cache,
        ITenantContext tenantContext,
        IConfiguration configuration,
        ILogger<MesHttpClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;

        var mesMode = configuration["Mes:Enabled"]?.ToLower();
        _enabled = mesMode == "true";
        if (_enabled)
        {
            var baseUrl = configuration["Mes:BaseUrl"] ?? "http://localhost:8080";
            var timeout = configuration.GetValue<int>("Mes:TimeoutSeconds", 10);

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_enabled) return false;

        try
        {
            var response = await _httpClient.GetAsync("/api/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<MesInventoryItem>> GetInventoryAsync(CancellationToken ct = default)
    {
        if (!_enabled) return [];

        try
        {
            // Use shop-integration endpoint (aggregated by product, optimized for Shop)
            var response = await SendAuthenticatedAsync(HttpMethod.Get, "/api/shop-integration/inventory", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES inventory request failed: {StatusCode}", response.StatusCode);
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<MesApiResponse<List<MesInventoryResponse>>>(JsonOptions, ct);
            if (result?.Data is null) return [];

            return result.Data.Select(r => new MesInventoryItem(
                r.ProductCode ?? "",
                r.ProductName,
                r.AvailableQuantity,
                (int)(r.ReservedQuantity ?? 0),
                r.WarehouseCode,
                r.WarehouseName,
                r.LastTransactionDate
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MES inventory");
            return [];
        }
    }

    public async Task<MesSalesOrderResult> CreateSalesOrderAsync(MesSalesOrderRequest request, CancellationToken ct = default)
    {
        if (!_enabled)
            return new MesSalesOrderResult(false, null, "MES integration is disabled");

        try
        {
            // Use shop-integration endpoint (auto-creates + auto-confirms in one call)
            var payload = new
            {
                shopOrderNo = request.OrderNo,
                orderDate = request.OrderDate,
                customerId = request.CustomerId,
                salesUserId = request.SalesUserId,
                items = request.Items.Select(i => new
                {
                    productCode = "", // Will be resolved by MES side via product mapping
                    productName = $"Product-{i.ProductId}",
                    quantity = i.OrderedQuantity,
                    unit = i.Unit,
                    unitPrice = i.UnitPrice
                })
            };

            var response = await SendAuthenticatedAsync(HttpMethod.Post, "/api/shop-integration/orders", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES order creation failed: {StatusCode} {Body}", response.StatusCode, body);
                return new MesSalesOrderResult(false, null, $"HTTP {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<MesApiResponse<MesOrderCreated>>(body, JsonOptions);
            var orderId = result?.Data?.SalesOrderId;
            if (orderId is null)
                return new MesSalesOrderResult(false, null, "MES returned no order ID");

            return new MesSalesOrderResult(true, orderId.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MES sales order: {OrderNo}", request.OrderNo);
            return new MesSalesOrderResult(false, null, ex.Message);
        }
    }

    public async Task<MesSyncStatus> GetSyncStatusAsync(CancellationToken ct = default)
    {
        if (!_enabled)
            return new MesSyncStatus(false, null, 0, "MES integration is disabled");

        var isConnected = await IsAvailableAsync(ct);
        var lastSyncStr = await _cache.GetStringAsync("mes:lastSync", ct);
        var lastSync = lastSyncStr is not null ? DateTime.Parse(lastSyncStr) : (DateTime?)null;
        var syncedCountStr = await _cache.GetStringAsync("mes:syncedCount", ct);
        var syncedCount = syncedCountStr is not null ? int.Parse(syncedCountStr) : 0;

        return new MesSyncStatus(isConnected, lastSync, syncedCount, isConnected ? null : "MES 서버에 연결할 수 없습니다");
    }

    // ── Shop-MES Integration Bridge (v2 endpoints) ────────

    public async Task<MesReservationResult> ReserveInventoryAsync(MesReservationRequest request, CancellationToken ct = default)
    {
        if (!_enabled)
            return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);

        try
        {
            var payload = new
            {
                shopOrderNo = request.ShopOrderNo,
                requestId = request.RequestId,
                items = request.Items.Select(i => new { productCode = i.ProductCode, quantity = i.Quantity })
            };

            var response = await SendAuthenticatedAsync(HttpMethod.Post, "/api/shop-integration/inventory/reserve", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES inventory reserve failed: {StatusCode} {Body}", response.StatusCode, body);
                return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);
            }

            var result = JsonSerializer.Deserialize<MesApiResponse<MesReservationResultData>>(body, JsonOptions);
            if (result?.Data is null)
                return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);

            var items = result.Data.Items?.Select(i =>
                new MesReservationItemResult(i.ProductCode ?? "", i.Success, i.Message)).ToList() ?? [];

            return new MesReservationResult(request.ShopOrderNo, request.RequestId, result.Data.AllSuccess, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve MES inventory for order {ShopOrderNo}", request.ShopOrderNo);
            return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);
        }
    }

    public async Task<MesReservationResult> ReleaseInventoryAsync(MesReservationRequest request, CancellationToken ct = default)
    {
        if (!_enabled)
            return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);

        try
        {
            var payload = new
            {
                shopOrderNo = request.ShopOrderNo,
                requestId = request.RequestId,
                items = request.Items.Select(i => new { productCode = i.ProductCode, quantity = i.Quantity })
            };

            var response = await SendAuthenticatedAsync(HttpMethod.Post, "/api/shop-integration/inventory/release", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES inventory release failed: {StatusCode} {Body}", response.StatusCode, body);
                return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);
            }

            var result = JsonSerializer.Deserialize<MesApiResponse<MesReservationResultData>>(body, JsonOptions);
            if (result?.Data is null)
                return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);

            var items = result.Data.Items?.Select(i =>
                new MesReservationItemResult(i.ProductCode ?? "", i.Success, i.Message)).ToList() ?? [];

            return new MesReservationResult(request.ShopOrderNo, request.RequestId, result.Data.AllSuccess, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release MES inventory for order {ShopOrderNo}", request.ShopOrderNo);
            return new MesReservationResult(request.ShopOrderNo, request.RequestId, false, []);
        }
    }

    public async Task<MesOrderStatusResult?> GetOrderStatusAsync(string shopOrderNo, CancellationToken ct = default)
    {
        if (!_enabled) return null;

        try
        {
            var response = await SendAuthenticatedAsync(HttpMethod.Get, $"/api/shop-integration/orders/{shopOrderNo}/status", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES order status query failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MesApiResponse<MesOrderStatusData>>(JsonOptions, ct);
            if (result?.Data is null) return null;

            return new MesOrderStatusResult(
                result.Data.ShopOrderNo ?? shopOrderNo,
                result.Data.MesOrderId,
                result.Data.MesOrderNo,
                result.Data.Status,
                result.Data.OrderDate,
                result.Data.TotalAmount,
                result.Data.LastUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MES order status for {ShopOrderNo}", shopOrderNo);
            return null;
        }
    }

    public async Task<List<MesProductInfo>> GetMesProductsAsync(CancellationToken ct = default)
    {
        if (!_enabled) return [];

        try
        {
            var response = await SendAuthenticatedAsync(HttpMethod.Get, "/api/shop-integration/products", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES products request failed: {StatusCode}", response.StatusCode);
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<MesApiResponse<List<MesProductData>>>(JsonOptions, ct);
            if (result?.Data is null) return [];

            return result.Data.Select(p => new MesProductInfo(
                p.ProductId, p.ProductCode ?? "", p.ProductName, p.ProductType, p.Unit, p.IsActive ?? true
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MES products");
            return [];
        }
    }

    // ── JWT Authentication ──────────────────────────────────

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpMethod method, string url, object? body, CancellationToken ct)
    {
        var token = await EnsureAuthenticatedAsync(ct);
        var response = await SendWithTokenAsync(method, url, body, token, ct);

        // 401 → refresh token and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("MES token expired, refreshing...");
            await _cache.RemoveAsync(TokenCacheKey, ct);
            token = await EnsureAuthenticatedAsync(ct);
            response = await SendWithTokenAsync(method, url, body, token, ct);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpMethod method, string url, object? body, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Tenant-ID", _configuration["Mes:TenantId"] ?? "smartdocking");

        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        return await _httpClient.SendAsync(request, ct);
    }

    private async Task<string> EnsureAuthenticatedAsync(CancellationToken ct)
    {
        var cached = await _cache.GetStringAsync(TokenCacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
            return cached;

        var loginRequest = new
        {
            tenantId = _configuration["Mes:TenantId"] ?? "smartdocking",
            username = _configuration["Mes:Username"] ?? "admin",
            password = _configuration["Mes:Password"] ?? "admin123"
        };

        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MES login failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"MES login failed: HTTP {response.StatusCode}");
        }

        var result = JsonSerializer.Deserialize<MesApiResponse<MesLoginData>>(body, JsonOptions);
        var token = result?.Data?.AccessToken
            ?? throw new InvalidOperationException("MES login response missing accessToken");

        await _cache.SetStringAsync(TokenCacheKey, token,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TokenCacheDuration }, ct);

        _logger.LogInformation("MES JWT token obtained and cached");
        return token;
    }

    // ── MES Response DTOs ───────────────────────────────────

    private record MesApiResponse<T>(
        [property: JsonPropertyName("data")] T? Data,
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("timestamp")] string? Timestamp);

    private record MesLoginData(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("refreshToken")] string? RefreshToken,
        [property: JsonPropertyName("tokenType")] string? TokenType,
        [property: JsonPropertyName("expiresIn")] int? ExpiresIn);

    private record MesInventoryResponse(
        [property: JsonPropertyName("productId")] long? ProductId,
        [property: JsonPropertyName("productCode")] string? ProductCode,
        [property: JsonPropertyName("productName")] string? ProductName,
        [property: JsonPropertyName("availableQuantity")] decimal AvailableQuantity,
        [property: JsonPropertyName("reservedQuantity")] decimal? ReservedQuantity,
        [property: JsonPropertyName("warehouseCode")] string? WarehouseCode,
        [property: JsonPropertyName("warehouseName")] string? WarehouseName,
        [property: JsonPropertyName("lastTransactionDate")] DateTime? LastTransactionDate);

    private record MesOrderCreated(
        [property: JsonPropertyName("salesOrderId")] long? SalesOrderId,
        [property: JsonPropertyName("orderNo")] string? OrderNo,
        [property: JsonPropertyName("status")] string? Status);

    // v2 Shop-Integration response DTOs
    private record MesReservationResultData(
        [property: JsonPropertyName("shopOrderNo")] string? ShopOrderNo,
        [property: JsonPropertyName("requestId")] string? RequestId,
        [property: JsonPropertyName("allSuccess")] bool AllSuccess,
        [property: JsonPropertyName("items")] List<MesReservationItemData>? Items);

    private record MesReservationItemData(
        [property: JsonPropertyName("productCode")] string? ProductCode,
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message);

    private record MesOrderStatusData(
        [property: JsonPropertyName("shopOrderNo")] string? ShopOrderNo,
        [property: JsonPropertyName("mesOrderId")] long? MesOrderId,
        [property: JsonPropertyName("mesOrderNo")] string? MesOrderNo,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("orderDate")] DateTime? OrderDate,
        [property: JsonPropertyName("totalAmount")] decimal? TotalAmount,
        [property: JsonPropertyName("lastUpdated")] DateTime? LastUpdated);

    private record MesProductData(
        [property: JsonPropertyName("productId")] long ProductId,
        [property: JsonPropertyName("productCode")] string? ProductCode,
        [property: JsonPropertyName("productName")] string? ProductName,
        [property: JsonPropertyName("productType")] string? ProductType,
        [property: JsonPropertyName("unit")] string? Unit,
        [property: JsonPropertyName("isActive")] bool? IsActive);
}
