"use client";

import { useEffect, useState } from "react";
import { Bell, Check, Trash2 } from "lucide-react";
import {
  getNotifications,
  markAsRead,
  markAllAsRead,
  deleteNotification,
  type NotificationDto,
  type PagedNotifications,
} from "@/lib/notificationApi";

const TYPE_LABELS: Record<string, { label: string; color: string }> = {
  Order: { label: "주문", color: "bg-blue-100 text-blue-700" },
  System: { label: "시스템", color: "bg-gray-100 text-gray-700" },
  Promotion: { label: "프로모션", color: "bg-orange-100 text-orange-700" },
  Social: { label: "소셜", color: "bg-purple-100 text-purple-700" },
};

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins}분 전`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}시간 전`;
  return new Date(dateStr).toLocaleDateString("ko-KR");
}

export default function NotificationsPage() {
  const [data, setData] = useState<PagedNotifications | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    getNotifications(page)
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [page]);

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
          알림
        </h1>
        <button
          onClick={handleReadAll}
          className="flex items-center gap-1 text-sm text-[var(--color-primary)] hover:underline"
        >
          <Check size={16} /> 모두 읽음
        </button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <Bell size={48} className="mx-auto mb-3 opacity-30" />
          <p>알림이 없습니다.</p>
        </div>
      ) : (
        <div className="space-y-2">
          {data.items.map((noti) => {
            const typeInfo = TYPE_LABELS[noti.type] || TYPE_LABELS.System;
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
                      className={`px-2 py-0.5 text-xs rounded-full ${typeInfo.color}`}
                    >
                      {typeInfo.label}
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
                      title="읽음 처리"
                    >
                      <Check size={14} />
                    </button>
                  )}
                  <button
                    onClick={() => handleDelete(noti.id)}
                    className="p-1.5 text-gray-400 hover:text-red-500"
                    title="삭제"
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
                이전
              </button>
              <span className="text-sm text-gray-500">
                {page} / {Math.ceil(data.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page * 20 >= data.totalCount}
                className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
              >
                다음
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
