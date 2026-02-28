import { create } from "zustand";
import type { Cart, CartItem } from "@/types/cart";
import * as cartApi from "@/lib/cartApi";

interface CartState {
  cart: Cart | null;
  isLoading: boolean;
  error: string | null;
  fetchCart: () => Promise<void>;
  addToCart: (productId: number, variantId?: number | null, quantity?: number) => Promise<boolean>;
  updateQuantity: (itemId: number, quantity: number) => Promise<void>;
  removeItem: (itemId: number) => Promise<void>;
  clearCart: () => Promise<void>;
  totalQuantity: () => number;
}

export const useCartStore = create<CartState>((set, get) => ({
  cart: null,
  isLoading: false,
  error: null,

  fetchCart: async () => {
    set({ isLoading: true, error: null });
    try {
      const cart = await cartApi.getCart();
      set({ cart, isLoading: false });
    } catch {
      set({ isLoading: false, error: "장바구니를 불러올 수 없습니다." });
    }
  },

  addToCart: async (productId, variantId, quantity = 1) => {
    try {
      await cartApi.addToCart({ productId, variantId, quantity });
      await get().fetchCart();
      return true;
    } catch {
      set({ error: "장바구니에 추가할 수 없습니다." });
      return false;
    }
  },

  updateQuantity: async (itemId, quantity) => {
    // Optimistic update
    const prevCart = get().cart;
    if (prevCart) {
      const items = prevCart.items.map((item) =>
        item.id === itemId ? { ...item, quantity, subTotal: (item.salePrice ?? item.price) * quantity } : item
      );
      set({
        cart: {
          ...prevCart,
          items,
          totalAmount: items.reduce((sum, i) => sum + i.subTotal, 0),
          totalQuantity: items.reduce((sum, i) => sum + i.quantity, 0),
        },
      });
    }

    try {
      await cartApi.updateCartItem(itemId, quantity);
      await get().fetchCart();
    } catch {
      set({ cart: prevCart, error: "수량을 변경할 수 없습니다." });
    }
  },

  removeItem: async (itemId) => {
    const prevCart = get().cart;
    if (prevCart) {
      const items = prevCart.items.filter((item) => item.id !== itemId);
      set({
        cart: {
          ...prevCart,
          items,
          totalAmount: items.reduce((sum, i) => sum + i.subTotal, 0),
          totalQuantity: items.reduce((sum, i) => sum + i.quantity, 0),
        },
      });
    }

    try {
      await cartApi.removeCartItem(itemId);
      await get().fetchCart();
    } catch {
      set({ cart: prevCart, error: "항목을 삭제할 수 없습니다." });
    }
  },

  clearCart: async () => {
    const prevCart = get().cart;
    set({ cart: { id: prevCart?.id ?? 0, items: [], totalAmount: 0, totalQuantity: 0 } });

    try {
      await cartApi.clearCart();
    } catch {
      set({ cart: prevCart, error: "장바구니를 비울 수 없습니다." });
    }
  },

  totalQuantity: () => get().cart?.totalQuantity ?? 0,
}));
