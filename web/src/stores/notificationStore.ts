import { create } from "zustand";
import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
} from "@microsoft/signalr";
import type { NotificationDto } from "@/lib/notificationApi";

interface NotificationState {
  unreadCount: number;
  latestNotification: NotificationDto | null;
  connection: HubConnection | null;
  connect: (accessToken: string) => void;
  disconnect: () => void;
  setUnreadCount: (count: number) => void;
}

export const useNotificationStore = create<NotificationState>((set, get) => ({
  unreadCount: 0,
  latestNotification: null,
  connection: null,

  connect: (accessToken: string) => {
    const existing = get().connection;
    if (existing) return;

    const conn = new HubConnectionBuilder()
      .withUrl("/api/hubs/notifications", {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();

    conn.on("ReceiveNotification", (notification: NotificationDto) => {
      set({ latestNotification: notification });
    });

    conn.on("UpdateUnreadCount", (count: number) => {
      set({ unreadCount: count });
    });

    conn
      .start()
      .then(() => set({ connection: conn }))
      .catch(() => {});
  },

  disconnect: () => {
    const conn = get().connection;
    if (conn) {
      conn.stop().catch(() => {});
      set({ connection: null, unreadCount: 0, latestNotification: null });
    }
  },

  setUnreadCount: (count: number) => set({ unreadCount: count }),
}));
