import { create } from "zustand";
import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
} from "@microsoft/signalr";

interface AdminDashboardEvent {
  type: "NewOrder" | "OrderStatusChanged" | "MesSyncCompleted" | "AutoReorderTriggered";
  orderNumber: string;
  totalAmount?: number;
  newStatus?: string;
  syncedCount?: number;
  failedCount?: number;
  itemCount?: number;
  totalQuantity?: number;
  timestamp: string;
}

interface AdminDashboardState {
  lastEvent: AdminDashboardEvent | null;
  connection: HubConnection | null;
  connect: (accessToken: string) => void;
  disconnect: () => void;
  clearEvent: () => void;
}

export const useAdminDashboardStore = create<AdminDashboardState>((set, get) => ({
  lastEvent: null,
  connection: null,

  connect: (accessToken: string) => {
    const existing = get().connection;
    if (existing) return;

    const conn = new HubConnectionBuilder()
      .withUrl("/api/hubs/admin", {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();

    conn.on("NewOrder", (data: { orderNumber: string; totalAmount: number; timestamp: string }) => {
      set({
        lastEvent: {
          type: "NewOrder",
          orderNumber: data.orderNumber,
          totalAmount: data.totalAmount,
          timestamp: data.timestamp,
        },
      });
    });

    conn.on("OrderStatusChanged", (data: { orderNumber: string; newStatus: string; timestamp: string }) => {
      set({
        lastEvent: {
          type: "OrderStatusChanged",
          orderNumber: data.orderNumber,
          newStatus: data.newStatus,
          timestamp: data.timestamp,
        },
      });
    });

    conn.on("MesSyncCompleted", (data: { syncedCount: number; failedCount: number; timestamp: string }) => {
      set({
        lastEvent: {
          type: "MesSyncCompleted",
          orderNumber: "",
          syncedCount: data.syncedCount,
          failedCount: data.failedCount,
          timestamp: data.timestamp,
        },
      });
    });

    conn.on("AutoReorderTriggered", (data: { orderNumber: string; itemCount: number; totalQuantity: number; timestamp: string }) => {
      set({
        lastEvent: {
          type: "AutoReorderTriggered",
          orderNumber: data.orderNumber,
          itemCount: data.itemCount,
          totalQuantity: data.totalQuantity,
          timestamp: data.timestamp,
        },
      });
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
      set({ connection: null, lastEvent: null });
    }
  },

  clearEvent: () => set({ lastEvent: null }),
}));
