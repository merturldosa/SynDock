import { create } from "zustand";
import type { TenantInfo, TenantTheme, TenantConfig } from "@/types/tenant";
import api from "@/lib/api";

const DEFAULT_THEME: TenantTheme = {
  primary: "#C9A84C",
  primaryLight: "#D4BA6A",
  secondary: "#1B2A4A",
  secondaryLight: "#2A3D66",
  background: "#FAF8F5",
};

interface TenantState {
  slug: string;
  name: string;
  theme: TenantTheme;
  config: TenantConfig | null;
  currentSeason: string | null;
  isLoaded: boolean;
  loadTenant: () => Promise<void>;
  applySeasonalTheme: (seasonName: string) => void;
}

function applyThemeToDOM(theme: TenantTheme) {
  const root = document.documentElement;
  root.style.setProperty("--color-primary", theme.primary);
  root.style.setProperty("--color-primary-light", theme.primaryLight);
  root.style.setProperty("--color-secondary", theme.secondary);
  root.style.setProperty("--color-secondary-light", theme.secondaryLight);
  root.style.setProperty("--color-background", theme.background);
}

export const useTenantStore = create<TenantState>((set, get) => ({
  slug: process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia",
  name: "",
  theme: DEFAULT_THEME,
  config: null,
  currentSeason: null,
  isLoaded: false,

  loadTenant: async () => {
    const slug = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";
    try {
      const { data } = await api.get<TenantInfo>(
        `/platform/tenants/${slug}`
      );

      let config: TenantConfig | null = null;
      if (data.configJson) {
        try {
          config = JSON.parse(data.configJson);
        } catch {
          config = null;
        }
      }

      const theme = { ...DEFAULT_THEME, ...config?.theme };

      if (typeof window !== "undefined") {
        applyThemeToDOM(theme);
      }

      set({
        slug: data.slug,
        name: data.name,
        theme,
        config,
        isLoaded: true,
      });
    } catch {
      // Fallback: use slug as name, default theme
      if (typeof window !== "undefined") {
        applyThemeToDOM(DEFAULT_THEME);
      }
      set({
        slug,
        name: slug.charAt(0).toUpperCase() + slug.slice(1),
        theme: DEFAULT_THEME,
        config: null,
        isLoaded: true,
      });
    }
  },

  applySeasonalTheme: (seasonName: string) => {
    const { config, theme } = get();
    const seasonalOverride = config?.seasonalThemes?.[seasonName];
    if (seasonalOverride) {
      const seasonalTheme: TenantTheme = {
        ...theme,
        ...(seasonalOverride.primary && { primary: seasonalOverride.primary }),
        ...(seasonalOverride.secondary && { secondary: seasonalOverride.secondary }),
        ...(seasonalOverride.background && { background: seasonalOverride.background }),
      };
      if (typeof window !== "undefined") {
        applyThemeToDOM(seasonalTheme);
      }
      set({ theme: seasonalTheme, currentSeason: seasonName });
    } else {
      set({ currentSeason: seasonName });
    }
  },
}));
