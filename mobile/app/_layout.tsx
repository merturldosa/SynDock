import { useEffect } from "react";
import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useAuthStore } from "../src/stores/authStore";
import { useTenantStore } from "../src/stores/tenantStore";
import { useNotificationStore } from "../src/stores/notificationStore";
import { setupNotificationListeners } from "../src/lib/notifications";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 5 * 60 * 1000 },
  },
});

export default function RootLayout() {
  const checkAuth = useAuthStore((s) => s.checkAuth);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const fetchConfig = useTenantStore((s) => s.fetchConfig);
  const registerPushToken = useNotificationStore((s) => s.registerPushToken);
  const fetchUnreadCount = useNotificationStore((s) => s.fetchUnreadCount);

  useEffect(() => {
    checkAuth();
    fetchConfig();
    const cleanup = setupNotificationListeners();
    return cleanup;
  }, []);

  useEffect(() => {
    if (isAuthenticated) {
      registerPushToken();
      fetchUnreadCount();
    }
  }, [isAuthenticated]);

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
