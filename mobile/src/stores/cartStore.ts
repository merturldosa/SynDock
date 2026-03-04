import { create } from "zustand";
import {
  getCart,
  addToCart as addToCartApi,
  updateCartItem,
  removeCartItem,
  clearCart as clearCartApi,
} from "../lib/api";
import type { Cart, CartItem } from "../types";

interface CartState {
  cart: Cart;
  isLoading: boolean;
  fetchCart: () => Promise<void>;
  addToCart: (productId: number, quantity?: number) => Promise<void>;
  updateQuantity: (itemId: number, quantity: number) => Promise<void>;
  removeItem: (itemId: number) => Promise<void>;
  clearCart: () => Promise<void>;
}

export const useCartStore = create<CartState>((set) => ({
  cart: { items: [], totalQuantity: 0, totalAmount: 0 },
  isLoading: false,

  fetchCart: async () => {
    set({ isLoading: true });
    try {
      const res = await getCart();
      set({ cart: res.data, isLoading: false });
    } catch {
      set({ isLoading: false });
    }
  },

  addToCart: async (productId, quantity = 1) => {
    try {
      await addToCartApi(productId, quantity);
      const res = await getCart();
      set({ cart: res.data });
    } catch {}
  },

  updateQuantity: async (itemId, quantity) => {
    try {
      await updateCartItem(itemId, quantity);
      const res = await getCart();
      set({ cart: res.data });
    } catch {}
  },

  removeItem: async (itemId) => {
    try {
      await removeCartItem(itemId);
      const res = await getCart();
      set({ cart: res.data });
    } catch {}
  },

  clearCart: async () => {
    try {
      await clearCartApi();
      set({ cart: { items: [], totalQuantity: 0, totalAmount: 0 } });
    } catch {}
  },
}));
