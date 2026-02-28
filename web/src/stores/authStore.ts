import { create } from "zustand";
import type { UserInfo } from "@/types/auth";
import api from "@/lib/api";

interface AuthState {
  user: UserInfo | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (accessToken: string, refreshToken: string, user: UserInfo) => void;
  logout: () => void;
  fetchMe: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  isAuthenticated: false,
  isLoading: true,

  login: (accessToken, refreshToken, user) => {
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("refreshToken", refreshToken);
    set({ user, isAuthenticated: true, isLoading: false });
  },

  logout: () => {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    set({ user: null, isAuthenticated: false, isLoading: false });
  },

  fetchMe: async () => {
    if (get().isAuthenticated && get().user) {
      set({ isLoading: false });
      return;
    }

    try {
      const token = localStorage.getItem("accessToken");
      if (!token) {
        set({ user: null, isAuthenticated: false, isLoading: false });
        return;
      }
      const { data } = await api.get<UserInfo>("/auth/me");
      set({ user: data, isAuthenticated: true, isLoading: false });
    } catch {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      set({ user: null, isAuthenticated: false, isLoading: false });
    }
  },
}));
