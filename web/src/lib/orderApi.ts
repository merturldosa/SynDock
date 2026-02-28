import api from "./api";
import type { Order, PagedOrders, Address } from "@/types/order";

export async function createOrder(request: {
  shippingAddressId?: number | null;
  note?: string | null;
  couponCode?: string | null;
  pointsToUse?: number;
}): Promise<{ orderId: number }> {
  const { data } = await api.post<{ orderId: number }>("/order", request);
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
