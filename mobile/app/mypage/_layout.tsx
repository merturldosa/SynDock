import { Stack } from "expo-router";

export default function MyPageLayout() {
  return (
    <Stack>
      <Stack.Screen name="orders" options={{ title: "주문 내역" }} />
      <Stack.Screen name="addresses" options={{ title: "배송지 관리" }} />
      <Stack.Screen name="wishlist" options={{ title: "위시리스트" }} />
      <Stack.Screen name="reviews" options={{ title: "리뷰 관리" }} />
      <Stack.Screen name="qna" options={{ title: "Q&A" }} />
      <Stack.Screen name="coupons" options={{ title: "쿠폰함" }} />
      <Stack.Screen name="notifications" options={{ title: "알림" }} />
    </Stack>
  );
}
