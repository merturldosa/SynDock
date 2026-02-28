import api from "./api";
import type { TenantInfo } from "@/types/tenant";

export interface TenantDetail extends TenantInfo {
  subdomain: string | null;
  customDomain: string | null;
  createdAt: string;
}

export interface CreateTenantRequest {
  name: string;
  slug: string;
  configJson?: string;
}

export interface UpdateTenantRequest {
  name: string;
  isActive: boolean;
  configJson?: string;
}

export interface PlatformStats {
  totalTenants: number;
  activeTenants: number;
}

export async function getPlatformTenants(): Promise<TenantDetail[]> {
  const { data } = await api.get("/platform/tenants");
  return data;
}

export async function getPlatformTenant(slug: string): Promise<TenantDetail> {
  const { data } = await api.get(`/platform/tenants/${slug}`);
  return data;
}

export async function createPlatformTenant(
  req: CreateTenantRequest
): Promise<{ id: number }> {
  const { data } = await api.post("/platform/tenants", req);
  return data;
}

export async function updatePlatformTenant(
  slug: string,
  req: UpdateTenantRequest
): Promise<void> {
  await api.put(`/platform/tenants/${slug}`, req);
}
