import { useEffect } from "react";
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
} from "react-native";
import { Image } from "expo-image";
import { useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { Button } from "../../src/components/ui/Button";
import { EmptyState } from "../../src/components/ui/EmptyState";
import { useCartStore } from "../../src/stores/cartStore";
import { useAuthStore } from "../../src/stores/authStore";
import { formatPrice } from "../../src/lib/theme";

export default function CartScreen() {
  const theme = useTheme();
  const router = useRouter();
  const { cart, fetchCart, updateQuantity, removeItem, clearCart } =
    useCartStore();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  useEffect(() => {
    if (isAuthenticated) fetchCart();
  }, [isAuthenticated]);

  if (!isAuthenticated) {
    return (
      <EmptyState
        icon="cart-outline"
        title="로그인이 필요합니다"
        description="장바구니를 이용하려면 로그인해주세요"
        actionLabel="로그인"
        onAction={() => router.push("/(auth)/login" as never)}
      />
    );
  }

  if (cart.items.length === 0) {
    return (
      <EmptyState
        icon="cart-outline"
        title="장바구니가 비어있습니다"
        description="마음에 드는 상품을 담아보세요"
        actionLabel="쇼핑하기"
        onAction={() => router.push("/(tabs)/products" as never)}
      />
    );
  }

  return (
    <View style={[styles.container, { backgroundColor: theme.background }]}>
      <FlatList
        data={cart.items}
        keyExtractor={(item) => item.id.toString()}
        contentContainerStyle={{ padding: 16 }}
        ItemSeparatorComponent={() => (
          <View
            style={[styles.separator, { backgroundColor: theme.border }]}
          />
        )}
        renderItem={({ item }) => (
          <View style={[styles.cartItem, { backgroundColor: theme.surface }]}>
            <Image
              source={{ uri: item.productImage || "https://via.placeholder.com/80" }}
              style={styles.itemImage}
              contentFit="cover"
            />
            <View style={styles.itemInfo}>
              <Text
                style={[styles.itemName, { color: theme.text }]}
                numberOfLines={2}
              >
                {item.productName}
              </Text>
              {item.variantName && (
                <Text
                  style={[styles.variant, { color: theme.textSecondary }]}
                >
                  {item.variantName}
                </Text>
              )}
              <Text style={[styles.itemPrice, { color: theme.secondary }]}>
                {formatPrice(
                  (item.salePrice || item.price) * item.quantity
                )}
              </Text>
              <View style={styles.quantityRow}>
                <TouchableOpacity
                  style={[styles.qtyBtn, { borderColor: theme.border }]}
                  onPress={() =>
                    updateQuantity(item.id, Math.max(1, item.quantity - 1))
                  }
                >
                  <Ionicons name="remove" size={16} color={theme.text} />
                </TouchableOpacity>
                <Text style={[styles.qtyText, { color: theme.text }]}>
                  {item.quantity}
                </Text>
                <TouchableOpacity
                  style={[styles.qtyBtn, { borderColor: theme.border }]}
                  onPress={() => updateQuantity(item.id, item.quantity + 1)}
                >
                  <Ionicons name="add" size={16} color={theme.text} />
                </TouchableOpacity>
                <TouchableOpacity
                  onPress={() => removeItem(item.id)}
                  style={{ marginLeft: "auto" }}
                >
                  <Ionicons name="trash-outline" size={20} color={theme.error} />
                </TouchableOpacity>
              </View>
            </View>
          </View>
        )}
      />

      {/* Order Summary */}
      <View style={[styles.summary, { backgroundColor: theme.surface }]}>
        <View style={styles.summaryRow}>
          <Text style={[styles.summaryLabel, { color: theme.textSecondary }]}>
            합계 ({cart.totalQuantity}개)
          </Text>
          <Text style={[styles.summaryTotal, { color: theme.secondary }]}>
            {formatPrice(cart.totalAmount)}
          </Text>
        </View>
        <Button
          title="주문하기"
          onPress={() => router.push("/order" as never)}
          size="lg"
          style={{ marginTop: 12 }}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  separator: { height: 1, marginVertical: 12 },
  cartItem: { flexDirection: "row", gap: 12, padding: 12, borderRadius: 12 },
  itemImage: { width: 80, height: 80, borderRadius: 8 },
  itemInfo: { flex: 1 },
  itemName: { fontSize: 14, fontWeight: "500" },
  variant: { fontSize: 12, marginTop: 2 },
  itemPrice: { fontSize: 16, fontWeight: "700", marginTop: 4 },
  quantityRow: {
    flexDirection: "row",
    alignItems: "center",
    marginTop: 8,
    gap: 8,
  },
  qtyBtn: {
    width: 28,
    height: 28,
    borderWidth: 1,
    borderRadius: 6,
    alignItems: "center",
    justifyContent: "center",
  },
  qtyText: { fontSize: 14, fontWeight: "600", minWidth: 20, textAlign: "center" },
  summary: {
    padding: 16,
    borderTopWidth: 1,
    borderTopColor: "#E5E5E5",
  },
  summaryRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  summaryLabel: { fontSize: 14 },
  summaryTotal: { fontSize: 20, fontWeight: "700" },
});
