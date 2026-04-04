import { useEffect, useState, useRef } from "react";
import {
  View,
  Text,
  StyleSheet,
  ActivityIndicator,
  TouchableOpacity,
  Linking,
} from "react-native";
import { useLocalSearchParams } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTenantStore } from "../src/stores/tenantStore";
import api from "../src/lib/api";
import type { DeliveryTracking } from "../src/types";

const STATUS_LABELS: Record<string, string> = {
  Pending: "배정 대기",
  Offered: "기사 배정 중",
  Accepted: "기사 배정 완료",
  PickedUp: "상품 수령",
  InTransit: "배송 중",
  Delivered: "배송 완료",
  Cancelled: "취소됨",
};

export default function DeliveryTrackingScreen() {
  const { assignmentId } = useLocalSearchParams<{ assignmentId: string }>();
  const theme = useTenantStore((s) => s.theme);
  const [tracking, setTracking] = useState<DeliveryTracking | null>(null);
  const [loading, setLoading] = useState(true);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    fetchTracking();

    // Poll every 5 seconds for updates
    intervalRef.current = setInterval(fetchTracking, 5000);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [assignmentId]);

  const fetchTracking = async () => {
    try {
      const res = await api.get<DeliveryTracking>(
        `/delivery/assignments/${assignmentId}/tracking`
      );
      setTracking(res.data);

      // Stop polling if delivered or cancelled
      if (res.data.status === "Delivered" || res.data.status === "Cancelled") {
        if (intervalRef.current) clearInterval(intervalRef.current);
      }
    } catch (error) {
      console.log("Tracking fetch failed:", error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
        <Text style={styles.loadingText}>배송 정보를 불러오는 중...</Text>
      </View>
    );
  }

  if (!tracking) {
    return (
      <View style={styles.center}>
        <Ionicons name="location-outline" size={48} color="#ccc" />
        <Text style={styles.errorText}>배송 추적 정보가 없습니다.</Text>
      </View>
    );
  }

  const statusLabel = STATUS_LABELS[tracking.status] || tracking.status;
  const isActive = !["Delivered", "Cancelled"].includes(tracking.status);

  return (
    <View style={styles.container}>
      {/* Status Header */}
      <View style={[styles.statusHeader, { backgroundColor: theme.primary }]}>
        <View style={styles.statusIcon}>
          {isActive ? (
            <Ionicons name="bicycle" size={32} color="#fff" />
          ) : tracking.status === "Delivered" ? (
            <Ionicons name="checkmark-circle" size={32} color="#fff" />
          ) : (
            <Ionicons name="close-circle" size={32} color="#fff" />
          )}
        </View>
        <Text style={styles.statusLabel}>{statusLabel}</Text>
        {tracking.estimatedDeliveryAt && isActive && (
          <Text style={styles.etaText}>
            예상 도착:{" "}
            {new Date(tracking.estimatedDeliveryAt).toLocaleTimeString("ko-KR", {
              hour: "2-digit",
              minute: "2-digit",
            })}
          </Text>
        )}
        {tracking.deliveryOptionName && (
          <View style={styles.optionBadge}>
            <Ionicons name="flash" size={14} color={theme.primary} />
            <Text style={[styles.optionText, { color: theme.primary }]}>
              {tracking.deliveryOptionName}
            </Text>
          </View>
        )}
      </View>

      {/* Map Placeholder */}
      <View style={styles.mapArea}>
        {tracking.driverLatitude && tracking.driverLongitude ? (
          <View style={styles.mapContent}>
            <Ionicons name="navigate-circle" size={48} color={theme.primary} />
            <Text style={styles.coordText}>
              {tracking.driverLatitude.toFixed(4)}, {tracking.driverLongitude.toFixed(4)}
            </Text>
            <Text style={styles.mapNote}>실시간 위치 업데이트 중</Text>
          </View>
        ) : (
          <View style={styles.mapContent}>
            <Ionicons name="location-outline" size={48} color="#ccc" />
            <Text style={styles.mapNote}>위치 정보 대기 중</Text>
          </View>
        )}
      </View>

      {/* Driver Card */}
      {tracking.driverName && (
        <View style={styles.driverCard}>
          <Ionicons name="person-circle-outline" size={48} color={theme.secondary} />
          <View style={styles.driverDetails}>
            <Text style={styles.driverName}>{tracking.driverName}</Text>
            <Text style={styles.driverMeta}>
              {tracking.vehicleType}
              {tracking.licensePlate ? ` | ${tracking.licensePlate}` : ""}
            </Text>
          </View>
          {tracking.driverPhone && (
            <TouchableOpacity
              style={[styles.callBtn, { backgroundColor: theme.primary }]}
              onPress={() => Linking.openURL(`tel:${tracking.driverPhone}`)}
            >
              <Ionicons name="call" size={22} color="#fff" />
            </TouchableOpacity>
          )}
        </View>
      )}

      {/* Timeline */}
      <View style={styles.timelineSection}>
        {tracking.acceptedAt && (
          <TimelineEntry
            icon="person-add-outline"
            label="배송기사 배정"
            time={tracking.acceptedAt}
            color={theme.primary}
          />
        )}
        {tracking.pickedUpAt && (
          <TimelineEntry
            icon="cube-outline"
            label="상품 수령"
            time={tracking.pickedUpAt}
            color={theme.primary}
          />
        )}
        {tracking.inTransitAt && (
          <TimelineEntry
            icon="bicycle-outline"
            label="배송 출발"
            time={tracking.inTransitAt}
            color={theme.primary}
          />
        )}
        {tracking.deliveredAt && (
          <TimelineEntry
            icon="checkmark-circle-outline"
            label="배송 완료"
            time={tracking.deliveredAt}
            color="#10B981"
          />
        )}
      </View>

      {/* Delivery Proof */}
      {tracking.deliveryPhotoUrl && (
        <View style={styles.proofSection}>
          <Text style={styles.proofTitle}>배송 완료 사진</Text>
          <Text style={styles.proofNote}>{tracking.deliveryNote}</Text>
        </View>
      )}
    </View>
  );
}

