import { useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  ActivityIndicator,
  TouchableOpacity,
  Linking,
} from "react-native";
import { useLocalSearchParams, router } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { getOrderById, getDeliveryTracking } from "../../src/lib/api";
import { useTenantStore } from "../../src/stores/tenantStore";
import type { Order, DeliveryTracking } from "../../src/types";

const STATUS_MAP: Record<string, { label: string; color: string }> = {
  Pending: { label: "주문 접수", color: "#F59E0B" },
  Confirmed: { label: "주문 확인", color: "#3B82F6" },
  Processing: { label: "처리 중", color: "#8B5CF6" },
  Shipped: { label: "배송 중", color: "#6366F1" },
  Delivered: { label: "배송 완료", color: "#10B981" },
  Cancelled: { label: "취소", color: "#EF4444" },
  Refunded: { label: "환불", color: "#6B7280" },
};

const DELIVERY_STATUS_STEPS = [
  { key: "Accepted", label: "배정 완료", icon: "person-outline" as const },
  { key: "PickedUp", label: "상품 수령", icon: "cube-outline" as const },
  { key: "InTransit", label: "배송 중", icon: "bicycle-outline" as const },
  { key: "Delivered", label: "배송 완료", icon: "checkmark-circle-outline" as const },
];

export default function OrderDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const theme = useTenantStore((s) => s.theme);
  const [order, setOrder] = useState<Order | null>(null);
  const [tracking, setTracking] = useState<DeliveryTracking | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadData();
  }, [id]);

  const loadData = async () => {
    try {
      const orderRes = await getOrderById(Number(id));
      setOrder(orderRes.data);

      try {
        const trackingRes = await getDeliveryTracking(Number(id));
        setTracking(trackingRes.data);
      } catch {
        // No delivery assignment - normal for standard shipping
      }
    } catch (error) {
      console.log("Failed to load order:", error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  if (!order) {
    return (
      <View style={styles.center}>
        <Text style={styles.errorText}>주문을 찾을 수 없습니다.</Text>
      </View>
    );
  }

  const statusInfo = STATUS_MAP[order.status] || { label: order.status, color: "#6B7280" };

  const getStepStatus = (stepKey: string) => {
    if (!tracking) return "pending";
    const statusOrder = ["Pending", "Offered", "Accepted", "PickedUp", "InTransit", "Delivered"];
    const currentIdx = statusOrder.indexOf(tracking.status);
    const stepIdx = statusOrder.indexOf(stepKey);
    if (stepIdx <= currentIdx) return "completed";
    if (stepIdx === currentIdx + 1) return "active";
    return "pending";
  };

  return (
    <ScrollView style={styles.container}>
      {/* Order Header */}
      <View style={styles.section}>
        <Text style={styles.orderNumber}>{order.orderNumber}</Text>
        <View style={[styles.statusBadge, { backgroundColor: statusInfo.color + "20" }]}>
          <Text style={[styles.statusText, { color: statusInfo.color }]}>{statusInfo.label}</Text>
        </View>
        <Text style={styles.dateText}>
          {new Date(order.createdAt).toLocaleDateString("ko-KR")}
        </Text>
      </View>

      {/* Delivery Tracking Timeline */}
      {tracking && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>배송 추적</Text>

          {tracking.deliveryOptionName && (
            <View style={styles.deliveryType}>
              <Ionicons name="flash-outline" size={16} color={theme.primary} />
              <Text style={[styles.deliveryTypeText, { color: theme.primary }]}>
                {tracking.deliveryOptionName}
              </Text>
            </View>
          )}

          {tracking.estimatedDeliveryAt && (
            <Text style={styles.etaText}>
              예상 도착: {new Date(tracking.estimatedDeliveryAt).toLocaleTimeString("ko-KR", { hour: "2-digit", minute: "2-digit" })}
            </Text>
          )}

          <View style={styles.timeline}>
            {DELIVERY_STATUS_STEPS.map((step, idx) => {
              const status = getStepStatus(step.key);
              const isLast = idx === DELIVERY_STATUS_STEPS.length - 1;
              return (
                <View key={step.key} style={styles.timelineItem}>
                  <View style={styles.timelineLeft}>
                    <View
                      style={[
                        styles.timelineDot,
                        status === "completed" && { backgroundColor: theme.primary },
                        status === "active" && { backgroundColor: theme.primary, borderWidth: 3, borderColor: theme.primaryLight },
                      ]}
                    >
                      {status === "completed" && (
                        <Ionicons name="checkmark" size={12} color="#fff" />
                      )}
                    </View>
                    {!isLast && (
                      <View
                        style={[
                          styles.timelineLine,
                          status === "completed" && { backgroundColor: theme.primary },
                        ]}
                      />
                    )}
                  </View>
                  <View style={styles.timelineContent}>
                    <Text style={[styles.timelineLabel, status !== "pending" && { color: "#111" }]}>
                      {step.label}
                    </Text>
                  </View>
                </View>
              );
            })}
          </View>

          {/* Driver Info */}
          {tracking.driverName && (
            <View style={styles.driverCard}>
              <View style={styles.driverInfo}>
                <Ionicons name="person-circle-outline" size={40} color={theme.secondary} />
                <View style={{ marginLeft: 12 }}>
                  <Text style={styles.driverName}>{tracking.driverName}</Text>
                  <Text style={styles.driverVehicle}>
                    {tracking.vehicleType} {tracking.licensePlate && `(${tracking.licensePlate})`}
                  </Text>
                </View>
              </View>
              {tracking.driverPhone && (
                <TouchableOpacity
                  style={[styles.callButton, { backgroundColor: theme.primary }]}
                  onPress={() => Linking.openURL(`tel:${tracking.driverPhone}`)}
                >
                  <Ionicons name="call-outline" size={20} color="#fff" />
                </TouchableOpacity>
              )}
            </View>
          )}

          {/* Live Tracking Button */}
          {tracking.status !== "Delivered" && tracking.status !== "Cancelled" && tracking.driverName && (
            <TouchableOpacity
              style={[styles.trackButton, { backgroundColor: theme.primary }]}
              onPress={() => router.push(`/delivery-tracking?assignmentId=${tracking.assignmentId}`)}
            >
              <Ionicons name="location-outline" size={20} color="#fff" />
              <Text style={styles.trackButtonText}>실시간 배송 추적</Text>
            </TouchableOpacity>
          )}
        </View>
      )}

      {/* Order Items */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>주문 상품</Text>
        {order.items.map((item) => (
          <View key={item.id} style={styles.itemRow}>
            <View style={{ flex: 1 }}>
              <Text style={styles.itemName}>{item.productName}</Text>
              <Text style={styles.itemQty}>수량: {item.quantity}</Text>
            </View>
            <Text style={styles.itemPrice}>{(item.price * item.quantity).toLocaleString()}원</Text>
          </View>
        ))}
      </View>

      {/* Payment Summary */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>결제 정보</Text>
        <View style={styles.summaryRow}>
          <Text style={styles.summaryLabel}>배송비</Text>
          <Text style={styles.summaryValue}>{order.shippingFee?.toLocaleString() ?? 0}원</Text>
        </View>
        {order.discountAmount > 0 && (
          <View style={styles.summaryRow}>
            <Text style={styles.summaryLabel}>할인</Text>
            <Text style={[styles.summaryValue, { color: "#EF4444" }]}>
              -{order.discountAmount.toLocaleString()}원
            </Text>
          </View>
        )}
        <View style={[styles.summaryRow, styles.totalRow]}>
          <Text style={styles.totalLabel}>총 결제금액</Text>
          <Text style={[styles.totalValue, { color: theme.primary }]}>
            {order.totalAmount.toLocaleString()}원
          </Text>
        </View>
      </View>

      <View style={{ height: 40 }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#F5F5F5" },
  center: { flex: 1, justifyContent: "center", alignItems: "center" },
  errorText: { fontSize: 16, color: "#666" },
  section: { backgroundColor: "#fff", marginBottom: 8, padding: 16 },
  orderNumber: { fontSize: 18, fontWeight: "700", color: "#111" },
  statusBadge: { alignSelf: "flex-start", paddingHorizontal: 12, paddingVertical: 4, borderRadius: 12, marginTop: 8 },
  statusText: { fontSize: 13, fontWeight: "600" },
  dateText: { marginTop: 4, fontSize: 13, color: "#999" },
  sectionTitle: { fontSize: 16, fontWeight: "700", color: "#111", marginBottom: 12 },
  deliveryType: { flexDirection: "row", alignItems: "center", marginBottom: 4 },
  deliveryTypeText: { marginLeft: 4, fontSize: 14, fontWeight: "600" },
  etaText: { fontSize: 13, color: "#666", marginBottom: 12 },
  timeline: { marginTop: 8 },
  timelineItem: { flexDirection: "row", minHeight: 48 },
  timelineLeft: { width: 30, alignItems: "center" },
  timelineDot: { width: 24, height: 24, borderRadius: 12, backgroundColor: "#D1D5DB", justifyContent: "center", alignItems: "center" },
  timelineLine: { flex: 1, width: 2, backgroundColor: "#D1D5DB" },
  timelineContent: { flex: 1, paddingLeft: 12, paddingBottom: 16 },
  timelineLabel: { fontSize: 14, color: "#9CA3AF" },
  driverCard: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", backgroundColor: "#F9FAFB", borderRadius: 12, padding: 12, marginTop: 12 },
  driverInfo: { flexDirection: "row", alignItems: "center" },
  driverName: { fontSize: 15, fontWeight: "600", color: "#111" },
  driverVehicle: { fontSize: 13, color: "#666", marginTop: 2 },
  callButton: { width: 44, height: 44, borderRadius: 22, justifyContent: "center", alignItems: "center" },
  trackButton: { flexDirection: "row", alignItems: "center", justifyContent: "center", padding: 14, borderRadius: 12, marginTop: 12 },
  trackButtonText: { color: "#fff", fontSize: 15, fontWeight: "600", marginLeft: 8 },
  itemRow: { flexDirection: "row", alignItems: "center", paddingVertical: 10, borderBottomWidth: 1, borderBottomColor: "#F3F4F6" },
  itemName: { fontSize: 14, fontWeight: "500", color: "#111" },
  itemQty: { fontSize: 13, color: "#666", marginTop: 2 },
  itemPrice: { fontSize: 14, fontWeight: "600", color: "#111" },
  summaryRow: { flexDirection: "row", justifyContent: "space-between", paddingVertical: 6 },
  summaryLabel: { fontSize: 14, color: "#666" },
  summaryValue: { fontSize: 14, color: "#111" },
  totalRow: { borderTopWidth: 1, borderTopColor: "#E5E7EB", marginTop: 8, paddingTop: 12 },
  totalLabel: { fontSize: 16, fontWeight: "700", color: "#111" },
  totalValue: { fontSize: 18, fontWeight: "700" },
});
