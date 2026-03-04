import { useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  ActivityIndicator,
  Alert,
} from "react-native";
import { useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { Button } from "../../src/components/ui/Button";
import { useCartStore } from "../../src/stores/cartStore";
import {
  getAddresses,
  createOrder,
  getPointBalance,
  getMyCoupons,
} from "../../src/lib/api";
import { formatPrice } from "../../src/lib/theme";
import type { Address } from "../../src/types";

export default function OrderScreen() {
  const theme = useTheme();
  const router = useRouter();
  const { cart, clearCart } = useCartStore();
  const [addresses, setAddresses] = useState<Address[]>([]);
  const [selectedAddressId, setSelectedAddressId] = useState<number | null>(
    null
  );
  const [points, setPoints] = useState(0);
  const [usePoints, setUsePoints] = useState(0);
  const [note, setNote] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isOrdering, setIsOrdering] = useState(false);

  useEffect(() => {
    Promise.all([
      getAddresses().then((r) => {
        setAddresses(r.data);
        const def = r.data.find((a: Address) => a.isDefault);
        if (def) setSelectedAddressId(def.id);
        else if (r.data.length > 0) setSelectedAddressId(r.data[0].id);
      }),
      getPointBalance()
        .then((r) => setPoints(r.data.balance))
        .catch(() => {}),
    ]).finally(() => setIsLoading(false));
  }, []);

  const selectedAddress = addresses.find((a) => a.id === selectedAddressId);
  const itemTotal = cart?.totalAmount || 0;
  const shipping = itemTotal >= 50000 ? 0 : 3000;
  const pointDiscount = Math.min(usePoints, itemTotal);
  const finalTotal = itemTotal + shipping - pointDiscount;

  const handleOrder = async () => {
    if (!selectedAddressId) {
      Alert.alert("배송지 선택", "배송지를 선택해주세요");
      return;
    }
    setIsOrdering(true);
    try {
      const res = await createOrder({
        shippingAddressId: selectedAddressId,
        note: note || undefined,
        pointsToUse: usePoints > 0 ? usePoints : undefined,
      });
      await clearCart();
      router.replace({
        pathname: "/order/complete",
        params: { orderId: res.data.id, orderNumber: res.data.orderNumber },
      } as never);
    } catch (err: any) {
      Alert.alert(
        "주문 실패",
        err.response?.data?.error || "다시 시도해주세요"
      );
    }
    setIsOrdering(false);
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  return (
    <View style={[styles.container, { backgroundColor: theme.background }]}>
      <ScrollView>
        {/* Shipping Address */}
        <View style={[styles.section, { backgroundColor: theme.surface }]}>
          <Text style={[styles.sectionTitle, { color: theme.text }]}>
            <Ionicons name="location-outline" size={18} /> 배송지
          </Text>
          {selectedAddress ? (
            <View style={styles.addressCard}>
              <Text style={[styles.addressName, { color: theme.text }]}>
                {selectedAddress.recipientName}
                {selectedAddress.isDefault && (
                  <Text style={{ color: theme.primary }}> (기본)</Text>
                )}
              </Text>
              <Text style={{ color: theme.textSecondary, marginTop: 4 }}>
                {selectedAddress.phone}
              </Text>
              <Text style={{ color: theme.textSecondary, marginTop: 2 }}>
                [{selectedAddress.zipCode}] {selectedAddress.address1}
                {selectedAddress.address2 && ` ${selectedAddress.address2}`}
              </Text>
            </View>
          ) : (
            <Text style={{ color: theme.textSecondary, marginTop: 8 }}>
              등록된 배송지가 없습니다
            </Text>
          )}
          {addresses.length > 1 && (
            <Button
              title="배송지 변경"
              variant="outline"
              size="sm"
              onPress={() => {
                const idx = addresses.findIndex(
                  (a) => a.id === selectedAddressId
                );
                const next = addresses[(idx + 1) % addresses.length];
                setSelectedAddressId(next.id);
              }}
              style={{ marginTop: 8, alignSelf: "flex-start" }}
            />
          )}
        </View>

        {/* Order Items */}
        <View style={[styles.section, { backgroundColor: theme.surface }]}>
          <Text style={[styles.sectionTitle, { color: theme.text }]}>
            <Ionicons name="bag-outline" size={18} /> 주문 상품 (
            {cart?.totalQuantity || 0}개)
          </Text>
          {cart?.items.map((item) => (
            <View key={item.id} style={styles.orderItem}>
              <View style={{ flex: 1 }}>
                <Text style={[styles.itemName, { color: theme.text }]}>
                  {item.productName}
                </Text>
                {item.variantName && (
                  <Text style={{ color: theme.textSecondary, fontSize: 13 }}>
                    옵션: {item.variantName}
                  </Text>
                )}
                <Text style={{ color: theme.textSecondary, fontSize: 13 }}>
                  수량: {item.quantity}개
                </Text>
              </View>
              <Text style={[styles.itemPrice, { color: theme.text }]}>
                {formatPrice((item.salePrice || item.price) * item.quantity)}
              </Text>
            </View>
          ))}
        </View>

        {/* Points */}
        {points > 0 && (
          <View style={[styles.section, { backgroundColor: theme.surface }]}>
            <Text style={[styles.sectionTitle, { color: theme.text }]}>
              <Ionicons name="diamond-outline" size={18} /> 포인트
            </Text>
            <View style={styles.pointRow}>
              <Text style={{ color: theme.textSecondary }}>
                보유 포인트: {points.toLocaleString()}P
              </Text>
              <Button
                title={usePoints > 0 ? "취소" : "전액 사용"}
                variant="outline"
                size="sm"
                onPress={() => setUsePoints(usePoints > 0 ? 0 : points)}
              />
            </View>
            {usePoints > 0 && (
              <Text style={{ color: theme.primary, marginTop: 4 }}>
                -{usePoints.toLocaleString()}P 사용
              </Text>
            )}
          </View>
        )}

        {/* Payment Summary */}
        <View style={[styles.section, { backgroundColor: theme.surface }]}>
          <Text style={[styles.sectionTitle, { color: theme.text }]}>
            결제 정보
          </Text>
          <View style={styles.summaryRow}>
            <Text style={{ color: theme.textSecondary }}>상품 금액</Text>
            <Text style={{ color: theme.text }}>{formatPrice(itemTotal)}</Text>
          </View>
          <View style={styles.summaryRow}>
            <Text style={{ color: theme.textSecondary }}>배송비</Text>
            <Text style={{ color: shipping === 0 ? theme.primary : theme.text }}>
              {shipping === 0 ? "무료" : formatPrice(shipping)}
            </Text>
          </View>
          {pointDiscount > 0 && (
            <View style={styles.summaryRow}>
              <Text style={{ color: theme.textSecondary }}>포인트 할인</Text>
              <Text style={{ color: theme.error }}>
                -{formatPrice(pointDiscount)}
              </Text>
            </View>
          )}
          <View
            style={[
              styles.summaryRow,
              styles.totalRow,
              { borderTopColor: theme.border },
            ]}
          >
            <Text style={[styles.totalLabel, { color: theme.text }]}>
              총 결제금액
            </Text>
            <Text style={[styles.totalAmount, { color: theme.secondary }]}>
              {formatPrice(finalTotal)}
            </Text>
          </View>
        </View>
      </ScrollView>

      {/* Bottom */}
      <View style={[styles.bottomBar, { backgroundColor: theme.surface }]}>
        <Button
          title={`${formatPrice(finalTotal)} 결제하기`}
          onPress={handleOrder}
          isLoading={isOrdering}
          size="lg"
          style={{ flex: 1 }}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  section: { marginTop: 8, padding: 20 },
  sectionTitle: { fontSize: 16, fontWeight: "700" },
  addressCard: { marginTop: 12 },
  addressName: { fontSize: 15, fontWeight: "600" },
  orderItem: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: 12,
    borderBottomWidth: 0.5,
    borderBottomColor: "#F0F0F0",
  },
  itemName: { fontSize: 14, fontWeight: "500" },
  itemPrice: { fontSize: 15, fontWeight: "600" },
  pointRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginTop: 12,
  },
  summaryRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginTop: 10,
  },
  totalRow: { marginTop: 12, paddingTop: 12, borderTopWidth: 1 },
  totalLabel: { fontSize: 16, fontWeight: "700" },
  totalAmount: { fontSize: 20, fontWeight: "700" },
  bottomBar: {
    padding: 16,
    borderTopWidth: 1,
    borderTopColor: "#E5E5E5",
  },
});
