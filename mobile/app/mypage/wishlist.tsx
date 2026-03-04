import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from "react-native";
import { useRouter } from "expo-router";
import { useTheme } from "../../src/hooks/useTheme";
import { ProductCard } from "../../src/components/ui/ProductCard";
import { getWishlist, toggleWishlist } from "../../src/lib/api";
import { EmptyState } from "../../src/components/ui/EmptyState";
import type { Product } from "../../src/types";

export default function WishlistScreen() {
  const theme = useTheme();
  const router = useRouter();
  const [products, setProducts] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const load = async () => {
    try {
      const res = await getWishlist();
      setProducts(res.data.items || res.data);
    } catch {}
    setIsLoading(false);
  };

  useEffect(() => {
    load();
  }, []);

  const handleRemove = async (productId: number) => {
    Alert.alert("위시리스트", "위시리스트에서 삭제하시겠습니까?", [
      { text: "취소" },
      {
        text: "삭제",
        style: "destructive",
        onPress: async () => {
          await toggleWishlist(productId);
          setProducts((prev) => prev.filter((p) => p.id !== productId));
        },
      },
    ]);
  };

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
        icon="heart-outline"
        title="위시리스트가 비어있습니다"
        description="마음에 드는 상품의 ♡를 눌러 추가해보세요"
        actionLabel="상품 둘러보기"
        onAction={() => router.push("/(tabs)/products" as never)}
      />
    );
  }

  return (
    <FlatList
      style={[styles.container, { backgroundColor: theme.background }]}
      data={products}
      numColumns={2}
      keyExtractor={(item) => String(item.id)}
      contentContainerStyle={styles.grid}
      columnWrapperStyle={styles.row}
      renderItem={({ item }) => (
        <View style={styles.cardWrapper}>
          <ProductCard product={item} />
        </View>
      )}
    />
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  grid: { padding: 8 },
  row: { gap: 8, paddingHorizontal: 8 },
  cardWrapper: { flex: 1 },
});
