namespace Shop.Application.Common.Interfaces;

public interface IShippingTracker
{
    Task<ShippingTrackingResult> GetTrackingInfo(string carrier, string trackingNumber, CancellationToken ct = default);
}

public record ShippingTrackingResult(
    bool IsSuccess,
    string? CurrentStatus,
    List<TrackingEvent>? Events,
    string? Error);

public record TrackingEvent(
    DateTime Time,
    string Status,
    string Location,
    string Description);
