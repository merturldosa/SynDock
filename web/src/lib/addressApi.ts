import api from "./api";
import type { Address } from "@/types/order";

export type { Address };

export async function getAddresses(): Promise<Address[]> {
  const { data } = await api.get<Address[]>("/address");
  return data;
}

export async function createAddress(data: Omit<Address, "id">): Promise<{ addressId: number }> {
  const { data: result } = await api.post<{ addressId: number }>("/address", data);
  return result;
}

export async function updateAddress(id: number, data: Omit<Address, "id">): Promise<void> {
  await api.put(`/address/${id}`, data);
}

export async function deleteAddress(id: number): Promise<void> {
  await api.delete(`/address/${id}`);
}
