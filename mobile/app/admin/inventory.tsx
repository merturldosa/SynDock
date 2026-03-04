import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  ActivityIndicator,
} from "react-native";
import { Image } from "expo-image";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { EmptyState } from "../../src/components/ui/EmptyState";
import api from "../../src/lib/api";

interface LowStockProduct {
  id: number;
  name: string;
  imageUrl?: string;
  stock: number;
  categoryName?: string;
}

export default function InventoryAlertScreen() {
  const theme = useTheme();
  const [products, setProducts] = useState<LowStockProduct[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    api
      .get("/admin/products/low-stock")
      .then((r) => setProducts(r.data.items || r.data))
      .catch(() => {})
      .finally(() => setIsLoading(false));
  }, []);

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  if (products.length === 0) {
    return (
      <EmptyState
        icon="checkmark-circle-outline"
        title="재고 부족 상품이 없습니다"
        description="모든 상품의 재고가 충분합니다"
      />
    );
  }

  return (
    <FlatList
      style={[styles.container, { backgroundColor: theme.background }]}
      data={products}
      keyExtractor={(item) => String(item.id)}
      ListHeaderComponent={
        <View style={[styles.header, { backgroundColor: theme.error + "10" }]}>
          <Ionicons name="alert-circle" size={20} color={theme.error} />
          <Text style={{ color: theme.error, fontSize: 13, fontWeight: "500" }}>
            재고 10개 미만 상품 {products.length}건
          </Text>
        </View>
      }
      renderItem={({ item }) => {
        const stockColor =
          item.stock === 0
            ? "#EF4444"
            : item.stock < 5
              ? "#F59E0B"
              : "#3B82F6";

        return (
          <View style={[styles.card, { backgroundColor: theme.surface }]}>
            <Image
              source={{
                uri: item.imageUrl || "https://via.placeholder.com/60",
              }}
              style={styles.image}
              contentFit="cover"
            />
            <View style={styles.info}>
              <Text
                style={[styles.productName, { color: theme.text }]}
                numberOfLines={2}
              >
                {item.name}
              </Text>
              {item.categoryName && (
                <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
                  {item.categoryName}
                </Text>
              )}
            </View>
            <View style={styles.stockInfo}>
              <View
                style={[
                  styles.stockBadge,
                  { backgroundColor: stockColor + "20" },
                ]}
              >
                <Text
                  style={[styles.stockText, { color: stockColor }]}
                >
                  {item.stock === 0 ? "품절" : `${item.stock}개`}
                </Text>
              </View>
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
  header: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    margin: 16,
    padding: 12,
    borderRadius: 8,
  },
  card: {
    flexDirection: "row",
    alignItems: "center",
    marginHorizontal: 16,
    marginBottom: 8,
    padding: 12,
    borderRadius: 12,
    gap: 12,
  },
  image: {
    width: 56,
    height: 56,
    borderRadius: 8,
  },
  info: { flex: 1, gap: 2 },
  productName: { fontSize: 14, fontWeight: "500" },
  stockInfo: { alignItems: "flex-end" },
  stockBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
  },
  stockText: { fontSize: 14, fontWeight: "700" },
});
