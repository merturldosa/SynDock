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

// ── Billing ──
export interface TenantBilling {
  tenantId: number;
  tenantName: string;
  tenantSlug: string;
  planType: string;
  monthlyPrice: number;
  billingStatus: string;
  trialEndsAt: string | null;
  nextBillingAt: string | null;
}

export async function getAllBilling(): Promise<TenantBilling[]> {
  const { data } = await api.get("/platform/tenants/billing");
  return data;
}

export async function getTenantBilling(
  slug: string
): Promise<TenantBilling> {
  const { data } = await api.get(`/platform/tenants/${slug}/billing`);
  return data;
}

export async function updateTenantBilling(
  slug: string,
  req: { planType?: string; monthlyPrice?: number; billingStatus?: string }
): Promise<void> {
  await api.put(`/platform/tenants/${slug}/billing`, req);
}

// ── Invoices ──
export interface InvoiceDto {
  id: number;
  tenantId: number;
  tenantName: string;
  invoiceNumber: string;
  amount: number;
  status: string;
  billingPeriod: string;
  planType: string | null;
  issuedAt: string;
  paidAt: string | null;
  transactionId: string | null;
  paymentMethod: string | null;
}

export async function getAllInvoices(): Promise<InvoiceDto[]> {
  const { data } = await api.get("/platform/tenants/invoices");
  return data;
}

export async function getTenantInvoices(slug: string): Promise<InvoiceDto[]> {
  const { data } = await api.get(`/platform/tenants/${slug}/invoices`);
  return data;
}

export async function generateInvoice(slug: string, billingPeriod: string): Promise<{ invoiceId: number }> {
  const { data } = await api.post(`/platform/tenants/${slug}/invoices`, { billingPeriod });
  return data;
}

export async function markInvoicePaid(
  id: number,
  transactionId?: string,
  paymentMethod?: string
): Promise<void> {
  await api.put(`/platform/tenants/invoices/${id}/pay`, { transactionId, paymentMethod });
}

// ── Usage ──
export interface TenantUsage {
  tenantId: number;
  tenantName: string;
  tenantSlug: string;
  planType: string;
  productCount: number;
  maxProducts: number;
  userCount: number;
  maxUsers: number;
  monthlyOrderCount: number;
  maxMonthlyOrders: number;
  storageUsedBytes: number;
  maxStorageBytes: number;
  currentPeriod: string;
}

export async function getTenantUsage(slug: string): Promise<TenantUsage> {
  const { data } = await api.get(`/platform/tenants/${slug}/usage`);
  return data;
}

// ── Domain Management ──
export interface DnsInstruction {
  type: string;
  host: string;
  target: string;
}

export interface DomainConfig {
  customDomain: string | null;
  subdomain: string | null;
  verificationStatus: string;
  verifiedAt: string | null;
  sslStatus: string;
  sslExpiresAt: string | null;
  dnsInstructions: DnsInstruction[];
}

export interface DomainVerificationResult {
  isVerified: boolean;
  message: string;
}

export async function getTenantDomainConfig(slug: string): Promise<DomainConfig> {
  const { data } = await api.get(`/platform/tenants/${slug}/domain`);
  return data;
}

export async function updateTenantDomain(
  slug: string,
  req: { customDomain?: string; subdomain?: string }
): Promise<DomainConfig> {
  const { data } = await api.put(`/platform/tenants/${slug}/domain`, req);
  return data;
}

export async function verifyTenantDomain(slug: string): Promise<DomainVerificationResult> {
  const { data } = await api.post(`/platform/tenants/${slug}/domain/verify`);
  return data;
}
