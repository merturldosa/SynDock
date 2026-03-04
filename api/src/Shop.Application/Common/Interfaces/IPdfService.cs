using Shop.Application.Common.DTOs;

namespace Shop.Application.Common.Interfaces;

public interface IPdfService
{
    byte[] GenerateOrderReceipt(OrderDto order, string tenantName, string? tenantLogoUrl = null);
}
