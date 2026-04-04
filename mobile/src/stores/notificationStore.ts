import { create } from "zustand";
import { getUnreadCount, markAllRead as markAllReadApi } from "../lib/api";
import { registerForPushNotifications } from "../lib/notifications";

interface NotificationState {
  pushToken: string | null;
  unreadCount: number;
  isRegistered: boolean;
  registerPushToken: () => Promise<void>;
  fetchUnreadCount: () => Promise<void>;
  markAllRead: () => Promise<void>;
  setUnreadCount: (count: number) => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
  pushToken: null,
  unreadCount: 0,
  isRegistered: false,

  registerPushToken: async () => {
    try {
      const token = await registerForPushNotifications();
      set({ pushToken: token, isRegistered: token !== null });
    } catch (error) {
      console.log("Push token registration failed:", error);
    }
  },

  fetchUnreadCount: async () => {
    try {
      const res = await getUnreadCount();
      set({ unreadCount: res.data.count });
    } catch {
      // Silently fail
    }
  },

  markAllRead: async () => {
    try {
      await markAllReadApi();
      set({ unreadCount: 0 });
    } catch {
      // Silently fail
    }
  },

  setUnreadCount: (count: number) => set({ unreadCount: count }),
}));
