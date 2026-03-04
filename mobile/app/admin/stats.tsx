import { useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { formatPrice } from "../../src/lib/theme";
import api from "../../src/lib/api";

interface SalesStats {
  totalRevenue: number;
  totalOrders: number;
  averageOrderValue: number;
  topProducts?: { name: string; sales: number; revenue: number }[];
}

type Period = "today" | "week" | "month";

export default function StatsScreen() {
  const theme = useTheme();
  const [stats, setStats] = useState<SalesStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [period, setPeriod] = useState<Period>("today");

  const load = async () => {
    setIsLoading(true);
    try {
      const res = await api.get("/admin/analytics/sales", {
        params: { period },
      });
      setStats(res.data);
    } catch {}
    setIsLoading(false);
  };

  useEffect(() => {
    load();
  }, [period]);

  const periods: { key: Period; label: string }[] = [
    { key: "today", label: "오늘" },
    { key: "week", label: "이번 주" },
    { key: "month", label: "이번 달" },
  ];

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: theme.background }]}
    >
      {/* Period Selector */}
      <View style={[styles.periodBar, { backgroundColor: theme.surface }]}>
        {periods.map((p) => (
          <TouchableOpacity
            key={p.key}
            style={[
              styles.periodTab,
              period === p.key
                ? { backgroundColor: theme.secondary }
                : { backgroundColor: theme.background },
            ]}
            onPress={() => setPeriod(p.key)}
          >
            <Text
              style={{
                color: period === p.key ? "#FFF" : theme.textSecondary,
                fontWeight: "600",
                fontSize: 14,
              }}
            >
              {p.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {isLoading ? (
        <View style={styles.loadingBox}>
          <ActivityIndicator size="large" color={theme.primary} />
        </View>
      ) : (
        <>
          {/* Summary Cards */}
          <View style={styles.summaryGrid}>
            <View style={[styles.summaryCard, { backgroundColor: theme.surface }]}>
              <Ionicons name="cash-outline" size={28} color="#10B981" />
              <Text style={[styles.summaryValue, { color: theme.text }]}>
                {formatPrice(stats?.totalRevenue || 0)}
              </Text>
              <Text style={[styles.summaryLabel, { color: theme.textSecondary }]}>
                총 매출
              </Text>
            </View>
            <View style={[styles.summaryCard, { backgroundColor: theme.surface }]}>
              <Ionicons name="receipt-outline" size={28} color="#3B82F6" />
              <Text style={[styles.summaryValue, { color: theme.text }]}>
                {stats?.totalOrders || 0}건
              </Text>
              <Text style={[styles.summaryLabel, { color: theme.textSecondary }]}>
                주문 수
              </Text>
            </View>
            <View style={[styles.summaryCard, { backgroundColor: theme.surface }]}>
              <Ionicons name="trending-up-outline" size={28} color="#8B5CF6" />
              <Text style={[styles.summaryValue, { color: theme.text }]}>
                {formatPrice(stats?.averageOrderValue || 0)}
              </Text>
              <Text style={[styles.summaryLabel, { color: theme.textSecondary }]}>
                평균 주문액
              </Text>
            </View>
          </View>

          {/* Top Products */}
          {stats?.topProducts && stats.topProducts.length > 0 && (
            <View style={[styles.section, { backgroundColor: theme.surface }]}>
              <Text style={[styles.sectionTitle, { color: theme.text }]}>
                인기 상품 TOP 5
              </Text>
              {stats.topProducts.slice(0, 5).map((product, idx) => (
                <View key={idx} style={styles.rankItem}>
                  <View
                    style={[
                      styles.rankBadge,
                      {
                        backgroundColor:
                          idx === 0
                            ? "#F59E0B"
                            : idx === 1
                              ? "#94A3B8"
                              : idx === 2
                                ? "#CD7F32"
                                : theme.border,
                      },
                    ]}
                  >
                    <Text style={styles.rankNumber}>{idx + 1}</Text>
                  </View>
                  <View style={{ flex: 1 }}>
                    <Text
                      style={[styles.rankName, { color: theme.text }]}
                      numberOfLines={1}
                    >
                      {product.name}
                    </Text>
                    <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
                      {product.sales}개 판매
                    </Text>
                  </View>
                  <Text style={[styles.rankRevenue, { color: theme.secondary }]}>
                    {formatPrice(product.revenue)}
                  </Text>
                </View>
              ))}
            </View>
          )}
        </>
      )}

      <View style={{ height: 40 }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  periodBar: {
    flexDirection: "row",
    padding: 12,
    gap: 8,
  },
  periodTab: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 8,
    alignItems: "center",
  },
  loadingBox: {
    padding: 60,
    alignItems: "center",
  },
  summaryGrid: {
    flexDirection: "row",
    padding: 12,
    gap: 8,
  },
  summaryCard: {
    flex: 1,
    padding: 16,
    borderRadius: 12,
    alignItems: "center",
    gap: 8,
  },
  summaryValue: { fontSize: 16, fontWeight: "700" },
  summaryLabel: { fontSize: 12 },
  section: {
    margin: 16,
    padding: 16,
    borderRadius: 12,
  },
  sectionTitle: { fontSize: 16, fontWeight: "700", marginBottom: 12 },
  rankItem: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingVertical: 10,
    borderBottomWidth: 0.5,
    borderBottomColor: "#F0F0F0",
  },
  rankBadge: {
    width: 28,
    height: 28,
    borderRadius: 14,
    alignItems: "center",
    justifyContent: "center",
  },
  rankNumber: { color: "#FFF", fontSize: 13, fontWeight: "700" },
  rankName: { fontSize: 14, fontWeight: "500" },
  rankRevenue: { fontSize: 14, fontWeight: "600" },
});
