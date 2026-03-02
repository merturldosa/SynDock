"use client";

import { useState } from "react";
import { Send, Bell } from "lucide-react";
import { useTranslations } from "next-intl";
import { broadcastNotification } from "@/lib/adminApi";

export default function AdminNotificationsPage() {
  const t = useTranslations();

  const NOTIFICATION_TYPES = [
    { value: "System", label: t("admin.notifications.typeSystem") },
    { value: "Promotion", label: t("admin.notifications.typePromotion") },
    { value: "Order", label: t("admin.notifications.typeOrder") },
    { value: "Social", label: t("admin.notifications.typeSocial") },
  ];

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
      setResult(t("admin.notifications.sentCount", { count: res.sentCount }));
      setTitle("");
      setMessage("");
      setType("System");
    } catch {
      setResult(t("admin.notifications.sendFailed"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-8">
        <Bell size={28} className="text-[var(--color-primary)]" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          {t("admin.notifications.title")}
        </h1>
      </div>

      <form onSubmit={handleSubmit} className="bg-white rounded-2xl shadow-sm p-6 space-y-5">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">{t("admin.notifications.notificationType")}</label>
          <select
            value={type}
            onChange={(e) => setType(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[var(--color-primary)] focus:border-transparent outline-none"
          >
            {NOTIFICATION_TYPES.map((nt) => (
              <option key={nt.value} value={nt.value}>
                {nt.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">{t("admin.notifications.titleLabel")}</label>
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t("admin.notifications.titlePlaceholder")}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[var(--color-primary)] focus:border-transparent outline-none"
            required
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">{t("admin.notifications.contentLabel")}</label>
          <textarea
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder={t("admin.notifications.contentPlaceholder")}
            rows={4}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[var(--color-primary)] focus:border-transparent outline-none resize-none"
          />
        </div>

        {result && (
          <div className={`p-3 rounded-lg text-sm ${result.includes(t("admin.notifications.sendFailed")) ? "bg-red-50 text-red-600" : "bg-green-50 text-green-600"}`}>
            {result}
          </div>
        )}

        <button
          type="submit"
          disabled={loading || !title.trim()}
          className="w-full flex items-center justify-center gap-2 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          <Send size={18} />
          {loading ? t("admin.email.sending") : t("admin.notifications.sendAll")}
        </button>
      </form>
    </div>
  );
}
