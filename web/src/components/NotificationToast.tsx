"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useNotificationStore } from "@/stores/notificationStore";
import { ShoppingBag, Monitor, Tag, Users, X } from "lucide-react";
import type { NotificationDto } from "@/lib/notificationApi";

const TYPE_CONFIG: Record<string, { icon: typeof ShoppingBag; bg: string; border: string }> = {
  Order: { icon: ShoppingBag, bg: "bg-blue-50", border: "border-blue-400" },
  System: { icon: Monitor, bg: "bg-gray-50", border: "border-gray-400" },
  Promotion: { icon: Tag, bg: "bg-orange-50", border: "border-orange-400" },
  Social: { icon: Users, bg: "bg-purple-50", border: "border-purple-400" },
};

export function NotificationToast() {
  const router = useRouter();
  const latestNotification = useNotificationStore((s) => s.latestNotification);
  const [toast, setToast] = useState<NotificationDto | null>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (!latestNotification) return;
    setToast(latestNotification);
    setVisible(true);

    const timer = setTimeout(() => setVisible(false), 3000);
    return () => clearTimeout(timer);
  }, [latestNotification]);

  if (!visible || !toast) return null;

  const config = TYPE_CONFIG[toast.type] || TYPE_CONFIG.System;
  const Icon = config.icon;

  const handleClick = () => {
    setVisible(false);
    if (toast.referenceType === "Order" && toast.referenceId) {
      router.push(`/mypage/orders/${toast.referenceId}`);
    } else {
      router.push("/mypage/notifications");
    }
  };

  return (
    <div
      className={`fixed top-4 right-4 z-[100] max-w-sm w-full ${config.bg} border-l-4 ${config.border} rounded-lg shadow-lg p-4 cursor-pointer animate-slide-in`}
      onClick={handleClick}
    >
      <div className="flex items-start gap-3">
        <Icon size={20} className="flex-shrink-0 mt-0.5 opacity-70" />
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-900 truncate">{toast.title}</p>
          {toast.message && (
            <p className="text-xs text-gray-500 mt-0.5 line-clamp-2">{toast.message}</p>
          )}
        </div>
        <button
          onClick={(e) => { e.stopPropagation(); setVisible(false); }}
          className="flex-shrink-0 text-gray-400 hover:text-gray-600"
          aria-label="Dismiss notification"
        >
          <X size={16} />
        </button>
      </div>
    </div>
  );
}
