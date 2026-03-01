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
}

export async function getSalesAnalytics(days = 30): Promise<SalesAnalytics> {
  const { data } = await api.get("/admin/analytics", { params: { days } });
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
  const { data } = await api.post("/api/products", payload);
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
  await api.put(`/api/products/${id}`, body);
}

export async function deleteProduct(id: number): Promise<void> {
  await api.delete(`/api/products/${id}`);
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
  const { data } = await api.post("/api/categories", body);
  return data;
}

export async function updateCategory(
  id: number,
  body: Partial<CreateCategoryRequest>
): Promise<void> {
  await api.put(`/api/categories/${id}`, body);
}

export async function deleteCategory(id: number): Promise<void> {
  await api.delete(`/api/categories/${id}`);
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
