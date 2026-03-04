import { View, Text, StyleSheet, TouchableOpacity } from "react-native";
import { Image } from "expo-image";
import { useRouter } from "expo-router";
import { useTheme } from "../../hooks/useTheme";
import { formatPrice } from "../../lib/theme";
import type { Product } from "../../types";

interface ProductCardProps {
  product: Product;
  onAddToCart?: () => void;
}

export function ProductCard({ product, onAddToCart }: ProductCardProps) {
  const theme = useTheme();
  const router = useRouter();

  const discount =
    product.salePrice && product.price > 0
      ? Math.round(((product.price - product.salePrice) / product.price) * 100)
      : 0;

  return (
    <TouchableOpacity
      style={[styles.card, { backgroundColor: theme.surface }]}
      onPress={() => router.push(`/product/${product.id}` as never)}
      activeOpacity={0.7}
    >
      <View style={styles.imageWrapper}>
        <Image
          source={{ uri: product.imageUrl || "https://via.placeholder.com/200" }}
          style={styles.image}
          contentFit="cover"
        />
        {product.isFeatured && (
          <View
            style={[styles.badge, { backgroundColor: theme.primary }]}
          >
            <Text style={styles.badgeText}>Best</Text>
          </View>
        )}
        {product.isNew && (
          <View
            style={[
              styles.badge,
              { backgroundColor: theme.success, right: undefined, left: 8 },
            ]}
          >
            <Text style={styles.badgeText}>New</Text>
          </View>
        )}
        {discount > 0 && (
          <View
            style={[styles.discountBadge, { backgroundColor: theme.error }]}
          >
            <Text style={styles.badgeText}>-{discount}%</Text>
          </View>
        )}
      </View>

      <View style={styles.info}>
        {product.categoryName && (
          <Text
            style={[styles.category, { color: theme.primary }]}
            numberOfLines={1}
          >
            {product.categoryName}
          </Text>
        )}
        <Text style={[styles.name, { color: theme.text }]} numberOfLines={2}>
          {product.name}
        </Text>
        <View style={styles.priceRow}>
          <Text style={[styles.price, { color: theme.secondary }]}>
            {formatPrice(product.salePrice || product.price)}
          </Text>
          {product.salePrice && (
            <Text style={styles.originalPrice}>
              {formatPrice(product.price)}
            </Text>
          )}
        </View>
        {product.stock > 0 && product.stock < 5 && (
          <Text style={[styles.stockWarning, { color: theme.warning }]}>
            재고 {product.stock}개 남음
          </Text>
        )}
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 12,
    overflow: "hidden",
    elevation: 2,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
  },
  imageWrapper: {
    aspectRatio: 1,
    position: "relative",
  },
  image: {
    width: "100%",
    height: "100%",
  },
  badge: {
    position: "absolute",
    top: 8,
    right: 8,
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
  },
  discountBadge: {
    position: "absolute",
    bottom: 8,
    right: 8,
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
  },
  badgeText: {
    color: "#FFFFFF",
    fontSize: 11,
    fontWeight: "700",
  },
  info: {
    padding: 10,
  },
  category: {
    fontSize: 11,
    fontWeight: "500",
    marginBottom: 4,
  },
  name: {
    fontSize: 14,
    fontWeight: "500",
    lineHeight: 20,
    marginBottom: 6,
  },
  priceRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 6,
  },
  price: {
    fontSize: 16,
    fontWeight: "700",
  },
  originalPrice: {
    fontSize: 13,
    color: "#999",
    textDecorationLine: "line-through",
  },
  stockWarning: {
    fontSize: 11,
    marginTop: 4,
    fontWeight: "500",
  },
});
