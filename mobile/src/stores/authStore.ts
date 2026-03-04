import { create } from "zustand";
import * as SecureStore from "expo-secure-store";
import { login as loginApi, getMe } from "../lib/api";
import type { UserInfo } from "../types";

interface AuthState {
  user: UserInfo | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<boolean>;
  logout: () => Promise<void>;
  fetchMe: () => Promise<void>;
  checkAuth: () => Promise<boolean>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isLoading: true,

  login: async (email, password) => {
    try {
      const res = await loginApi(email, password);
      await SecureStore.setItemAsync("accessToken", res.data.accessToken);
      await SecureStore.setItemAsync("refreshToken", res.data.refreshToken);
      const meRes = await getMe();
      set({ user: meRes.data, isAuthenticated: true });
      return true;
    } catch {
      return false;
    }
  },

  logout: async () => {
    await SecureStore.deleteItemAsync("accessToken");
    await SecureStore.deleteItemAsync("refreshToken");
    set({ user: null, isAuthenticated: false });
  },

  fetchMe: async () => {
    try {
      const res = await getMe();
      set({ user: res.data, isAuthenticated: true, isLoading: false });
    } catch {
      set({ user: null, isAuthenticated: false, isLoading: false });
    }
  },

  checkAuth: async () => {
    const token = await SecureStore.getItemAsync("accessToken");
    if (!token) {
      set({ isLoading: false, isAuthenticated: false });
      return false;
    }
    try {
      const res = await getMe();
      set({ user: res.data, isAuthenticated: true, isLoading: false });
      return true;
    } catch {
      set({ isLoading: false, isAuthenticated: false });
      return false;
    }
  },
}));
