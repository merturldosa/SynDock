using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IScmService
{
    // Suppliers
    Task<List<Supplier>> GetSuppliersAsync(int tenantId, string? status = null, CancellationToken ct = default);
    Task<Supplier?> GetSupplierAsync(int tenantId, int supplierId, CancellationToken ct = default);
    Task<Supplier> CreateSupplierAsync(int tenantId, string name, string code, string? contactName, string? email, string? phone, string? address, string? businessNumber, int leadTimeDays, string createdBy, CancellationToken ct = default);
    Task UpdateSupplierAsync(int tenantId, int supplierId, string? status, string? grade, string? notes, string updatedBy, CancellationToken ct = default);

    // Procurement Orders
    Task<ProcurementOrder> CreateProcurementOrderAsync(int tenantId, int supplierId, List<(int productId, string productName, int quantity, decimal unitPrice)> items, DateTime? expectedDelivery, string? notes, string createdBy, CancellationToken ct = default);
    Task<ProcurementOrder?> GetProcurementOrderAsync(int tenantId, int orderId, CancellationToken ct = default);
    Task<(List<ProcurementOrder> Items, int TotalCount)> GetProcurementOrdersAsync(int tenantId, string? status = null, int? supplierId = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task SubmitProcurementOrderAsync(int tenantId, int orderId, string updatedBy, CancellationToken ct = default);
    Task ConfirmProcurementOrderAsync(int tenantId, int orderId, string updatedBy, CancellationToken ct = default);
    Task MarkShippedAsync(int tenantId, int orderId, string? trackingNumber, string updatedBy, CancellationToken ct = default);
    Task MarkDeliveredAsync(int tenantId, int orderId, string updatedBy, CancellationToken ct = default);

    // Supplier Evaluation
    Task<SupplierEvaluation> EvaluateSupplierAsync(int tenantId, int supplierId, string period, int qualityScore, int deliveryScore, int priceScore, int serviceScore, string? comments, string createdBy, CancellationToken ct = default);
    Task<List<SupplierEvaluation>> GetEvaluationsAsync(int tenantId, int? supplierId = null, CancellationToken ct = default);

    // Analytics
    Task<object> GetScmDashboardAsync(int tenantId, CancellationToken ct = default);
    Task<object> GetLeadTimeAnalysisAsync(int tenantId, CancellationToken ct = default);
}
