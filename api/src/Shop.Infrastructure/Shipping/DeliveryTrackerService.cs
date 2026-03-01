using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Shipping;

public class DeliveryTrackerService : IShippingTracker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeliveryTrackerService> _logger;
    private const string GraphQLEndpoint = "https://apis.tracker.delivery/graphql";

    private static readonly Dictionary<string, string> CarrierCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CJ대한통운"] = "kr.cjlogistics",
        ["우체국택배"] = "kr.epost",
        ["한진택배"] = "kr.hanjin",
        ["로젠택배"] = "kr.logen",
        ["롯데택배"] = "kr.lotte",
        // Direct codes also supported
        ["kr.cjlogistics"] = "kr.cjlogistics",
        ["kr.epost"] = "kr.epost",
        ["kr.hanjin"] = "kr.hanjin",
        ["kr.logen"] = "kr.logen",
        ["kr.lotte"] = "kr.lotte",
    };

    public DeliveryTrackerService(HttpClient httpClient, ILogger<DeliveryTrackerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ShippingTrackingResult> GetTrackingInfo(string carrier, string trackingNumber, CancellationToken ct = default)
    {
        try
        {
            var carrierId = ResolveCarrierCode(carrier);
            if (carrierId is null)
                return new ShippingTrackingResult(false, null, null, $"지원하지 않는 택배사입니다: {carrier}");

            var query = @"
                query Track($carrierId: ID!, $trackingNumber: String!) {
                    track(carrierId: $carrierId, trackingNumber: $trackingNumber) {
                        lastEvent {
                            time
                            status { code name }
                            description
                        }
                        events {
                            edges {
                                node {
                                    time
                                    status { code name }
                                    description
                                }
                            }
                        }
                    }
                }";

            var requestBody = new
            {
                query,
                variables = new { carrierId, trackingNumber }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(GraphQLEndpoint, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Delivery Tracker API error: {StatusCode} {Body}", response.StatusCode, responseBody);
                return new ShippingTrackingResult(false, null, null, "배송 조회에 실패했습니다.");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Check for GraphQL errors
            if (root.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
            {
                var errorMsg = errors[0].TryGetProperty("message", out var msg) ? msg.GetString() : "배송 조회 오류";
                return new ShippingTrackingResult(false, null, null, errorMsg);
            }

            var track = root.GetProperty("data").GetProperty("track");

            // Parse current status
            string? currentStatus = null;
            if (track.TryGetProperty("lastEvent", out var lastEvent) && lastEvent.ValueKind != JsonValueKind.Null)
            {
                currentStatus = lastEvent.GetProperty("status").GetProperty("name").GetString();
            }

            // Parse events
            var events = new List<TrackingEvent>();
            if (track.TryGetProperty("events", out var eventsObj) &&
                eventsObj.TryGetProperty("edges", out var edges))
            {
                foreach (var edge in edges.EnumerateArray())
                {
                    var node = edge.GetProperty("node");
                    var time = DateTime.Parse(node.GetProperty("time").GetString()!);
                    var status = node.GetProperty("status").GetProperty("name").GetString() ?? "";
                    var description = node.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";

                    events.Add(new TrackingEvent(time, status, "", description));
                }
            }

            return new ShippingTrackingResult(true, currentStatus, events, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delivery tracking exception for {Carrier} {TrackingNumber}", carrier, trackingNumber);
            return new ShippingTrackingResult(false, null, null, $"배송 조회 중 오류: {ex.Message}");
        }
    }

    private static string? ResolveCarrierCode(string carrier)
    {
        return CarrierCodeMap.TryGetValue(carrier, out var code) ? code : null;
    }

    public static bool IsDelivered(string? statusName)
    {
        if (statusName is null) return false;
        return statusName.Contains("배달완료") || statusName.Contains("수령") || statusName.Contains("Delivered");
    }
}
