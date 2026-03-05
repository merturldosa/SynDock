import api from "./api";
import type { CategoryInfo } from "@/types/product";

// ── Admin Stats ──
export interface OrderStatusCount {
  status: string;
  count: number;
}

export interface RecentOrder {
  id: number;
  orderNumber: string;
  status: string;
  totalAmount: number;
  createdAt: string;
}

export interface TopProduct {
  productId: number;
  productName: string;
  imageUrl: string | null;
  orderCount: number;
  totalSales: number;
}

export interface CategorySales {
  categoryName: string;
  totalSales: number;
  orderCount: number;
}

export interface DashboardStats {
  totalProducts: number;
  totalCategories: number;
  totalOrders: number;
  totalRevenue: number;
  totalUsers: number;
  ordersByStatus: OrderStatusCount[];
  recentOrders: RecentOrder[];
  topProducts: TopProduct[];
  lowStockCount: number;
  todayOrders: number;
  todayRevenue: number;
  categorySales: CategorySales[];
}

export async function getDashboardStats(): Promise<DashboardStats> {
  const { data } = await api.get("/admin/stats");
  return data;
}

export interface DailySales {
  date: string;
  orderCount: number;
  revenue: number;
}

export interface SalesAnalytics {
  dailySales: DailySales[];
  totalRevenue: number;
  totalOrders: number;
  averageOrderValue: number;
  previousPeriodRevenue?: number | null;
  previousPeriodOrders?: number | null;
  revenueChangePercent?: number | null;
  ordersChangePercent?: number | null;
}

export async function getSalesAnalytics(
  days = 30,
  startDate?: string,
  endDate?: string,
  includeComparison = false
): Promise<SalesAnalytics> {
  const { data } = await api.get("/admin/analytics", {
    params: { days, startDate, endDate, includeComparison },
  });
  return data;
}

// ── Customer Analytics ──
export interface CustomerSegment {
  segment: string;
  count: number;
  totalSpent: number;
}

export interface SpendTier {
  tier: string;
  count: number;
  totalSpent: number;
}

export interface TopCustomer {
  userId: number;
  name: string;
  email: string;
  orderCount: number;
  totalSpent: number;
  lastOrderAt: string;
}

export interface CustomerAnalytics {
  totalCustomers: number;
  newCustomers30Days: number;
  returningCustomers: number;
  segments: CustomerSegment[];
  spendTiers: SpendTier[];
  topCustomers: TopCustomer[];
}

export async function getCustomerAnalytics(): Promise<CustomerAnalytics> {
  const { data } = await api.get("/admin/analytics/customers");
  return data;
}

// ── Product Performance ──
export interface ProductPerformance {
  productId: number;
  productName: string;
  imageUrl: string | null;
  categoryName: string;
  viewCount: number;
  orderCount: number;
  revenue: number;
  conversionRate: number;
  averageRating: number;
}

export interface ProductPerformanceResult {
  products: ProductPerformance[];
  totalProducts: number;
}

export async function getProductPerformance(
  sort = "revenue",
  page = 1,
  pageSize = 20
): Promise<ProductPerformanceResult> {
  const { data } = await api.get("/admin/analytics/products", {
    params: { sort, page, pageSize },
  });
  return data;
}

// ── Product Admin ──
export interface CreateProductRequest {
  name: string;
  slug: string;
  description?: string;
  specification?: string;
  categoryId: number;
  price: number;
  salePrice?: number;
  priceType: string;
  isActive: boolean;
  isFeatured: boolean;
  isNew: boolean;
  imageUrls?: string[];
}

export async function createProduct(
  body: CreateProductRequest
): Promise<{ productId: number }> {
  const payload = {
    ...body,
    images: (body.imageUrls || []).map((url, i) => ({
      url,
      altText: "",
      sortOrder: i,
      isPrimary: i === 0,
    })),
    variants: [],
  };
  const { data } = await api.post("/products", payload);
  return data;
}

