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
