import api from "./api";
import type { Order, PagedOrders, Address } from "@/types/order";

export async function createOrder(request: {
  shippingAddressId?: number | null;
  note?: string | null;
  couponCode?: string | null;
  pointsToUse?: number;
}): Promise<{ orderId: number; orderNumber: string }> {
  const { data } = await api.post<{ orderId: number; orderNumber: string }>("/order", request);
  return data;
}

export async function getOrders(params?: { status?: string; page?: number; pageSize?: number }): Promise<PagedOrders> {
  const { data } = await api.get<PagedOrders>("/order", { params });
  return data;
}

export async function getOrderById(id: number): Promise<Order> {
  const { data } = await api.get<Order>(`/order/${id}`);
  return data;
}

export async function cancelOrder(id: number): Promise<void> {
  await api.post(`/order/${id}/cancel`);
}

export async function getAddresses(): Promise<Address[]> {
  const { data } = await api.get<Address[]>("/address");
  return data;
}

export async function createAddress(request: Omit<Address, "id">): Promise<{ addressId: number }> {
  const { data } = await api.post<{ addressId: number }>("/address", request);
  return data;
}

export async function confirmPayment(paymentKey: string, orderId: string, amount: number): Promise<{ orderId: number; success: boolean }> {
  const { data } = await api.post<{ orderId: number; success: boolean }>("/payment/confirm", { paymentKey, orderId, amount });
  return data;
}

export async function getPaymentClientKey(): Promise<{ clientKey: string | null; provider: string }> {
  const { data } = await api.get<{ clientKey: string | null; provider: string }>("/payment/client-key");
  return data;
}

export interface TrackingEvent {
  time: string;
  status: string;
  location: string;
  description: string;
}

export interface ShippingTrackingResult {
  isSuccess: boolean;
  currentStatus: string | null;
  events: TrackingEvent[] | null;
  error: string | null;
}

export async function getShippingTracking(orderId: number): Promise<ShippingTrackingResult> {
  const { data } = await api.get<ShippingTrackingResult>(`/order/${orderId}/tracking`);
  return data;
}

export async function downloadOrderReceipt(orderId: number, orderNumber: string): Promise<void> {
  const { data } = await api.get(`/order/${orderId}/receipt`, { responseType: "blob" });
  const url = window.URL.createObjectURL(new Blob([data], { type: "application/pdf" }));
  const link = document.createElement("a");
  link.href = url;
  link.download = `receipt-${orderNumber}.pdf`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}
