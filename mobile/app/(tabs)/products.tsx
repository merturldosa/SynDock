import { useEffect, useState, useCallback } from "react";
import {
  View,
  Text,
  FlatList,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { ProductCard } from "../../src/components/ui/ProductCard";
import { EmptyState } from "../../src/components/ui/EmptyState";
import { getProducts, getCategories } from "../../src/lib/api";
import type { Product, Category } from "../../src/types";

export default function ProductsScreen() {
  const theme = useTheme();
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [search, setSearch] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [hasNext, setHasNext] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  const fetchProducts = useCallback(
    async (p = 1, append = false) => {
      setIsLoading(true);
      try {
        const params: Record<string, unknown> = { page: p, pageSize: 12 };
        if (search) params.search = search;
        if (selectedCategory) params.category = selectedCategory;

        const res = await getProducts(params);
        const items = res.data.items || [];
        setProducts(append ? (prev) => [...prev, ...items] : items);
        setHasNext(res.data.hasNext);
        setPage(p);
      } catch {}
      setIsLoading(false);
    },
    [search, selectedCategory]
  );

  useEffect(() => {
    fetchProducts(1);
  }, [fetchProducts]);

  useEffect(() => {
    getCategories()
      .then((res) => setCategories(res.data))
      .catch(() => {});
  }, []);

  const loadMore = () => {
    if (hasNext && !isLoading) fetchProducts(page + 1, true);
  };

  return (
    <View style={[styles.container, { backgroundColor: theme.background }]}>
      {/* Search */}
      <View style={[styles.searchBar, { backgroundColor: theme.surface }]}>
        <Ionicons name="search" size={20} color={theme.textSecondary} />
        <TextInput
          style={[styles.searchInput, { color: theme.text }]}
          placeholder="상품 검색..."
          placeholderTextColor={theme.textSecondary}
          value={search}
          onChangeText={setSearch}
          onSubmitEditing={() => fetchProducts(1)}
          returnKeyType="search"
        />
        {search.length > 0 && (
          <TouchableOpacity onPress={() => setSearch("")}>
            <Ionicons name="close-circle" size={20} color={theme.textSecondary} />
          </TouchableOpacity>
        )}
      </View>

      {/* Category Chips */}
      <FlatList
        horizontal
        showsHorizontalScrollIndicator={false}
        data={[{ id: 0, name: "전체", slug: "" } as Category, ...categories]}
        keyExtractor={(item) => item.id.toString()}
        contentContainerStyle={{ gap: 8, paddingHorizontal: 16, paddingVertical: 8 }}
        renderItem={({ item }) => {
          const active = item.slug === (selectedCategory ?? "");
          return (
            <TouchableOpacity
              style={[
                styles.chip,
                {
                  backgroundColor: active ? theme.secondary : theme.surface,
                  borderColor: active ? theme.secondary : theme.border,
                },
              ]}
              onPress={() =>
                setSelectedCategory(item.slug || null)
              }
            >
              <Text
                style={{
                  color: active ? "#FFF" : theme.text,
                  fontSize: 13,
                  fontWeight: active ? "600" : "400",
                }}
              >
                {item.name}
              </Text>
            </TouchableOpacity>
          );
        }}
      />

      {/* Product Grid */}
      <FlatList
        data={products}
        numColumns={2}
        keyExtractor={(item) => item.id.toString()}
        contentContainerStyle={{ padding: 12, gap: 8 }}
        columnWrapperStyle={{ gap: 8 }}
        renderItem={({ item }) => (
          <View style={{ flex: 1 }}>
            <ProductCard product={item} />
          </View>
        )}
        onEndReached={loadMore}
        onEndReachedThreshold={0.3}
        ListFooterComponent={
          isLoading ? (
            <ActivityIndicator
              size="small"
              color={theme.primary}
              style={{ padding: 16 }}
            />
          ) : null
        }
        ListEmptyComponent={
          !isLoading ? (
            <EmptyState
              icon="bag-outline"
              title="상품이 없습니다"
              description="다른 카테고리를 선택하거나 검색어를 변경해보세요"
            />
          ) : null
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  searchBar: {
    flexDirection: "row",
    alignItems: "center",
    margin: 16,
    paddingHorizontal: 12,
    height: 44,
    borderRadius: 22,
    gap: 8,
  },
  searchInput: { flex: 1, fontSize: 15 },
  chip: {
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
  },
});
