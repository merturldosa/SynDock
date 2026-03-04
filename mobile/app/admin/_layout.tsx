import { Stack } from "expo-router";

export default function AdminLayout() {
  return (
    <Stack>
      <Stack.Screen name="index" options={{ title: "관리자 대시보드" }} />
      <Stack.Screen name="orders" options={{ title: "주문 관리" }} />
      <Stack.Screen name="inventory" options={{ title: "재고 알림" }} />
      <Stack.Screen name="stats" options={{ title: "매출 통계" }} />
    </Stack>
  );
}
