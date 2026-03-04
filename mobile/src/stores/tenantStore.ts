import { create } from "zustand";
import type { TenantConfig } from "../types";
import type { Theme } from "../lib/theme";
import { defaultTheme } from "../lib/theme";
import api from "../lib/api";

interface TenantState {
  config: TenantConfig | null;
  theme: Theme;
  isLoaded: boolean;
  fetchConfig: () => Promise<void>;
}

function buildTheme(config: TenantConfig | null): Theme {
  if (!config) return defaultTheme;

  const primary = config.primaryColor || defaultTheme.primary;
  const secondary = config.secondaryColor || defaultTheme.secondary;

  return {
    ...defaultTheme,
    primary,
    primaryLight: lighten(primary, 0.3),
    secondary,
    secondaryLight: lighten(secondary, 0.3),
  };
}

function lighten(hex: string, amount: number): string {
  const num = parseInt(hex.replace("#", ""), 16);
  const r = Math.min(255, ((num >> 16) & 0xff) + Math.round(255 * amount));
  const g = Math.min(255, ((num >> 8) & 0xff) + Math.round(255 * amount));
  const b = Math.min(255, (num & 0xff) + Math.round(255 * amount));
  return `#${((r << 16) | (g << 8) | b).toString(16).padStart(6, "0")}`;
}

export const useTenantStore = create<TenantState>((set) => ({
  config: null,
  theme: defaultTheme,
  isLoaded: false,

  fetchConfig: async () => {
    try {
      const res = await api.get<TenantConfig>("/tenant/config");
      const config = res.data;
      set({ config, theme: buildTheme(config), isLoaded: true });
    } catch {
      set({ isLoaded: true });
    }
  },
}));
