import { useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from "react-native";
import { Image } from "expo-image";
import { useLocalSearchParams, useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { Button } from "../../src/components/ui/Button";
import { getProductById, toggleWishlist } from "../../src/lib/api";
import { useCartStore } from "../../src/stores/cartStore";
import { useAuthStore } from "../../src/stores/authStore";
import { formatPrice } from "../../src/lib/theme";
import type { Product } from "../../src/types";

export default function ProductDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const theme = useTheme();
  const router = useRouter();
  const [product, setProduct] = useState<Product | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [quantity, setQuantity] = useState(1);
  const addToCart = useCartStore((s) => s.addToCart);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  useEffect(() => {
    if (id) {
      getProductById(Number(id))
        .then((r) => setProduct(r.data))
        .catch(() => {})
        .finally(() => setIsLoading(false));
    }
  }, [id]);

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      Alert.alert("로그인 필요", "장바구니에 담으려면 로그인이 필요합니다", [
        { text: "취소" },
        {
          text: "로그인",
          onPress: () => router.push("/(auth)/login" as never),
        },
      ]);
      return;
    }
    await addToCart(Number(id), quantity);
    Alert.alert("장바구니", "상품이 장바구니에 담겼습니다", [
      { text: "쇼핑 계속" },
      {
        text: "장바구니 보기",
        onPress: () => router.push("/(tabs)/cart" as never),
      },
    ]);
  };

  const handleWishlist = async () => {
    if (!isAuthenticated) {
      router.push("/(auth)/login" as never);
      return;
    }
    await toggleWishlist(Number(id));
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  if (!product) {
    return (
      <View style={styles.center}>
        <Text style={{ color: theme.textSecondary }}>상품을 찾을 수 없습니다</Text>
      </View>
    );
  }

  const discount =
    product.salePrice && product.price > 0
      ? Math.round(
          ((product.price - product.salePrice) / product.price) * 100
        )
      : 0;

  return (
    <View style={[styles.container, { backgroundColor: theme.background }]}>
      <ScrollView>
        <Image
          source={{ uri: product.imageUrl || "https://via.placeholder.com/400" }}
          style={styles.productImage}
          contentFit="cover"
        />

        <View style={[styles.infoSection, { backgroundColor: theme.surface }]}>
          {product.categoryName && (
            <Text style={[styles.category, { color: theme.primary }]}>
              {product.categoryName}
            </Text>
          )}
          <Text style={[styles.name, { color: theme.text }]}>
            {product.name}
          </Text>

          <View style={styles.priceSection}>
            {discount > 0 && (
              <Text style={[styles.discount, { color: theme.error }]}>
                {discount}%
              </Text>
            )}
            <Text style={[styles.price, { color: theme.secondary }]}>
              {formatPrice(product.salePrice || product.price)}
            </Text>
            {product.salePrice && (
              <Text style={styles.originalPrice}>
                {formatPrice(product.price)}
              </Text>
            )}
          </View>

          {product.rating !== undefined && (
            <View style={styles.ratingRow}>
              <Ionicons name="star" size={16} color="#F59E0B" />
              <Text style={{ color: theme.text, fontWeight: "600" }}>
                {product.rating.toFixed(1)}
              </Text>
              <Text style={{ color: theme.textSecondary }}>
                ({product.reviewCount || 0}개 리뷰)
              </Text>
            </View>
          )}

          {product.stock > 0 && product.stock < 10 && (
            <Text style={[styles.stockWarning, { color: theme.warning }]}>
              재고 {product.stock}개 남음
            </Text>
          )}
        </View>

        {product.description && (
          <View
            style={[
              styles.descriptionSection,
              { backgroundColor: theme.surface },
            ]}
          >
            <Text style={[styles.sectionTitle, { color: theme.text }]}>
              상품 설명
            </Text>
            <Text
              style={[styles.description, { color: theme.textSecondary }]}
            >
              {product.description}
            </Text>
          </View>
        )}
      </ScrollView>

      {/* Bottom Bar */}
      <View style={[styles.bottomBar, { backgroundColor: theme.surface }]}>
        <Button
          title="♡"
          onPress={handleWishlist}
          variant="outline"
          style={{ width: 48 }}
        />
        <View style={styles.quantityControl}>
          <Button
            title="−"
            onPress={() => setQuantity(Math.max(1, quantity - 1))}
            variant="outline"
            size="sm"
            style={{ width: 36 }}
          />
          <Text style={[styles.qtyText, { color: theme.text }]}>
            {quantity}
          </Text>
          <Button
            title="+"
            onPress={() => setQuantity(quantity + 1)}
            variant="outline"
            size="sm"
            style={{ width: 36 }}
          />
        </View>
        <Button
          title="장바구니 담기"
          onPress={handleAddToCart}
          style={{ flex: 1 }}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  productImage: { width: "100%", height: 400 },
  infoSection: { padding: 20 },
  category: { fontSize: 13, fontWeight: "500", marginBottom: 4 },
  name: { fontSize: 20, fontWeight: "700", lineHeight: 28 },
  priceSection: { flexDirection: "row", alignItems: "baseline", gap: 8, marginTop: 12 },
  discount: { fontSize: 20, fontWeight: "700" },
  price: { fontSize: 22, fontWeight: "700" },
  originalPrice: {
    fontSize: 16,
    color: "#999",
    textDecorationLine: "line-through",
  },
  ratingRow: { flexDirection: "row", alignItems: "center", gap: 4, marginTop: 8 },
  stockWarning: { fontSize: 13, fontWeight: "500", marginTop: 8 },
  descriptionSection: { marginTop: 8, padding: 20 },
  sectionTitle: { fontSize: 16, fontWeight: "700", marginBottom: 12 },
  description: { fontSize: 14, lineHeight: 22 },
  bottomBar: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    padding: 16,
    borderTopWidth: 1,
    borderTopColor: "#E5E5E5",
  },
  quantityControl: { flexDirection: "row", alignItems: "center", gap: 4 },
  qtyText: { fontSize: 16, fontWeight: "600", width: 28, textAlign: "center" },
});
