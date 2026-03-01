"use client";

import { useState } from "react";
import { Send, Bell } from "lucide-react";
import { broadcastNotification } from "@/lib/adminApi";

const NOTIFICATION_TYPES = [
  { value: "System", label: "시스템" },
  { value: "Promotion", label: "프로모션" },
  { value: "Order", label: "주문" },
  { value: "Social", label: "소셜" },
];

export default function AdminNotificationsPage() {
  const [title, setTitle] = useState("");
  const [message, setMessage] = useState("");
  const [type, setType] = useState("System");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    setLoading(true);
    setResult(null);
    try {
      const res = await broadcastNotification(title, message, type);
      setResult(`${res.sentCount}명에게 알림이 발송되었습니다.`);
      setTitle("");
      setMessage("");
      setType("System");
    } catch {
      setResult("알림 발송에 실패했습니다.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-8">
        <Bell size={28} className="text-[var(--color-primary)]" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          알림 발송
        </h1>
      </div>

      <form onSubmit={handleSubmit} className="bg-white rounded-2xl shadow-sm p-6 space-y-5">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">알림 타입</label>
          <select
            value={type}
            onChange={(e) => setType(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[var(--color-primary)] focus:border-transparent outline-none"
          >
            {NOTIFICATION_TYPES.map((t) => (
              <option key={t.value} value={t.value}>
                {t.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">제목</label>
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="알림 제목을 입력하세요"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[var(--color-primary)] focus:border-transparent outline-none"
            required
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">내용</label>
          <textarea
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="알림 내용을 입력하세요"
            rows={4}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[var(--color-primary)] focus:border-transparent outline-none resize-none"
          />
        </div>

        {result && (
          <div className={`p-3 rounded-lg text-sm ${result.includes("실패") ? "bg-red-50 text-red-600" : "bg-green-50 text-green-600"}`}>
            {result}
          </div>
        )}

        <button
          type="submit"
          disabled={loading || !title.trim()}
          className="w-full flex items-center justify-center gap-2 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          <Send size={18} />
          {loading ? "발송 중..." : "전체 발송"}
        </button>
      </form>
    </div>
  );
}
