import { useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  RefreshControl,
} from "react-native";
import { Image } from "expo-image";
import { useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { useTenantStore } from "../../src/stores/tenantStore";
import { ProductCard } from "../../src/components/ui/ProductCard";
import { getCategories, getProducts, getPopular } from "../../src/lib/api";
import type { Category, Product } from "../../src/types";

export default function HomeScreen() {
  const theme = useTheme();
  const tenantConfig = useTenantStore((s) => s.config);
  const router = useRouter();
  const [categories, setCategories] = useState<Category[]>([]);
  const [featured, setFeatured] = useState<Product[]>([]);
  const [popular, setPopular] = useState<Product[]>([]);
  const [refreshing, setRefreshing] = useState(false);

  const load = async () => {
    try {
      const [catRes, featRes, popRes] = await Promise.allSettled([
        getCategories(),
        getProducts({ isFeatured: true, pageSize: 6 }),
        getPopular(),
      ]);
      if (catRes.status === "fulfilled") setCategories(catRes.value.data);
      if (featRes.status === "fulfilled")
        setFeatured(featRes.value.data.items || []);
      if (popRes.status === "fulfilled") setPopular(popRes.value.data);
    } catch {}
  };

  useEffect(() => {
    load();
  }, []);

  const onRefresh = async () => {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  };

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: theme.background }]}
      refreshControl={
        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
      }
    >
      {/* Header */}
      <View
        style={[styles.header, { backgroundColor: theme.surface }]}
      >
        <Text style={[styles.logo, { color: theme.secondary }]}>
          {tenantConfig?.name || "SynDock"}
        </Text>
        <View style={styles.headerIcons}>
          <TouchableOpacity
            onPress={() => router.push("/product/search" as never)}
          >
            <Ionicons name="search-outline" size={24} color={theme.text} />
          </TouchableOpacity>
          <TouchableOpacity>
            <Ionicons
              name="notifications-outline"
              size={24}
              color={theme.text}
            />
          </TouchableOpacity>
        </View>
      </View>

      {/* Categories */}
      <View style={styles.section}>
        <Text style={[styles.sectionTitle, { color: theme.text }]}>
          카테고리
        </Text>
        <FlatList
          horizontal
          showsHorizontalScrollIndicator={false}
          data={categories}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={{ gap: 16, paddingHorizontal: 16 }}
          renderItem={({ item }) => (
            <TouchableOpacity
              style={styles.categoryItem}
              onPress={() =>
                router.push(`/products?category=${item.slug}` as never)
              }
            >
              <View
                style={[
                  styles.categoryIcon,
                  { backgroundColor: theme.primaryLight },
                ]}
              >
                <Ionicons
                  name="folder-outline"
                  size={24}
                  color={theme.secondary}
                />
              </View>
              <Text
                style={[styles.categoryName, { color: theme.text }]}
                numberOfLines={1}
              >
                {item.name}
              </Text>
            </TouchableOpacity>
          )}
        />
      </View>

      {/* Featured Products */}
      {featured.length > 0 && (
        <View style={styles.section}>
          <View style={styles.sectionHeader}>
            <Text style={[styles.sectionTitle, { color: theme.text }]}>
              추천 상품
            </Text>
            <TouchableOpacity
              onPress={() =>
                router.push("/products?isFeatured=true" as never)
              }
            >
              <Text style={{ color: theme.primary }}>더보기</Text>
            </TouchableOpacity>
          </View>
          <FlatList
            horizontal
            showsHorizontalScrollIndicator={false}
            data={featured}
            keyExtractor={(item) => item.id.toString()}
            contentContainerStyle={{ gap: 12, paddingHorizontal: 16 }}
            renderItem={({ item }) => (
              <View style={{ width: 160 }}>
                <ProductCard product={item} />
              </View>
            )}
          />
        </View>
      )}

      {/* Popular Products */}
      {popular.length > 0 && (
        <View style={styles.section}>
          <Text style={[styles.sectionTitle, { color: theme.text }]}>
            인기 상품
          </Text>
          <View style={styles.productGrid}>
            {popular.slice(0, 4).map((product) => (
              <View key={product.id} style={styles.gridItem}>
                <ProductCard product={product} />
              </View>
            ))}
          </View>
        </View>
      )}

      <View style={{ height: 20 }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingTop: 50,
    paddingBottom: 12,
  },
  logo: { fontSize: 22, fontWeight: "700" },
  headerIcons: { flexDirection: "row", gap: 16 },
  section: { marginTop: 24 },
  sectionHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingHorizontal: 16,
    marginBottom: 12,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: "700",
    paddingHorizontal: 16,
    marginBottom: 12,
  },
  categoryItem: { alignItems: "center", width: 72 },
  categoryIcon: {
    width: 56,
    height: 56,
    borderRadius: 28,
    alignItems: "center",
    justifyContent: "center",
  },
  categoryName: { fontSize: 12, marginTop: 6, textAlign: "center" },
  productGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    paddingHorizontal: 12,
    gap: 8,
  },
  gridItem: { width: "48%" },
});
