import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { Button } from "../../src/components/ui/Button";
import { EmptyState } from "../../src/components/ui/EmptyState";
import { formatPrice } from "../../src/lib/theme";
import api from "../../src/lib/api";
import type { Order } from "../../src/types";

const STATUS_ACTIONS: Record<string, { next: string; label: string }> = {
  Pending: { next: "Confirmed", label: "주문 확인" },
  Confirmed: { next: "Shipping", label: "배송 시작" },
  Shipping: { next: "Delivered", label: "배송 완료" },
};

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending: { label: "주문 접수", color: "#F59E0B" },
  Confirmed: { label: "주문 확인", color: "#3B82F6" },
  Shipping: { label: "배송 중", color: "#8B5CF6" },
  Delivered: { label: "배송 완료", color: "#10B981" },
  Cancelled: { label: "취소", color: "#EF4444" },
};

type FilterStatus = "all" | "Pending" | "Confirmed" | "Shipping";

export default function AdminOrdersScreen() {
  const theme = useTheme();
  const [orders, setOrders] = useState<Order[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filter, setFilter] = useState<FilterStatus>("all");

  const load = async () => {
    try {
      const params: Record<string, unknown> = { pageSize: 50 };
      if (filter !== "all") params.status = filter;
      const res = await api.get("/admin/orders", { params });
      setOrders(res.data.items || res.data);
    } catch {}
    setIsLoading(false);
  };

  useEffect(() => {
    setIsLoading(true);
    load();
  }, [filter]);

  const handleStatusChange = async (orderId: number, newStatus: string) => {
    try {
      await api.put(`/admin/orders/${orderId}/status`, { status: newStatus });
      load();
    } catch (err: any) {
      Alert.alert("처리 실패", err.response?.data?.error || "다시 시도해주세요");
    }
  };

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return `${String(d.getMonth() + 1).padStart(2, "0")}/${String(d.getDate()).padStart(2, "0")} ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
  };

  const filters: { key: FilterStatus; label: string }[] = [
    { key: "all", label: "전체" },
    { key: "Pending", label: "대기" },
    { key: "Confirmed", label: "확인" },
    { key: "Shipping", label: "배송중" },
  ];

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  return (
    <View style={[styles.container, { backgroundColor: theme.background }]}>
      {/* Filter Tabs */}
      <View style={[styles.filterBar, { backgroundColor: theme.surface }]}>
        {filters.map((f) => (
          <TouchableOpacity
            key={f.key}
            style={[
              styles.filterTab,
              filter === f.key && {
                backgroundColor: theme.secondary,
                borderColor: theme.secondary,
              },
              filter !== f.key && { borderColor: theme.border },
            ]}
            onPress={() => setFilter(f.key)}
          >
            <Text
              style={{
                color: filter === f.key ? "#FFF" : theme.textSecondary,
                fontSize: 13,
                fontWeight: "600",
              }}
            >
              {f.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <FlatList
        data={orders}
        keyExtractor={(item) => String(item.id)}
        contentContainerStyle={orders.length === 0 ? { flex: 1 } : undefined}
        ListEmptyComponent={
          <EmptyState
            icon="receipt-outline"
            title="주문이 없습니다"
            description="해당 상태의 주문이 없습니다"
          />
        }
        renderItem={({ item }) => {
          const status = STATUS_LABELS[item.status] || {
            label: item.status,
            color: "#999",
          };
          const action = STATUS_ACTIONS[item.status];

          return (
            <View style={[styles.orderCard, { backgroundColor: theme.surface }]}>
              <View style={styles.orderHeader}>
                <View>
                  <Text style={[styles.orderNumber, { color: theme.text }]}>
                    {item.orderNumber}
                  </Text>
                  <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
                    {formatDate(item.createdAt)}
                  </Text>
                </View>
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

              {item.items?.map((oi) => (
                <Text
                  key={oi.id}
                  style={{ color: theme.textSecondary, fontSize: 13 }}
                  numberOfLines={1}
                >
                  {oi.productName} x{oi.quantity}
                </Text>
              ))}

              <View style={styles.orderFooter}>
                <Text style={[styles.orderTotal, { color: theme.secondary }]}>
                  {formatPrice(item.totalAmount)}
                </Text>
                {action && (
                  <Button
                    title={action.label}
                    size="sm"
                    onPress={() =>
                      Alert.alert(
                        action.label,
                        `주문 ${item.orderNumber}을 "${action.label}" 처리하시겠습니까?`,
                        [
                          { text: "취소" },
                          {
                            text: "확인",
                            onPress: () =>
                              handleStatusChange(item.id, action.next),
                          },
                        ]
                      )
                    }
                  />
                )}
              </View>
            </View>
          );
        }}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  filterBar: {
    flexDirection: "row",
    padding: 12,
    gap: 8,
    borderBottomWidth: 0.5,
    borderBottomColor: "#F0F0F0",
  },
  filterTab: {
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
  },
  orderCard: {
    margin: 16,
    marginBottom: 0,
    padding: 16,
    borderRadius: 12,
  },
  orderHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    marginBottom: 8,
  },
  orderNumber: { fontSize: 15, fontWeight: "600" },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
  },
  statusText: { fontSize: 12, fontWeight: "600" },
  orderFooter: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 0.5,
    borderTopColor: "#F0F0F0",
  },
  orderTotal: { fontSize: 17, fontWeight: "700" },
});
