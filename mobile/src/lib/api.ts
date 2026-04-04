import axios from "axios";
import * as SecureStore from "expo-secure-store";
import type {
  AuthTokens,
  Cart,
  Category,
  Order,
  PagedResponse,
  Product,
  UserInfo,
  Notification,
  Address,
  TenantConfig,
} from "../types";

const API_BASE = process.env.EXPO_PUBLIC_API_URL || "http://10.0.2.2:5100";
const TENANT_ID = process.env.EXPO_PUBLIC_TENANT_ID || "catholia";

const api = axios.create({
  baseURL: `${API_BASE}/api`,
  headers: {
    "Content-Type": "application/json",
    "X-Tenant-Id": TENANT_ID,
  },
});

// Token interceptor
api.interceptors.request.use(async (config) => {
  const token = await SecureStore.getItemAsync("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status === 401) {
      const refreshToken = await SecureStore.getItemAsync("refreshToken");
      if (refreshToken) {
        try {
          const res = await axios.post(`${API_BASE}/api/auth/refresh`, {
            refreshToken,
          });
          const tokens: AuthTokens = res.data;
          await SecureStore.setItemAsync("accessToken", tokens.accessToken);
          await SecureStore.setItemAsync("refreshToken", tokens.refreshToken);
          error.config.headers.Authorization = `Bearer ${tokens.accessToken}`;
          return api(error.config);
        } catch {
          await SecureStore.deleteItemAsync("accessToken");
          await SecureStore.deleteItemAsync("refreshToken");
        }
      }
    }
    return Promise.reject(error);
  }
);

// Auth
export const login = (email: string, password: string) =>
  api.post<AuthTokens>("/auth/login", { email, password });

export const register = (data: {
  email: string;
  password: string;
  username: string;
  name: string;
}) => api.post("/auth/register", data);

export const getMe = () => api.get<UserInfo>("/auth/me");

// Products
export const getProducts = (params?: Record<string, unknown>) =>
  api.get<PagedResponse<Product>>("/products", { params });

export const getProductById = (id: number) =>
  api.get<Product>(`/products/${id}`);

export const getSearchSuggestions = (term: string) =>
  api.get<Product[]>("/products/suggestions", { params: { term } });

// Categories
export const getCategories = () => api.get<Category[]>("/categories");

// Cart
export const getCart = () => api.get<Cart>("/cart");
export const addToCart = (productId: number, quantity: number) =>
  api.post("/cart/items", { productId, quantity });
export const updateCartItem = (itemId: number, quantity: number) =>
  api.put(`/cart/items/${itemId}`, { quantity });
export const removeCartItem = (itemId: number) =>
  api.delete(`/cart/items/${itemId}`);
export const clearCart = () => api.delete("/cart");

// Orders
export const createOrder = (data: {
  shippingAddressId: number;
  note?: string;
  couponCode?: string;
  pointsToUse?: number;
  deliveryOptionId?: number;
}) => api.post("/order", data);

export const getOrders = (params?: Record<string, unknown>) =>
  api.get<PagedResponse<Order>>("/order", { params });

export const getOrderById = (id: number) => api.get<Order>(`/order/${id}`);

// Addresses
export const getAddresses = () => api.get<Address[]>("/address");
export const createAddress = (data: Omit<Address, "id">) =>
  api.post("/address", data);

// Wishlist
export const getWishlist = () => api.get("/wishlist");
export const toggleWishlist = (productId: number) =>
  api.post("/wishlist/toggle", { productId });

// Notifications
export const getNotifications = (page = 1, pageSize = 20) =>
  api.get<PagedResponse<Notification>>("/notifications", {
    params: { page, pageSize },
  });
export const getUnreadCount = () =>
  api.get<{ count: number }>("/notifications/unread-count");
export const markAllRead = () => api.put("/notifications/read-all");

// Push Token
export const registerPushToken = (token: string, platform: string) =>
  api.post("/push/mobile-token", { token, platform });
export const unregisterPushToken = (token: string, platform: string) =>
  api.post("/push/mobile-token/unregister", { token, platform });

// Delivery Tracking
export const getDeliveryTracking = (orderId: number) =>
  api.get(`/delivery/orders/${orderId}/assignment`);

// Recommendations
export const getPopular = () => api.get<Product[]>("/recommendations/popular");

// Points
export const getPointBalance = () =>
  api.get<{ balance: number }>("/points/balance");

// Coupons
export const getMyCoupons = () => api.get("/coupons/my");

export default api;
