import { useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  RefreshControl,
} from "react-native";
import { useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { formatPrice } from "../../src/lib/theme";
import api from "../../src/lib/api";

interface DashboardStats {
  todaySales: number;
  todayOrders: number;
  pendingOrders: number;
  lowStockCount: number;
  totalProducts: number;
  totalMembers: number;
}

export default function AdminDashboardScreen() {
  const theme = useTheme();
  const router = useRouter();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = async () => {
    try {
      const res = await api.get("/admin/dashboard");
      setStats(res.data);
    } catch {}
    setIsLoading(false);
    setRefreshing(false);
  };

  useEffect(() => {
    load();
  }, []);

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  const cards = [
    {
      icon: "cash-outline" as const,
      label: "오늘 매출",
      value: formatPrice(stats?.todaySales || 0),
      color: "#10B981",
    },
    {
      icon: "receipt-outline" as const,
      label: "오늘 주문",
      value: `${stats?.todayOrders || 0}건`,
      color: "#3B82F6",
    },
    {
      icon: "time-outline" as const,
      label: "처리 대기",
      value: `${stats?.pendingOrders || 0}건`,
      color: "#F59E0B",
      onPress: () => router.push("/admin/orders" as never),
    },
    {
      icon: "alert-circle-outline" as const,
      label: "재고 부족",
      value: `${stats?.lowStockCount || 0}건`,
      color: "#EF4444",
      onPress: () => router.push("/admin/inventory" as never),
    },
  ];

  const menuItems = [
    {
      icon: "receipt-outline" as const,
      label: "주문 관리",
      description: "주문 확인/처리/배송 관리",
      route: "/admin/orders",
    },
    {
      icon: "cube-outline" as const,
      label: "재고 알림",
      description: "재고 부족 상품 확인",
      route: "/admin/inventory",
    },
    {
      icon: "bar-chart-outline" as const,
      label: "매출 통계",
      description: "매출/주문/고객 분석",
      route: "/admin/stats",
    },
  ];

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: theme.background }]}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={() => {
            setRefreshing(true);
            load();
          }}
        />
      }
    >
      {/* Summary Cards */}
      <View style={styles.cardsGrid}>
        {cards.map((card) => (
          <TouchableOpacity
            key={card.label}
            style={[styles.card, { backgroundColor: theme.surface }]}
            onPress={card.onPress}
            disabled={!card.onPress}
          >
            <Ionicons name={card.icon} size={24} color={card.color} />
            <Text style={[styles.cardValue, { color: theme.text }]}>
              {card.value}
            </Text>
            <Text style={[styles.cardLabel, { color: theme.textSecondary }]}>
              {card.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Quick Stats */}
      <View style={[styles.quickStats, { backgroundColor: theme.surface }]}>
        <View style={styles.quickStatItem}>
          <Ionicons name="bag-outline" size={20} color={theme.primary} />
          <Text style={{ color: theme.textSecondary, fontSize: 13 }}>
            전체 상품
          </Text>
          <Text style={[styles.quickStatValue, { color: theme.text }]}>
            {stats?.totalProducts || 0}
          </Text>
        </View>
        <View
          style={[styles.quickStatDivider, { backgroundColor: theme.border }]}
        />
        <View style={styles.quickStatItem}>
          <Ionicons name="people-outline" size={20} color={theme.primary} />
          <Text style={{ color: theme.textSecondary, fontSize: 13 }}>
            전체 회원
          </Text>
          <Text style={[styles.quickStatValue, { color: theme.text }]}>
            {stats?.totalMembers || 0}
          </Text>
        </View>
      </View>

      {/* Menu */}
      <View style={[styles.menuCard, { backgroundColor: theme.surface }]}>
        {menuItems.map((item) => (
          <TouchableOpacity
            key={item.label}
            style={styles.menuItem}
            onPress={() => router.push(item.route as never)}
          >
            <Ionicons name={item.icon} size={24} color={theme.secondary} />
            <View style={{ flex: 1 }}>
              <Text style={[styles.menuLabel, { color: theme.text }]}>
                {item.label}
              </Text>
              <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
                {item.description}
              </Text>
            </View>
            <Ionicons
              name="chevron-forward"
              size={18}
              color={theme.textSecondary}
            />
          </TouchableOpacity>
        ))}
      </View>

      <View style={{ height: 40 }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  cardsGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    padding: 12,
    gap: 8,
  },
  card: {
    width: "48%",
    flexGrow: 1,
    padding: 16,
    borderRadius: 12,
    alignItems: "center",
    gap: 8,
  },
  cardValue: { fontSize: 20, fontWeight: "700" },
  cardLabel: { fontSize: 13 },
  quickStats: {
    flexDirection: "row",
    marginHorizontal: 16,
    borderRadius: 12,
    padding: 16,
  },
  quickStatItem: { flex: 1, alignItems: "center", gap: 4 },
  quickStatDivider: { width: 1 },
  quickStatValue: { fontSize: 18, fontWeight: "700" },
  menuCard: {
    margin: 16,
    borderRadius: 12,
    overflow: "hidden",
  },
  menuItem: {
    flexDirection: "row",
    alignItems: "center",
    padding: 16,
    gap: 12,
    borderBottomWidth: 0.5,
    borderBottomColor: "#F0F0F0",
  },
  menuLabel: { fontSize: 15, fontWeight: "600" },
});
