import api from "./api";
import type { Cart, AddToCartRequest } from "@/types/cart";

export async function getCart(): Promise<Cart> {
  const { data } = await api.get<Cart>("/cart");
  return data;
}

export async function addToCart(request: AddToCartRequest): Promise<{ cartId: number }> {
  const { data } = await api.post<{ cartId: number }>("/cart/items", request);
  return data;
}

export async function updateCartItem(itemId: number, quantity: number): Promise<void> {
  await api.put(`/cart/items/${itemId}`, { quantity });
}

export async function removeCartItem(itemId: number): Promise<void> {
  await api.delete(`/cart/items/${itemId}`);
}

export async function clearCart(): Promise<void> {
  await api.delete("/cart");
}
