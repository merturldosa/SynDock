import { useEffect } from "react";
import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useAuthStore } from "../src/stores/authStore";
import { useTenantStore } from "../src/stores/tenantStore";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 5 * 60 * 1000 },
  },
});

export default function RootLayout() {
  const checkAuth = useAuthStore((s) => s.checkAuth);
  const fetchConfig = useTenantStore((s) => s.fetchConfig);

  useEffect(() => {
    checkAuth();
    fetchConfig();
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <StatusBar style="dark" />
      <Stack screenOptions={{ headerShown: false }}>
        <Stack.Screen name="(tabs)" />
        <Stack.Screen name="(auth)" />
        <Stack.Screen
          name="product/[id]"
          options={{ headerShown: true, title: "" }}
        />
        <Stack.Screen
          name="order/index"
          options={{ headerShown: true, title: "주문하기" }}
        />
        <Stack.Screen
          name="order/complete"
          options={{ headerShown: true, title: "주문 완료" }}
        />
        <Stack.Screen name="mypage" options={{ headerShown: false }} />
        <Stack.Screen name="admin" options={{ headerShown: false }} />
      </Stack>
    </QueryClientProvider>
  );
}
