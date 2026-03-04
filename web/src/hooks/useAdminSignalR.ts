"use client";

import { useEffect, useRef, useState, useCallback } from "react";
import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "@/stores/authStore";

export interface AdminEvent {
  type: "NewOrder" | "OrderStatusChanged" | "MesSyncCompleted" | "AutoReorderTriggered";
  data: Record<string, unknown>;
  timestamp: string;
}

export function useAdminSignalR() {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [events, setEvents] = useState<AdminEvent[]>([]);
  const { isAuthenticated } = useAuthStore();

  const addEvent = useCallback((type: AdminEvent["type"], data: Record<string, unknown>) => {
    const event: AdminEvent = {
      type,
      data,
      timestamp: new Date().toISOString(),
    };
    setEvents((prev) => [event, ...prev].slice(0, 50)); // Keep last 50 events
  }, []);

  useEffect(() => {
    if (!isAuthenticated) return;
    const accessToken = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
    if (!accessToken) return;

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://127.0.0.1:5100";
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/api/hubs/admin`, {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("NewOrder", (data) => {
      addEvent("NewOrder", data);
    });

    connection.on("OrderStatusChanged", (data) => {
      addEvent("OrderStatusChanged", data);
    });

    connection.on("MesSyncCompleted", (data) => {
      addEvent("MesSyncCompleted", data);
    });

    connection.on("AutoReorderTriggered", (data) => {
      addEvent("AutoReorderTriggered", data);
    });

    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));
    connection.onreconnecting(() => setIsConnected(false));

    connection
      .start()
      .then(() => setIsConnected(true))
      .catch((err) => console.error("SignalR connection failed:", err));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [isAuthenticated, addEvent]);

  const clearEvents = useCallback(() => setEvents([]), []);

  return { isConnected, events, clearEvents };
}
