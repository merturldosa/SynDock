import { View, Text, StyleSheet } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { Button } from "../../src/components/ui/Button";

export default function OrderCompleteScreen() {
  const theme = useTheme();
  const router = useRouter();
  const { orderId, orderNumber } = useLocalSearchParams<{
    orderId: string;
    orderNumber: string;
  }>();

  return (
    <View
      style={[styles.container, { backgroundColor: theme.background }]}
    >
      <View style={styles.content}>
        <View
          style={[styles.iconCircle, { backgroundColor: theme.primary + "20" }]}
        >
          <Ionicons name="checkmark-circle" size={80} color={theme.primary} />
        </View>

        <Text style={[styles.title, { color: theme.text }]}>
          주문이 완료되었습니다
        </Text>
        <Text style={[styles.subtitle, { color: theme.textSecondary }]}>
          주문번호: {orderNumber || orderId}
        </Text>
        <Text style={[styles.description, { color: theme.textSecondary }]}>
          주문 내역은 마이페이지 &gt; 주문 내역에서 확인할 수 있습니다.
        </Text>
      </View>

      <View style={styles.buttons}>
        <Button
          title="주문 내역 보기"
          onPress={() => router.replace("/mypage/orders" as never)}
          size="lg"
          style={{ flex: 1 }}
        />
        <Button
          title="쇼핑 계속하기"
          onPress={() => router.replace("/(tabs)/" as never)}
          variant="outline"
          size="lg"
          style={{ flex: 1 }}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "space-between",
    padding: 24,
  },
  content: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
  },
  iconCircle: {
    width: 120,
    height: 120,
    borderRadius: 60,
    alignItems: "center",
    justifyContent: "center",
    marginBottom: 24,
  },
  title: {
    fontSize: 22,
    fontWeight: "700",
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 15,
    fontWeight: "500",
    marginBottom: 16,
  },
  description: {
    fontSize: 14,
    textAlign: "center",
    lineHeight: 20,
  },
  buttons: {
    flexDirection: "row",
    gap: 12,
  },
});