export interface UpdateProductRequest {
  name?: string;
  slug?: string;
  description?: string;
  specification?: string;
  categoryId?: number;
  price?: number;
  salePrice?: number;
  priceType?: string;
  isActive?: boolean;
  isFeatured?: boolean;
  isNew?: boolean;
}

export async function updateProduct(
  id: number,
  body: UpdateProductRequest
): Promise<void> {
  await api.put(`/products/${id}`, body);
}

export async function deleteProduct(id: number): Promise<void> {
  await api.delete(`/products/${id}`);
}

// ── Category Admin ──
export interface CreateCategoryRequest {
  name: string;
  slug: string;
  description?: string;
  parentId?: number;
  sortOrder?: number;
}

export async function createCategory(
  body: CreateCategoryRequest
): Promise<{ categoryId: number }> {
  const { data } = await api.post("/categories", body);
  return data;
}

export async function updateCategory(
  id: number,
  body: Partial<CreateCategoryRequest>
): Promise<void> {
  await api.put(`/categories/${id}`, body);
}

export async function deleteCategory(id: number): Promise<void> {
  await api.delete(`/categories/${id}`);
}

// ── Order Admin ──
export interface OrderSummary {
  id: number;
  orderNumber: string;
  status: string;
  itemCount: number;
  totalAmount: number;
  firstProductName: string | null;
  firstProductImageUrl: string | null;
  createdAt: string;
}

export interface PagedOrders {
  items: OrderSummary[];
  totalCount: number;
}

export async function getAdminOrders(
  status?: string,
  page = 1,
  pageSize = 20
): Promise<PagedOrders> {
  const { data } = await api.get("/order", {
    params: { status, page, pageSize },
  });
  return data;
}

// ── Admin Order Search (new API) ──
export interface AdminOrderSummary {
  id: number;
  orderNumber: string;
  status: string;
  itemCount: number;
  totalAmount: number;
  firstProductName: string | null;
  firstProductImageUrl: string | null;
  customerName: string | null;
  customerEmail: string | null;
  createdAt: string;
}

