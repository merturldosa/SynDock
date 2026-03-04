import { useEffect, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  Alert,
} from "react-native";
import { useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useTheme } from "../../src/hooks/useTheme";
import { Button } from "../../src/components/ui/Button";
import { useAuthStore } from "../../src/stores/authStore";
import { getPointBalance, getUnreadCount } from "../../src/lib/api";

export default function MyPageScreen() {
  const theme = useTheme();
  const router = useRouter();
  const { user, isAuthenticated, logout } = useAuthStore();
  const [points, setPoints] = useState(0);
  const [unread, setUnread] = useState(0);

  useEffect(() => {
    if (isAuthenticated) {
      getPointBalance()
        .then((r) => setPoints(r.data.balance))
        .catch(() => {});
      getUnreadCount()
        .then((r) => setUnread(r.data.count))
        .catch(() => {});
    }
  }, [isAuthenticated]);

  if (!isAuthenticated) {
    return (
      <View style={[styles.centerContainer, { backgroundColor: theme.background }]}>
        <Ionicons name="person-circle-outline" size={80} color={theme.border} />
        <Text style={[styles.loginPrompt, { color: theme.text }]}>
          로그인하고 다양한 서비스를 이용하세요
        </Text>
        <Button
          title="로그인"
          onPress={() => router.push("/(auth)/login" as never)}
          style={{ width: 200, marginTop: 16 }}
        />
        <Button
          title="회원가입"
          onPress={() => router.push("/(auth)/register" as never)}
          variant="outline"
          style={{ width: 200, marginTop: 8 }}
        />
      </View>
    );
  }

  const isAdmin =
    user?.role === "TenantAdmin" ||
    user?.role === "Admin" ||
    user?.role === "PlatformAdmin";

  const menuItems = [
    { icon: "receipt-outline" as const, label: "주문 내역", route: "/mypage/orders" },
    { icon: "location-outline" as const, label: "배송지 관리", route: "/mypage/addresses" },
    { icon: "heart-outline" as const, label: "위시리스트", route: "/mypage/wishlist" },
    { icon: "star-outline" as const, label: "리뷰 관리", route: "/mypage/reviews" },
    { icon: "chatbubble-outline" as const, label: "Q&A", route: "/mypage/qna" },
    { icon: "ticket-outline" as const, label: "쿠폰함", route: "/mypage/coupons" },
    {
      icon: "notifications-outline" as const,
      label: "알림",
      route: "/mypage/notifications",
      badge: unread,
    },
  ];

  const handleLogout = () => {
    Alert.alert("로그아웃", "정말 로그아웃하시겠습니까?", [
      { text: "취소", style: "cancel" },
      {
        text: "로그아웃",
        style: "destructive",
        onPress: () => {
          logout();
          router.replace("/(tabs)/" as never);
        },
      },
    ]);
  };

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: theme.background }]}
    >
      {/* Profile Card */}
      <View style={[styles.profileCard, { backgroundColor: theme.secondary }]}>
        <View style={styles.profileInfo}>
          <Ionicons name="person-circle" size={48} color="#FFF" />
          <View>
            <Text style={styles.profileName}>{user?.name}</Text>
            <Text style={styles.profileEmail}>{user?.email}</Text>
          </View>
        </View>
        <View style={styles.statsRow}>
          <View style={styles.stat}>
            <Text style={styles.statValue}>{points.toLocaleString()}</Text>
            <Text style={styles.statLabel}>포인트</Text>
          </View>
          <View style={[styles.statDivider, { backgroundColor: "rgba(255,255,255,0.2)" }]} />
          <TouchableOpacity
            style={styles.stat}
            onPress={() => router.push("/mypage/orders" as never)}
          >
            <Ionicons name="receipt-outline" size={20} color="#FFF" />
            <Text style={styles.statLabel}>주문내역</Text>
          </TouchableOpacity>
          <View style={[styles.statDivider, { backgroundColor: "rgba(255,255,255,0.2)" }]} />
          <TouchableOpacity
            style={styles.stat}
            onPress={() => router.push("/mypage/wishlist" as never)}
          >
            <Ionicons name="heart-outline" size={20} color="#FFF" />
            <Text style={styles.statLabel}>찜 목록</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Menu List */}
      <View style={[styles.menuCard, { backgroundColor: theme.surface }]}>
        {menuItems.map((item) => (
          <TouchableOpacity
            key={item.label}
            style={styles.menuItem}
            onPress={() => router.push(item.route as never)}
          >
            <Ionicons name={item.icon} size={22} color={theme.text} />
            <Text style={[styles.menuLabel, { color: theme.text }]}>
              {item.label}
            </Text>
            <View style={{ flex: 1 }} />
            {item.badge ? (
              <View
                style={[styles.menuBadge, { backgroundColor: theme.error }]}
              >
                <Text style={styles.menuBadgeText}>{item.badge}</Text>
              </View>
            ) : null}
            <Ionicons
              name="chevron-forward"
              size={18}
              color={theme.textSecondary}
            />
          </TouchableOpacity>
        ))}
      </View>

      {/* Admin */}
      {isAdmin && (
        <TouchableOpacity
          style={[styles.adminBtn, { backgroundColor: theme.secondary }]}
          onPress={() => router.push("/admin" as never)}
        >
          <Ionicons name="settings-outline" size={20} color="#FFF" />
          <Text style={styles.adminBtnText}>관리자 대시보드</Text>
          <Ionicons name="chevron-forward" size={18} color="rgba(255,255,255,0.7)" />
        </TouchableOpacity>
      )}

      {/* Logout */}
      <TouchableOpacity
        style={[styles.logoutBtn, { borderColor: theme.border }]}
        onPress={handleLogout}
      >
        <Ionicons name="log-out-outline" size={20} color={theme.error} />
        <Text style={[styles.logoutText, { color: theme.error }]}>
          로그아웃
        </Text>
      </TouchableOpacity>

      <View style={{ height: 40 }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  centerContainer: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    padding: 40,
  },
  loginPrompt: { fontSize: 16, marginTop: 16, textAlign: "center" },
  profileCard: {
    margin: 16,
    borderRadius: 16,
    padding: 20,
  },
  profileInfo: { flexDirection: "row", alignItems: "center", gap: 12 },
  profileName: { color: "#FFF", fontSize: 18, fontWeight: "700" },
  profileEmail: { color: "rgba(255,255,255,0.7)", fontSize: 13, marginTop: 2 },
  statsRow: {
    flexDirection: "row",
    marginTop: 20,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: "rgba(255,255,255,0.15)",
  },
  stat: { flex: 1, alignItems: "center", gap: 4 },
  statValue: { color: "#FFF", fontSize: 16, fontWeight: "700" },
  statLabel: { color: "rgba(255,255,255,0.8)", fontSize: 12 },
  statDivider: { width: 1 },
  menuCard: {
    marginHorizontal: 16,
    borderRadius: 12,
    overflow: "hidden",
  },
  menuItem: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: 14,
    paddingHorizontal: 16,
    gap: 12,
    borderBottomWidth: 0.5,
    borderBottomColor: "#F0F0F0",
  },
  menuLabel: { fontSize: 15 },
  menuBadge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 10,
    marginRight: 4,
  },
  menuBadgeText: { color: "#FFF", fontSize: 11, fontWeight: "700" },
  adminBtn: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    margin: 16,
    marginBottom: 0,
    padding: 14,
    borderRadius: 12,
  },
  adminBtnText: { color: "#FFF", fontSize: 15, fontWeight: "600", flex: 1 },
  logoutBtn: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: 8,
    margin: 16,
    padding: 14,
    borderWidth: 1,
    borderRadius: 12,
  },
  logoutText: { fontSize: 15, fontWeight: "500" },
});
