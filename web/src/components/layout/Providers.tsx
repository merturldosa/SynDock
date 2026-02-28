"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState } from "react";
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
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            retry: 1,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <TenantInitializer>{children}</TenantInitializer>
    </QueryClientProvider>
  );
}