function TimelineEntry({
  icon,
  label,
  time,
  color,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  label: string;
  time: string;
  color: string;
}) {
  return (
    <View style={styles.timelineRow}>
      <Ionicons name={icon} size={20} color={color} />
      <Text style={styles.timelineLabel}>{label}</Text>
      <Text style={styles.timelineTime}>
        {new Date(time).toLocaleTimeString("ko-KR", { hour: "2-digit", minute: "2-digit" })}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: "#F5F5F5" },
  center: { flex: 1, justifyContent: "center", alignItems: "center" },
  loadingText: { marginTop: 12, fontSize: 14, color: "#666" },
  errorText: { marginTop: 12, fontSize: 16, color: "#999" },
  statusHeader: { padding: 24, alignItems: "center" },
  statusIcon: { marginBottom: 8 },
  statusLabel: { fontSize: 22, fontWeight: "700", color: "#fff" },
  etaText: { fontSize: 14, color: "rgba(255,255,255,0.85)", marginTop: 4 },
  optionBadge: { flexDirection: "row", alignItems: "center", backgroundColor: "#fff", paddingHorizontal: 12, paddingVertical: 4, borderRadius: 12, marginTop: 8 },
  optionText: { fontSize: 13, fontWeight: "600", marginLeft: 4 },
  mapArea: { height: 200, backgroundColor: "#E5E7EB", justifyContent: "center", alignItems: "center" },
  mapContent: { alignItems: "center" },
  coordText: { marginTop: 8, fontSize: 14, color: "#374151", fontFamily: "monospace" },
  mapNote: { marginTop: 4, fontSize: 12, color: "#9CA3AF" },
  driverCard: { flexDirection: "row", alignItems: "center", backgroundColor: "#fff", margin: 16, padding: 16, borderRadius: 16, elevation: 2 },
  driverDetails: { flex: 1, marginLeft: 12 },
  driverName: { fontSize: 16, fontWeight: "700", color: "#111" },
  driverMeta: { fontSize: 13, color: "#666", marginTop: 2 },
  callBtn: { width: 48, height: 48, borderRadius: 24, justifyContent: "center", alignItems: "center" },
  timelineSection: { backgroundColor: "#fff", marginHorizontal: 16, borderRadius: 16, padding: 16 },
  timelineRow: { flexDirection: "row", alignItems: "center", paddingVertical: 10 },
  timelineLabel: { flex: 1, marginLeft: 12, fontSize: 14, fontWeight: "500", color: "#111" },
  timelineTime: { fontSize: 13, color: "#666" },
  proofSection: { backgroundColor: "#fff", margin: 16, borderRadius: 16, padding: 16 },
  proofTitle: { fontSize: 15, fontWeight: "600", color: "#111" },
  proofNote: { marginTop: 4, fontSize: 13, color: "#666" },
});
