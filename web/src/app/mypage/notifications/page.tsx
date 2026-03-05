"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Bell, Check, Trash2 } from "lucide-react";
import toast from "react-hot-toast";
import {
  getNotifications,
  markAsRead,
  markAllAsRead,
  deleteNotification,
  type NotificationDto,
  type PagedNotifications,
} from "@/lib/notificationApi";
import { useNotificationStore } from "@/stores/notificationStore";

const TYPE_COLORS: Record<string, string> = {
  Order: "bg-blue-100 text-blue-700",
  System: "bg-gray-100 text-gray-700",
  Promotion: "bg-orange-100 text-orange-700",
  Social: "bg-purple-100 text-purple-700",
};

export default function NotificationsPage() {
  const t = useTranslations();

  const typeLabel = (type: string) => {
    const key = `mypage.notifications.type.${type}` as const;
    return t(key);
  };

  const timeAgo = (dateStr: string): string => {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return t("mypage.notifications.timeAgo.justNow");
    if (mins < 60) return t("mypage.notifications.timeAgo.minutesAgo", { count: mins });
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return t("mypage.notifications.timeAgo.hoursAgo", { count: hrs });
    const days = Math.floor(hrs / 24);
    return t("mypage.notifications.timeAgo.daysAgo", { count: days });
  };
  const [data, setData] = useState<PagedNotifications | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const latestNotification = useNotificationStore((s) => s.latestNotification);

  const load = () => {
    setLoading(true);
    getNotifications(page)
      .then(setData)
      .catch(() => toast.error(t("common.fetchError")))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [page]);

  // Auto-refresh when new real-time notification arrives
  useEffect(() => {
    if (latestNotification) load();
  }, [latestNotification]);

  const handleRead = async (id: number) => {
    await markAsRead(id);
    load();
  };

  const handleReadAll = async () => {
    await markAllAsRead();
    load();
  };

  const handleDelete = async (id: number) => {
    await deleteNotification(id);
    load();
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          {t("mypage.notifications.title")}
        </h1>
        <button
          onClick={handleReadAll}
          className="flex items-center gap-1 text-sm text-[var(--color-primary)] hover:underline"
        >
          <Check size={16} /> {t("mypage.notifications.markAllRead")}
        </button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <Bell size={48} className="mx-auto mb-3 opacity-30" />
          <p>{t("mypage.notifications.empty")}</p>
        </div>
      ) : (
        <div className="space-y-2">
          {data.items.map((noti) => {
            const typeColor = TYPE_COLORS[noti.type] || TYPE_COLORS.System;
            return (
              <div
                key={noti.id}
                className={`bg-white rounded-xl shadow-sm p-4 flex items-start gap-3 ${
                  !noti.isRead ? "border-l-4 border-[var(--color-primary)]" : ""
                }`}
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full ${typeColor}`}
                    >
                      {typeLabel(noti.type)}
                    </span>
                    <span className="text-xs text-gray-400">
                      {timeAgo(noti.createdAt)}
                    </span>
                  </div>
                  <p
                    className={`text-sm ${
                      noti.isRead ? "text-gray-500" : "text-gray-900 font-medium"
                    }`}
                  >
                    {noti.title}
                  </p>
                  {noti.message && (
                    <p className="text-xs text-gray-400 mt-0.5 line-clamp-2">
                      {noti.message}
                    </p>
                  )}
                </div>
                <div className="flex items-center gap-1 flex-shrink-0">
                  {!noti.isRead && (
                    <button
                      onClick={() => handleRead(noti.id)}
                      className="p-1.5 text-gray-400 hover:text-[var(--color-primary)]"
                      title={t("mypage.notifications.markRead")}
                      aria-label="Mark as read"
                    >
                      <Check size={14} />
                    </button>
                  )}
                  <button
                    onClick={() => handleDelete(noti.id)}
                    className="p-1.5 text-gray-400 hover:text-red-500"
                    title={t("common.delete")}
                    aria-label="Delete notification"
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>
            );
          })}

          {data.totalCount > 20 && (
            <div className="flex items-center justify-center gap-2 mt-6">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
              >
                {t("common.prev")}
              </button>
              <span className="text-sm text-gray-500">
                {page} / {Math.ceil(data.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page * 20 >= data.totalCount}
                className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
              >
                {t("common.next")}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
