namespace Shop.Application.Common.Interfaces;

public interface IKakaoAlimtalkService
{
    Task<bool> SendOrderConfirmedAsync(string phoneNumber, string orderNumber, decimal totalAmount, CancellationToken ct = default);
    Task<bool> SendShippedAsync(string phoneNumber, string orderNumber, string? trackingCarrier, string? trackingNumber, CancellationToken ct = default);
    Task<bool> SendDeliveredAsync(string phoneNumber, string orderNumber, CancellationToken ct = default);
    Task<bool> SendAsync(string phoneNumber, string templateCode, Dictionary<string, string> variables, CancellationToken ct = default);
}
