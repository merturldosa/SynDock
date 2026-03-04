import { useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { EmptyState } from "../../src/components/ui/EmptyState";
import api from "../../src/lib/api";

interface QnA {
  id: number;
  productId: number;
  productName?: string;
  question: string;
  answer?: string;
  isAnswered: boolean;
  createdAt: string;
}

export default function QnAScreen() {
  const theme = useTheme();
  const [qnas, setQnas] = useState<QnA[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [expanded, setExpanded] = useState<number | null>(null);

  useEffect(() => {
    api
      .get("/qna/my")
      .then((r) => setQnas(r.data.items || r.data))
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

  if (qnas.length === 0) {
    return (
      <EmptyState
        icon="chatbubble-outline"
        title="문의 내역이 없습니다"
        description="상품 상세 페이지에서 Q&A를 작성할 수 있습니다"
      />
    );
  }

  return (
    <FlatList
      style={[styles.container, { backgroundColor: theme.background }]}
      data={qnas}
      keyExtractor={(item) => String(item.id)}
      renderItem={({ item }) => (
        <TouchableOpacity
          style={[styles.card, { backgroundColor: theme.surface }]}
          onPress={() => setExpanded(expanded === item.id ? null : item.id)}
        >
          <View style={styles.cardHeader}>
            <View
              style={[
                styles.statusDot,
                {
                  backgroundColor: item.isAnswered
                    ? "#10B981"
                    : theme.warning,
                },
              ]}
            />
            <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
              {item.isAnswered ? "답변완료" : "답변대기"}
            </Text>
            <View style={{ flex: 1 }} />
            <Text style={{ color: theme.textSecondary, fontSize: 12 }}>
              {formatDate(item.createdAt)}
            </Text>
          </View>
          {item.productName && (
            <Text
              style={[styles.productName, { color: theme.primary }]}
              numberOfLines={1}
            >
              {item.productName}
            </Text>
          )}
          <Text
            style={[styles.question, { color: theme.text }]}
            numberOfLines={expanded === item.id ? undefined : 2}
          >
            Q. {item.question}
          </Text>
          {expanded === item.id && item.answer && (
            <View
              style={[styles.answerBox, { backgroundColor: theme.background }]}
            >
              <Text style={[styles.answer, { color: theme.textSecondary }]}>
                A. {item.answer}
              </Text>
            </View>
          )}
          <Ionicons
            name={expanded === item.id ? "chevron-up" : "chevron-down"}
            size={16}
            color={theme.textSecondary}
            style={styles.chevron}
          />
        </TouchableOpacity>
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
  cardHeader: {
    flexDirection: "row",
    alignItems: "center",
    gap: 6,
    marginBottom: 8,
  },
  statusDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  productName: { fontSize: 13, fontWeight: "500", marginBottom: 4 },
  question: { fontSize: 14, lineHeight: 20 },
  answerBox: {
    marginTop: 12,
    padding: 12,
    borderRadius: 8,
  },
  answer: { fontSize: 14, lineHeight: 20 },
  chevron: { alignSelf: "center", marginTop: 8 },
});
