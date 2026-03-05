import api from "./api";

export interface DailyDemand {
  date: string;
  quantity: number;
}

export interface AiInsight {
  trendAnalysis: string;
  seasonalPatterns: string;
  recommendations: string[];
  eventImpact: string | null;
  confidenceScore: number;
}

export interface ForecastResult {
  productId: number;
  productName: string;
  historicalDemand: DailyDemand[];
  forecastedDemand: DailyDemand[];
  averageDailyDemand: number;
  currentStock: number;
  estimatedDaysUntilStockout: number;
  aiInsight?: AiInsight | null;
  trendSlope?: number | null;
  trendDirection?: string | null;
  seasonalityStrength?: number | null;
  forecastMethod?: string | null;
}

export interface CategoryForecastResult {
  categoryId: number;
  categoryName: string;
  productCount: number;
  totalStock: number;
  totalAverageDailyDemand: number;
  minDaysUntilStockout: number;
  topProducts: ForecastResult[];
  aiInsight?: AiInsight | null;
}

export interface PurchaseRecommendation {
  productId: number;
  productName: string;
  categoryName: string | null;
  currentStock: number;
  averageDailyDemand: number;
  estimatedDaysUntilStockout: number;
  recommendedOrderQuantity: number;
  urgency: string;
  reason: string;
}

export interface MesSyncStatus {
  isConnected: boolean;
  lastSyncAt: string | null;
  syncedProductCount: number;
  errorMessage: string | null;
}

export interface MesStockDiscrepancy {
  productId: number;
  productName: string;
  productCode: string | null;
  shopStock: number;
  mesStock: number;
  difference: number;
}

// Sprint 5: New types

export interface MesSyncHistory {
  id: number;
  startedAt: string;
  completedAt: string | null;
  status: string;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  elapsedMs: number;
  errorDetailsJson: string | null;
  conflictDetailsJson: string | null;
}

