import api from "./api";

export interface CouponDto {
  id: number;
  code: string;
  name: string;
  description: string | null;
  discountType: string;
  discountValue: number;
  minOrderAmount: number;
  maxDiscountAmount: number | null;
  startDate: string;
  endDate: string;
  maxUsageCount: number;
  currentUsageCount: number;
  isActive: boolean;
  createdAt: string;
}

export interface UserCouponDto {
  id: number;
  couponId: number;
  code: string;
  name: string;
  description: string | null;
  discountType: string;
  discountValue: number;
  minOrderAmount: number;
  maxDiscountAmount: number | null;
  endDate: string;
  isUsed: boolean;
  usedAt: string | null;
}

export interface CouponValidationResult {
  isValid: boolean;
  errorMessage: string | null;
  discountAmount: number;
  couponName?: string;
  reason?: string;
}

export interface PagedCoupons {
  items: CouponDto[];
  totalCount: number;
}

// Admin APIs
export async function getCoupons(
  page = 1,
  pageSize = 20
): Promise<PagedCoupons> {
  const { data } = await api.get("/coupons", {
    params: { page, pageSize },
  });
  return data;
}

export interface CreateCouponRequest {
  code: string;
  name: string;
  description?: string;
  discountType: string;
  discountValue: number;
  minOrderAmount: number;
  maxDiscountAmount?: number;
  startDate: string;
  endDate: string;
  maxUsageCount: number;
}

export async function createCoupon(
  req: CreateCouponRequest
): Promise<{ couponId: number }> {
  const { data } = await api.post("/coupons", req);
  return data;
}

export async function updateCoupon(
  id: number,
  req: Partial<CreateCouponRequest> & { isActive: boolean }
): Promise<void> {
  await api.put(`/coupons/${id}`, req);
}

export async function deleteCoupon(id: number): Promise<void> {
  await api.delete(`/coupons/${id}`);
}

export async function issueCoupon(
  id: number,
  userIds?: number[]
): Promise<{ issuedCount: number }> {
  const { data } = await api.post(`/coupons/${id}/issue`, { userIds });
  return data;
}

// User APIs
export async function getMyCoupons(): Promise<UserCouponDto[]> {
  const { data } = await api.get("/coupons/my");
  return data;
}

export async function validateCoupon(
  code: string,
  orderAmount: number
): Promise<CouponValidationResult> {
  const { data } = await api.post("/coupons/validate", { code, orderAmount });
  return data;
}
