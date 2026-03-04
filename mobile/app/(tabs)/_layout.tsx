import { Tabs } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { View } from "react-native";
import { useTheme } from "../../src/hooks/useTheme";
import { Badge } from "../../src/components/ui/Badge";
import { useCartStore } from "../../src/stores/cartStore";

export default function TabLayout() {
  const theme = useTheme();
  const totalQuantity = useCartStore((s) => s.cart.totalQuantity);

  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: theme.secondary,
        tabBarInactiveTintColor: theme.textSecondary,
        tabBarStyle: {
          backgroundColor: theme.surface,
          borderTopColor: theme.border,
          height: 60,
          paddingBottom: 8,
        },
        headerStyle: { backgroundColor: theme.surface },
        headerTintColor: theme.secondary,
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: "홈",
          headerShown: false,
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="home-outline" size={size} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="products"
        options={{
          title: "상품",
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="grid-outline" size={size} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="cart"
        options={{
          title: "장바구니",
          tabBarIcon: ({ color, size }) => (
            <View>
              <Ionicons name="cart-outline" size={size} color={color} />
              <Badge count={totalQuantity} />
            </View>
          ),
        }}
      />
      <Tabs.Screen
        name="mypage"
        options={{
          title: "마이",
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="person-outline" size={size} color={color} />
          ),
        }}
      />
    </Tabs>
  );
}
