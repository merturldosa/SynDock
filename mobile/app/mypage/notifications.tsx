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
import { getNotifications, markAllRead } from "../../src/lib/api";
import { EmptyState } from "../../src/components/ui/EmptyState";
import { Button } from "../../src/components/ui/Button";
import type { Notification } from "../../src/types";

const ICON_MAP: Record<string, keyof typeof Ionicons.glyphMap> = {
  Order: "receipt-outline",
  Shipping: "airplane-outline",
  Point: "diamond-outline",
  Coupon: "ticket-outline",
  System: "megaphone-outline",
};

export default function NotificationsScreen() {
  const theme = useTheme();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  const load = async (p = 1) => {
    try {
      const res = await getNotifications(p);
      if (p === 1) {
        setNotifications(res.data.items);
      } else {
        setNotifications((prev) => [...prev, ...res.data.items]);
      }
      setHasMore(res.data.hasNext);
    } catch {}
    setIsLoading(false);
  };

  useEffect(() => {
    load();
  }, []);

  const handleMarkAllRead = async () => {
    await markAllRead();
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
  };

  const loadMore = () => {
    if (!hasMore) return;
    const next = page + 1;
    setPage(next);
    load(next);
  };

  const timeAgo = (dateStr: string) => {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${mins}분 전`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}시간 전`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}일 전`;
    return new Date(dateStr).toLocaleDateString("ko-KR");
  };

  if (isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" color={theme.primary} />
      </View>
    );
  }

  if (notifications.length === 0) {
    return (
      <EmptyState
        icon="notifications-outline"
        title="알림이 없습니다"
        description="주문 상태, 이벤트 등의 알림이 여기에 표시됩니다"
      />
    );
  }

  const hasUnread = notifications.some((n) => !n.isRead);

  return (
    <View style={[styles.container, { backgroundColor: theme.background }]}>
      {hasUnread && (
        <View style={[styles.toolbar, { backgroundColor: theme.surface }]}>
          <Button
            title="모두 읽음 처리"
            variant="ghost"
            size="sm"
            onPress={handleMarkAllRead}
          />
        </View>
      )}
      <FlatList
        data={notifications}
        keyExtractor={(item) => String(item.id)}
        onEndReached={loadMore}
        onEndReachedThreshold={0.5}
        renderItem={({ item }) => (
          <View
            style={[
              styles.notifItem,
              {
                backgroundColor: item.isRead
                  ? theme.surface
                  : theme.primary + "08",
              },
            ]}
          >
            <View
              style={[
                styles.iconBox,
                { backgroundColor: theme.primary + "15" },
              ]}
            >
              <Ionicons
                name={ICON_MAP[item.type] || "notifications-outline"}
                size={20}
                color={theme.primary}
              />
            </View>
            <View style={styles.notifContent}>
              <Text style={[styles.notifTitle, { color: theme.text }]}>
                {item.title}
              </Text>
              <Text
                style={[styles.notifMessage, { color: theme.textSecondary }]}
                numberOfLines={2}
              >
                {item.message}
              </Text>
              <Text style={{ color: theme.textSecondary, fontSize: 11, marginTop: 4 }}>
                {timeAgo(item.createdAt)}
              </Text>
            </View>
            {!item.isRead && (
              <View style={[styles.unreadDot, { backgroundColor: theme.error }]} />
            )}
          </View>
        )}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  center: { flex: 1, alignItems: "center", justifyContent: "center" },
  toolbar: {
    flexDirection: "row",
    justifyContent: "flex-end",
    padding: 8,
    borderBottomWidth: 0.5,
    borderBottomColor: "#F0F0F0",
  },
  notifItem: {
    flexDirection: "row",
    alignItems: "flex-start",
    padding: 16,
    gap: 12,
    borderBottomWidth: 0.5,
    borderBottomColor: "#F0F0F0",
  },
  iconBox: {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: "center",
    justifyContent: "center",
  },
  notifContent: { flex: 1 },
  notifTitle: { fontSize: 14, fontWeight: "600" },
  notifMessage: { fontSize: 13, marginTop: 2, lineHeight: 18 },
  unreadDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginTop: 4,
  },
});
