import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  ActivityIndicator,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { getMyCoupons } from "../../src/lib/api";
import { EmptyState } from "../../src/components/ui/EmptyState";
import { formatPrice } from "../../src/lib/theme";

interface Coupon {
  id: number;
  code: string;
  name: string;
  discountType: string;
  discountValue: number;
  minOrderAmount?: number;
  expiresAt?: string;
  isUsed: boolean;
}

export default function CouponsScreen() {
  const theme = useTheme();
  const [coupons, setCoupons] = useState<Coupon[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    getMyCoupons()
      .then((r) => setCoupons(r.data.items || r.data))
      .catch(() => {})
      .finally(() => setIsLoading(false));
  }, []);

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

  if (coupons.length === 0) {
    return (
      <EmptyState
        icon="ticket-outline"
        title="보유 쿠폰이 없습니다"
        description="이벤트에 참여하여 쿠폰을 받아보세요"
      />
    );
  }

  return (
    <FlatList
      style={[styles.container, { backgroundColor: theme.background }]}
      data={coupons}
      keyExtractor={(item) => String(item.id)}
      renderItem={({ item }) => {
        const isExpired =
          item.expiresAt && new Date(item.expiresAt) < new Date();
        const disabled = item.isUsed || isExpired;

        return (
          <View
            style={[
              styles.couponCard,
              { backgroundColor: theme.surface, opacity: disabled ? 0.5 : 1 },
            ]}
          >
            <View style={[styles.discountBadge, { backgroundColor: theme.secondary }]}>
              <Text style={styles.discountText}>
                {item.discountType === "Percentage"
                  ? `${item.discountValue}%`
                  : formatPrice(item.discountValue)}
              </Text>
              <Text style={styles.discountLabel}>할인</Text>
            </View>
            <View style={styles.couponInfo}>
              <Text style={[styles.couponName, { color: theme.text }]}>
                {item.name}
              </Text>
              <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
                코드: {item.code}
              </Text>
              {item.minOrderAmount && (
                <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
                  {formatPrice(item.minOrderAmount)} 이상 구매 시
                </Text>
              )}
              {item.expiresAt && (
                <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
                  ~{formatDate(item.expiresAt)}
                </Text>
              )}
              {item.isUsed && (
                <Text style={{ color: theme.error, fontSize: 12, fontWeight: "600" }}>
                  사용 완료
                </Text>
              )}
            </View>
          </View>
        );
      }}
    />
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  couponCard: {
    flexDirection: "row",
    margin: 16,
    marginBottom: 0,
    borderRadius: 12,
    overflow: "hidden",
  },
  discountBadge: {
    width: 80,
    alignItems: "center",
    justifyContent: "center",
    padding: 12,
  },
  discountText: {
    color: "#FFF",
    fontSize: 18,
    fontWeight: "700",
  },
  discountLabel: {
    color: "rgba(255,255,255,0.8)",
    fontSize: 11,
    marginTop: 2,
  },
  couponInfo: {
    flex: 1,
    padding: 12,
    gap: 2,
  },
  couponName: {
    fontSize: 15,
    fontWeight: "600",
    marginBottom: 4,
  },
});
