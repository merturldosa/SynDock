import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
} from "react-native";
import { useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { getOrders } from "../../src/lib/api";
import { formatPrice } from "../../src/lib/theme";
import { EmptyState } from "../../src/components/ui/EmptyState";
import type { Order } from "../../src/types";

const STATUS_MAP: Record<string, { label: string; color: string }> = {
  Pending: { label: "주문 접수", color: "#F59E0B" },
  Confirmed: { label: "주문 확인", color: "#3B82F6" },
  Shipping: { label: "배송 중", color: "#8B5CF6" },
  Delivered: { label: "배송 완료", color: "#10B981" },
  Cancelled: { label: "취소", color: "#EF4444" },
};

export default function OrdersScreen() {
  const theme = useTheme();
  const router = useRouter();
  const [orders, setOrders] = useState<Order[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  const loadOrders = async (p = 1) => {
    try {
      const res = await getOrders({ page: p, pageSize: 20 });
      if (p === 1) {
        setOrders(res.data.items);
      } else {
        setOrders((prev) => [...prev, ...res.data.items]);
      }
      setHasMore(res.data.hasNext);
    } catch {}
    setIsLoading(false);
  };

  useEffect(() => {
    loadOrders();
  }, []);

  const loadMore = () => {
    if (!hasMore) return;
    const next = page + 1;
    setPage(next);
    loadOrders(next);
  };

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return `${d.getFullYear()}.${String(d.getMonth() + 1).padStart(2, "0")}.${String(d.getDate()).padStart(2, "0")}`;
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  if (orders.length === 0) {
    return (
      <EmptyState
        icon="receipt-outline"
        title="주문 내역이 없습니다"
        description="상품을 주문하면 여기에 표시됩니다"
        actionLabel="쇼핑하러 가기"
        onAction={() => router.push("/(tabs)/products" as never)}
      />
    );
  }

  return (
    <FlatList
      style={[styles.container, { backgroundColor: theme.background }]}
      data={orders}
      keyExtractor={(item) => String(item.id)}
      onEndReached={loadMore}
      onEndReachedThreshold={0.5}
      renderItem={({ item }) => {
        const status = STATUS_MAP[item.status] || {
          label: item.status,
          color: "#999",
        };
        return (
          <TouchableOpacity
            style={[styles.orderCard, { backgroundColor: theme.surface }]}
            onPress={() => router.push(`/mypage/order-detail?id=${item.id}` as never)}
          >
            <View style={styles.orderHeader}>
              <Text style={[styles.orderNumber, { color: theme.text }]}>
                {item.orderNumber}
              </Text>
              <View
                style={[
                  styles.statusBadge,
                  { backgroundColor: status.color + "20" },
                ]}
              >
                <Text style={[styles.statusText, { color: status.color }]}>
                  {status.label}
                </Text>
              </View>
            </View>
            <Text style={{ color: theme.textSecondary, fontSize: 13 }}>
              {formatDate(item.createdAt)}
            </Text>
            {item.items?.slice(0, 2).map((oi) => (
              <Text
                key={oi.id}
                style={{ color: theme.textSecondary, fontSize: 13, marginTop: 4 }}
                numberOfLines={1}
              >
                {oi.productName} x{oi.quantity}
              </Text>
            ))}
            {item.items && item.items.length > 2 && (
              <Text style={{ color: theme.textSecondary, fontSize: 13 }}>
                외 {item.items.length - 2}건
              </Text>
            )}
            <Text style={[styles.orderTotal, { color: theme.secondary }]}>
              {formatPrice(item.totalAmount)}
            </Text>
          </TouchableOpacity>
        );
      }}
    />
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  orderCard: {
    margin: 16,
    marginBottom: 0,
    padding: 16,
    borderRadius: 12,
  },
  orderHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 4,
  },
  orderNumber: { fontSize: 15, fontWeight: "600" },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
  },
  statusText: { fontSize: 12, fontWeight: "600" },
  orderTotal: {
    fontSize: 17,
    fontWeight: "700",
    marginTop: 8,
    textAlign: "right",
  },
});
