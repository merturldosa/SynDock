"use client";

import { useEffect } from "react";
import { useTenantStore } from "@/stores/tenantStore";

function TenantInitializer({ children }: { children: React.ReactNode }) {
  const { isLoaded, loadTenant } = useTenantStore();

  useEffect(() => {
    if (!isLoaded) {
      loadTenant();
    }
  }, [isLoaded, loadTenant]);

  return <>{children}</>;
}

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <TenantInitializer>{children}</TenantInitializer>
  );
}