export interface MesSyncHistoryPage {
  items: MesSyncHistory[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ForecastAccuracyResult {
  productId: number;
  productName: string;
  mape: number;
  mae: number;
  forecastCount: number;
  dataPoints: AccuracyDataPoint[];
}

export interface AccuracyDataPoint {
  targetDate: string;
  predicted: number;
  actual: number;
  percentageError: number;
}

export interface AutoPurchaseOrderResult {
  success: boolean;
  mesOrderId: string | null;
  productCount: number;
  totalQuantity: number;
  errorMessage: string | null;
}

export interface BatchAiInsightResult {
  products: ProductAiSummary[];
  overallSummary: string;
  averageConfidence: number;
}

export interface ProductAiSummary {
  productId: number;
  productName: string;
  trendDirection: string;
  seasonalityStrength: number;
  keyInsight: string;
}

// --- Existing API functions ---

export async function getProductForecast(
  productId: number,
  days = 30
): Promise<ForecastResult> {
  const { data } = await api.get<ForecastResult>(
    `/admin/forecast/products/${productId}`,
    { params: { days } }
  );
  return data;
}

export async function getProductForecastWithAi(
  productId: number,
  days = 30
): Promise<ForecastResult> {
  const { data } = await api.get<ForecastResult>(
    `/admin/forecast/products/${productId}/ai`,
    { params: { days } }
  );
  return data;
}

export async function getLowStockForecasts(
  daysThreshold = 14
): Promise<ForecastResult[]> {
  const { data } = await api.get<ForecastResult[]>(
    "/admin/forecast/low-stock",
    { params: { daysThreshold } }
  );
  return data;
}

export async function getCategoryForecasts(
  days = 30
): Promise<CategoryForecastResult[]> {
  const { data } = await api.get<CategoryForecastResult[]>(
    "/admin/forecast/categories",
    { params: { days } }
  );
  return data;
}

export async function getCategoryForecast(
  categoryId: number,
  days = 30
): Promise<CategoryForecastResult> {
  const { data } = await api.get<CategoryForecastResult>(
    `/admin/forecast/categories/${categoryId}`,
    { params: { days } }
  );
  return data;
}

export async function getPurchaseRecommendations(
  daysThreshold = 14
): Promise<PurchaseRecommendation[]> {
  const { data } = await api.get<PurchaseRecommendation[]>(
    "/admin/forecast/purchase-recommendations",
    { params: { daysThreshold } }
  );
  return data;
}

export async function getMesSyncStatus(): Promise<MesSyncStatus> {
  const { data } = await api.get<MesSyncStatus>("/admin/mes/status");
  return data;
}

export async function triggerMesSync(): Promise<{ message: string }> {
  const { data } = await api.post<{ message: string }>("/admin/mes/sync");
  return data;
}

export async function getMesDiscrepancies(): Promise<MesStockDiscrepancy[]> {
  const { data } = await api.get<MesStockDiscrepancy[]>(
    "/admin/mes/discrepancies"
  );
  return data;
}

export interface MesInventoryComparisonItem {
  shopProductId: number | null;
  productName: string;
  mesProductCode: string | null;
  shopStock: number;
  mesStock: number;
  difference: number;
  status: "matched" | "discrepancy" | "shop_only" | "mes_only";
}

export async function getMesInventoryComparison(): Promise<
  MesInventoryComparisonItem[]
> {
  const { data } = await api.get<MesInventoryComparisonItem[]>(
    "/admin/mes/inventory-comparison"
  );
  return data;
}

export async function syncMesProduct(
  productId: number
): Promise<{ message: string; productId: number; mesStock: number }> {
  const { data } = await api.post(
    `/admin/mes/sync-product/${productId}`
  );
  return data;
}

export async function forwardOrderToMes(
  orderId: number
): Promise<{ message: string; mesOrderId?: string }> {
  const { data } = await api.post(`/admin/mes/orders/${orderId}/forward`);
  return data;
}

export async function generateProductImage(
  productId: number,
  prompt?: string,
  size = "1024x1024"
): Promise<{ url: string; revisedPrompt?: string }> {
  const { data } = await api.post(`/products/${productId}/generate-image`, {
    prompt,
    size,
  });
  return data;
}

// --- Sprint 5: New API functions ---

export async function getMesSyncHistory(
  page = 1,
  pageSize = 20
): Promise<MesSyncHistoryPage> {
  const { data } = await api.get<MesSyncHistoryPage>(
    "/admin/mes/sync-history",
    { params: { page, pageSize } }
  );
  return data;
}

export async function getProductAccuracy(
  productId: number
): Promise<ForecastAccuracyResult> {
  const { data } = await api.get<ForecastAccuracyResult>(
    `/admin/forecast/accuracy/${productId}`
  );
  return data;
}

export async function getAllAccuracies(): Promise<ForecastAccuracyResult[]> {
  const { data } = await api.get<ForecastAccuracyResult[]>(
    "/admin/forecast/accuracy"
  );
  return data;
}

export async function updateAccuracy(): Promise<{ message: string }> {
  const { data } = await api.post<{ message: string }>(
    "/admin/forecast/accuracy/update"
  );
  return data;
}

export async function createAutoPurchaseOrder(
  productIds: number[]
): Promise<AutoPurchaseOrderResult> {
  const { data } = await api.post<AutoPurchaseOrderResult>(
    "/admin/forecast/auto-purchase-order",
    { productIds }
  );
  return data;
}

export async function getBatchAiInsights(): Promise<BatchAiInsightResult> {
  const { data } = await api.get<BatchAiInsightResult>(
    "/admin/forecast/batch-ai-insights"
  );
  return data;
}

export async function getHoltWintersForecast(
  productId: number,
  days = 30
): Promise<ForecastResult> {
  const { data } = await api.get<ForecastResult>(
    `/admin/forecast/products/${productId}/holt-winters`,
    { params: { days } }
  );
  return data;
}

// ── Production Plan Management (MES L2) ──

export interface ProductionPlanSuggestionDto {
  id: number;
  productId: number;
  productName: string;
  suggestedQuantity: number;
  currentStock: number;
  averageDailyDemand: number;
  reason: string;
  status: string;
  mesOrderId: string | null;
  approvedBy: string | null;
  approvedAt: string | null;
  createdAt: string;
}

export async function generateProductionPlan(): Promise<{ count: number }> {
  const { data } = await api.post<{ count: number }>("/admin/mes/production-plan/generate");
  return data;
}

export async function getProductionPlanSuggestions(
  status?: string
): Promise<ProductionPlanSuggestionDto[]> {
  const { data } = await api.get<ProductionPlanSuggestionDto[]>(
    "/admin/mes/production-plan",
    { params: { status } }
  );
  return data;
}

export async function approveProductionPlan(id: number): Promise<void> {
  await api.put(`/admin/mes/production-plan/${id}/approve`);
}

export async function rejectProductionPlan(id: number): Promise<void> {
  await api.put(`/admin/mes/production-plan/${id}/reject`);
}

export async function forwardProductionPlanToMes(
  id: number
): Promise<{ mesOrderId: string }> {
  const { data } = await api.post<{ mesOrderId: string }>(
    `/admin/mes/production-plan/${id}/forward-mes`
  );
  return data;
}

// ── MES Inventory Reserve/Release ──

export async function reserveInventory(request: {
  productId: number;
  quantity: number;
  reason?: string;
}): Promise<{ reservationId: string }> {
  const { data } = await api.post<{ reservationId: string }>(
    "/admin/mes/inventory/reserve",
    request
  );
  return data;
}

export async function releaseInventory(request: {
  productId: number;
  quantity: number;
  reason?: string;
}): Promise<void> {
  await api.post("/admin/mes/inventory/release", request);
}
