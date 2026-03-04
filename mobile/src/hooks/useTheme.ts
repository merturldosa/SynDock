import { useTenantStore } from "../stores/tenantStore";
import type { Theme } from "../lib/theme";

export function useTheme(): Theme {
  return useTenantStore((s) => s.theme);
}