export interface AdminPagedOrders {
  items: AdminOrderSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function getAdminOrdersSearch(
  status?: string,
  page = 1,
  pageSize = 20,
  search?: string
): Promise<AdminPagedOrders> {
  const { data } = await api.get("/admin/orders", {
    params: { status, page, pageSize, search },
  });
  return data;
}

export async function updateOrderStatus(
  id: number,
  status: string
): Promise<void> {
  await api.put(`/order/${id}/status`, { status });
}

export async function updateShippingInfo(
  id: number,
  trackingNumber: string,
  trackingCarrier?: string
): Promise<void> {
  await api.put(`/order/${id}/shipping`, { trackingNumber, trackingCarrier });
}

export async function bulkUpdateOrderStatus(
  orderIds: number[],
  status: string
): Promise<{ successCount: number; failCount: number; errors: string[] }> {
  const { data } = await api.put("/admin/orders/bulk-status", {
    orderIds,
    status,
  });
  return data;
}

// ── Inventory Admin ──
export interface LowStockItem {
  variantId: number;
  productId: number;
  productName: string;
  variantName: string;
  sku: string | null;
  stock: number;
  imageUrl: string | null;
}

export async function getLowStock(
  threshold = 10
): Promise<LowStockItem[]> {
  const { data } = await api.get("/admin/low-stock", {
    params: { threshold },
  });
  return data;
}

export async function updateStock(
  variantId: number,
  newStock: number
): Promise<void> {
  await api.put("/admin/stock", { variantId, newStock });
}

// ── User Admin ──
export interface UserSummary {
  id: number;
  username: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export async function getAdminUsers(): Promise<UserSummary[]> {
  const { data } = await api.get("/admin/users");
  return data;
}

export async function updateUser(id: number, role: string, isActive: boolean): Promise<void> {
  await api.put(`/admin/users/${id}`, { role, isActive });
}

// ── Product Variants ──
export interface ProductVariantDto {
  id?: number;
  name: string;
  sku?: string | null;
  price?: number | null;
  stock: number;
  sortOrder: number;
  isActive: boolean;
}

export async function getProductVariants(productId: number): Promise<ProductVariantDto[]> {
  const { data } = await api.get(`/products/${productId}/variants`);
  return data;
}

export async function updateProductVariants(productId: number, variants: ProductVariantDto[]): Promise<void> {
  await api.put(`/products/${productId}/variants`, { variants });
}

// ── Refund ──
export async function refundOrder(orderId: number, reason: string): Promise<void> {
  await api.post(`/payment/${orderId}/refund`, { reason });
}

// ── AI Content ──
export interface GeneratedContent {
  heroSection: string;
  featureSection: string;
  closingSection: string;
  fullDescription: string;
}

export async function generateProductContent(
  productId: number
): Promise<GeneratedContent> {
  const { data } = await api.post(`/products/${productId}/generate-content`);
  return data;
}

// ── Email Broadcast ──
export async function sendMarketingEmail(
  title: string,
  content: string,
  target: string
): Promise<{ sentCount: number }> {
  const { data } = await api.post("/admin/email/broadcast", {
    title,
    content,
    target,
  });
  return data;
}

// ── Email Campaigns ──
export interface CampaignDto {
  id: number;
  title: string;
  target: string;
  status: string;
  scheduledAt: string | null;
  sentAt: string | null;
  sentCount: number;
  failCount: number;
  createdAt: string;
}

export async function getCampaigns(): Promise<CampaignDto[]> {
  const { data } = await api.get("/admin/campaigns");
  return data;
}

export async function createCampaign(
  title: string,
  content: string,
  target: string,
  scheduledAt?: string
): Promise<{ campaignId: number }> {
  const { data } = await api.post("/admin/campaigns", { title, content, target, scheduledAt });
  return data;
}

export async function sendCampaign(id: number): Promise<{ sentCount: number }> {
  const { data } = await api.post(`/admin/campaigns/${id}/send`);
  return data;
}

// ── A/B Test Campaigns ──
export interface CampaignVariantDto {
  id: number;
  variantName: string;
  subjectLine: string;
  trafficPercent: number;
  sentCount: number;
  openCount: number;
  clickCount: number;
  conversionCount: number;
  revenue: number;
  openRate: number;
  clickRate: number;
  conversionRate: number;
  isWinner: boolean;
}

export interface CampaignAnalyticsDto {
  id: number;
  title: string;
  target: string;
  status: string;
  isAbTest: boolean;
  scheduledAt: string | null;
  sentAt: string | null;
  sentCount: number;
  openCount: number;
  clickCount: number;
  conversionCount: number;
  revenue: number;
  openRate: number;
  clickRate: number;
  conversionRate: number;
  variants: CampaignVariantDto[] | null;
}

export interface CampaignSummaryDto {
  totalCampaigns: number;
  sentCampaigns: number;
  totalSent: number;
  totalOpened: number;
  totalClicked: number;
  totalConverted: number;
  totalRevenue: number;
  avgOpenRate: number;
  avgClickRate: number;
  avgConversionRate: number;
}

export async function createAbTestCampaign(
  title: string, target: string, scheduledAt: string | undefined,
  subjectLineA: string, contentA: string,
  subjectLineB: string, contentB: string,
  trafficPercentA = 50
): Promise<{ campaignId: number }> {
  const { data } = await api.post("/admin/campaigns/ab-test", {
    title, target, scheduledAt, subjectLineA, contentA, subjectLineB, contentB, trafficPercentA
  });
  return data;
}

export async function getCampaignAnalytics(id: number): Promise<CampaignAnalyticsDto> {
  const { data } = await api.get(`/admin/campaigns/${id}/analytics`);
  return data;
}

export async function getCampaignSummary(): Promise<CampaignSummaryDto> {
  const { data } = await api.get("/admin/campaigns/summary");
  return data;
}

// ── Tenant Settings ──
export interface TenantSettingsTheme {
  primary: string | null;
  primaryLight: string | null;
  secondary: string | null;
  secondaryLight: string | null;
  background: string | null;
}

export interface TenantSettings {
  companyName: string | null;
  companyAddress: string | null;
  businessNumber: string | null;
  ceoName: string | null;
  contactPhone: string | null;
  contactEmail: string | null;
  heroSubtitle: string | null;
  heroTagline: string | null;
  heroDescription: string | null;
  theme: TenantSettingsTheme | null;
  logoUrl: string | null;
  faviconUrl: string | null;
}

export async function getTenantSettings(): Promise<TenantSettings> {
  const { data } = await api.get("/admin/settings");
  return data;
}

export async function updateTenantSettings(settings: Partial<TenantSettings>): Promise<void> {
  await api.put("/admin/settings", settings);
}

// ── Notifications ──
export async function broadcastNotification(
  title: string,
  message: string,
  type: string
): Promise<{ sentCount: number }> {
  const { data } = await api.post("/admin/notifications/broadcast", {
    title,
    message,
    type,
  });
  return data;
}

// ── Export ──
function downloadBlob(blob: Blob, filename: string) {
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  window.URL.revokeObjectURL(url);
}

export async function exportSalesReport(
  startDate: string,
  endDate: string
): Promise<void> {
  const { data } = await api.get("/admin/export/sales", {
    params: { startDate, endDate },
    responseType: "blob",
  });
  downloadBlob(data, `sales-report-${startDate}-${endDate}.csv`);
}

export async function exportOrders(params: {
  status?: string;
  startDate?: string;
  endDate?: string;
  search?: string;
}): Promise<void> {
  const { data } = await api.get("/admin/export/orders", {
    params,
    responseType: "blob",
  });
  const today = new Date().toISOString().slice(0, 10);
  downloadBlob(data, `orders-export-${today}.csv`);
}

// ── Platform Settlements ──
export interface SettlementDto {
  id: number;
  tenantId: number;
  periodStart: string;
  periodEnd: string;
  orderCount: number;
  totalOrderAmount: number;
  totalCommission: number;
  totalSettlementAmount: number;
  status: string;
  bankName: string | null;
  bankAccount: string | null;
  transactionId: string | null;
  settledAt: string | null;
  settledBy: string | null;
  createdAt: string;
}

export interface CommissionSummaryItem {
  tenantId: number;
  tenantName: string;
  totalOrders: number;
  totalOrderAmount: number;
  totalCommission: number;
  totalSettlementAmount: number;
  pendingSettlement: number;
}

export async function getCommissionSummary(): Promise<CommissionSummaryItem[]> {
  const { data } = await api.get("/platform/commissions/summary");
  return data;
}

export async function getSettlements(slug: string, status?: string): Promise<SettlementDto[]> {
  const { data } = await api.get(`/platform/${slug}/settlements`, { params: { status } });
  return data;
}

export async function createSettlement(slug: string, periodStart: string, periodEnd: string): Promise<{ settlementId: number }> {
  const { data } = await api.post(`/platform/${slug}/settlements`, { periodStart, periodEnd });
  return data;
}

export async function processSettlement(id: number, transactionId: string, settledBy: string): Promise<void> {
  await api.put(`/platform/settlements/${id}/process`, { transactionId, settledBy });
}

// ── TenantAdmin Settlements ──

export async function getMySettlements(status?: string): Promise<SettlementDto[]> {
  const { data } = await api.get("/admin/settlements", { params: { status } });
  return data;
}

export interface CommissionDto {
  id: number;
  tenantId: number;
  orderId: number;
  orderAmount: number;
  commissionRate: number;
  commissionAmount: number;
  settlementAmount: number;
  status: string;
  settlementId: number | null;
  createdAt: string;
}

export async function getMyCommissions(status?: string): Promise<CommissionDto[]> {
  const { data } = await api.get("/admin/commissions", { params: { status } });
  return data;
}

export interface CommissionSettingDto {
  id: number;
  tenantId: number;
  productId: number | null;
  categoryId: number | null;
  commissionRate: number;
  settlementCycle: string;
  settlementDayOfWeek: number;
  minSettlementAmount: number;
  bankName: string | null;
  bankAccount: string | null;
  bankHolder: string | null;
}

export async function getMyCommissionSettings(): Promise<CommissionSettingDto[]> {
  const { data } = await api.get("/admin/commissions/settings");
  return data;
}

// ── Auto Reorder (MES L3) ──

export interface AutoReorderStatsDto {
  totalRules: number;
  enabledRules: number;
  productsBelowThreshold: number;
  totalPurchaseOrders: number;
  pendingOrders: number;
  forwardedOrders: number;
  lastAutoRun: string | null;
}

export interface AutoReorderRuleDto {
  id: number;
  productId: number;
  productName: string;
  reorderThreshold: number;
  reorderQuantity: number;
  maxStockLevel: number;
  isEnabled: boolean;
  autoForwardToMes: boolean;
  minIntervalHours: number;
  lastTriggeredAt: string | null;
  currentStock: number;
  createdAt: string;
}

export interface PurchaseOrderItemDto {
  id: number;
  productId: number;
  productName: string;
  mesProductCode: string | null;
  currentStock: number;
  reorderThreshold: number;
  orderedQuantity: number;
  receivedQuantity: number;
  reason: string | null;
}

export interface PurchaseOrderDto {
  id: number;
  orderNumber: string;
  status: string;
  triggerType: string;
  totalQuantity: number;
  itemCount: number;
  mesOrderId: string | null;
  mesOrderNo: string | null;
  forwardedAt: string | null;
  confirmedAt: string | null;
  notes: string | null;
  createdByUser: string | null;
  createdAt: string;
  items: PurchaseOrderItemDto[];
}

export interface PurchaseOrderListDto {
  items: PurchaseOrderDto[];
  total: number;
  page: number;
  pageSize: number;
}

export async function getAutoReorderStats(): Promise<AutoReorderStatsDto> {
  const { data } = await api.get("/admin/mes/auto-reorder/stats");
  return data;
}

export async function getAutoReorderRules(enabledOnly?: boolean): Promise<AutoReorderRuleDto[]> {
  const { data } = await api.get("/admin/mes/auto-reorder/rules", { params: { enabledOnly } });
  return data;
}

export async function upsertAutoReorderRule(rule: {
  productId: number;
  reorderThreshold: number;
  reorderQuantity?: number;
  maxStockLevel?: number;
  isEnabled?: boolean;
  autoForwardToMes?: boolean;
  minIntervalHours?: number;
}): Promise<{ ruleId: number }> {
  const { data } = await api.post("/admin/mes/auto-reorder/rules", rule);
  return data;
}

export async function deleteAutoReorderRule(id: number): Promise<void> {
  await api.delete(`/admin/mes/auto-reorder/rules/${id}`);
}

export async function toggleAutoReorderRule(id: number, isEnabled: boolean): Promise<void> {
  await api.put(`/admin/mes/auto-reorder/rules/${id}/toggle`, { isEnabled });
}

export async function bulkCreateAutoReorderRules(params: {
  reorderThreshold?: number;
  minIntervalHours?: number;
  autoForwardToMes?: boolean;
}): Promise<{ createdCount: number }> {
  const { data } = await api.post("/admin/mes/auto-reorder/rules/bulk", params);
  return data;
}

export async function getPurchaseOrders(status?: string, page = 1, pageSize = 20): Promise<PurchaseOrderListDto> {
  const { data } = await api.get("/admin/mes/purchase-orders", { params: { status, page, pageSize } });
  return data;
}

export async function forwardPurchaseOrder(id: number): Promise<{ mesOrderId: string }> {
  const { data } = await api.post(`/admin/mes/purchase-orders/${id}/forward`);
  return data;
}

export async function cancelPurchaseOrder(id: number): Promise<void> {
  await api.post(`/admin/mes/purchase-orders/${id}/cancel`);
}
