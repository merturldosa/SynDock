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
import { EmptyState } from "../../src/components/ui/EmptyState";
import api from "../../src/lib/api";

interface Review {
  id: number;
  productId: number;
  productName?: string;
  rating: number;
  content: string;
  createdAt: string;
}

export default function ReviewsScreen() {
  const theme = useTheme();
  const [reviews, setReviews] = useState<Review[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    api
      .get("/reviews/my")
      .then((r) => setReviews(r.data.items || r.data))
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

  if (reviews.length === 0) {
    return (
      <EmptyState
        icon="star-outline"
        title="작성한 리뷰가 없습니다"
        description="구매한 상품의 리뷰를 작성해보세요"
      />
    );
  }

  return (
    <FlatList
      style={[styles.container, { backgroundColor: theme.background }]}
      data={reviews}
      keyExtractor={(item) => String(item.id)}
      renderItem={({ item }) => (
        <View style={[styles.card, { backgroundColor: theme.surface }]}>
          {item.productName && (
            <Text
              style={[styles.productName, { color: theme.primary }]}
              numberOfLines={1}
            >
              {item.productName}
            </Text>
          )}
          <View style={styles.ratingRow}>
            {[1, 2, 3, 4, 5].map((star) => (
              <Ionicons
                key={star}
                name={star <= item.rating ? "star" : "star-outline"}
                size={16}
                color="#F59E0B"
              />
            ))}
            <Text style={{ color: theme.textSecondary, fontSize: 12, marginLeft: 8 }}>
              {formatDate(item.createdAt)}
            </Text>
          </View>
          <Text style={[styles.content, { color: theme.text }]}>
            {item.content}
          </Text>
        </View>
      )}
    />
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  card: {
    margin: 16,
    marginBottom: 0,
    padding: 16,
    borderRadius: 12,
  },
  productName: { fontSize: 13, fontWeight: "500", marginBottom: 6 },
  ratingRow: { flexDirection: "row", alignItems: "center" },
  content: { fontSize: 14, lineHeight: 20, marginTop: 8 },
});
