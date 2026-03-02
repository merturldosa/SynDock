"use client";

import { useEffect, useState } from "react";
import { Send, Eye, Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  sendMarketingEmail,
  getCampaigns,
  createCampaign,
  sendCampaign,
  type CampaignDto,
} from "@/lib/adminApi";

const STATUS_COLORS: Record<string, string> = {
  Draft: "bg-gray-100 text-gray-600",
  Scheduled: "bg-blue-100 text-blue-700",
  Sending: "bg-yellow-100 text-yellow-700",
  Sent: "bg-emerald-100 text-emerald-700",
  Failed: "bg-red-100 text-red-700",
};

export default function AdminEmailPage() {
  const t = useTranslations();

  const TARGETS = [
    { value: "all", label: t("admin.email.targetAll") },
    { value: "new_users", label: t("admin.email.targetNew") },
    { value: "vip", label: t("admin.email.targetVip") },
    { value: "inactive", label: t("admin.email.targetInactive") },
  ];

  const [tab, setTab] = useState<"compose" | "campaigns">("compose");
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [target, setTarget] = useState("all");
  const [sending, setSending] = useState(false);
  const [showPreview, setShowPreview] = useState(false);

  // Campaign state
  const [campaigns, setCampaigns] = useState<CampaignDto[]>([]);
  const [campaignsLoading, setCampaignsLoading] = useState(false);
  const [scheduleDate, setScheduleDate] = useState("");

  const loadCampaigns = () => {
    setCampaignsLoading(true);
    getCampaigns()
      .then(setCampaigns)
      .catch(() => {})
      .finally(() => setCampaignsLoading(false));
  };

  useEffect(() => {
    if (tab === "campaigns") loadCampaigns();
  }, [tab]);

  const handleSend = async () => {
    if (!title.trim() || !content.trim()) {
      alert(t("admin.email.titleContentRequired"));
      return;
    }
    const targetLabel = TARGETS.find((t) => t.value === target)?.label || target;
    if (!confirm(t("admin.email.sendConfirm", { target: targetLabel }))) return;

    setSending(true);
    try {
      const { sentCount } = await sendMarketingEmail(title, content, target);
      alert(t("admin.email.sentCountMsg", { count: sentCount }));
      setTitle("");
      setContent("");
    } catch {
      alert(t("admin.email.sendFailed"));
    }
    setSending(false);
  };

  const handleSaveCampaign = async () => {
    if (!title.trim() || !content.trim()) {
      alert(t("admin.email.titleContentRequired"));
      return;
    }
    try {
      await createCampaign(
        title,
        content,
        target,
        scheduleDate || undefined
      );
      alert(scheduleDate ? t("admin.email.campaignScheduled") : t("admin.email.campaignSaved"));
      setTitle("");
      setContent("");
      setScheduleDate("");
      setTab("campaigns");
    } catch {
      alert(t("admin.email.campaignSaveFailed"));
    }
  };

  const handleSendCampaign = async (id: number) => {
    if (!confirm(t("admin.email.sendCampaignConfirm"))) return;
    try {
      const { sentCount } = await sendCampaign(id);
      alert(t("admin.email.sentComplete", { count: sentCount }));
      loadCampaigns();
    } catch {
      alert(t("admin.email.sendFailed"));
    }
  };

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("admin.email.marketingEmail")}
      </h1>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 bg-gray-100 rounded-lg p-1">
        <button
          onClick={() => setTab("compose")}
          className={`flex-1 py-2 rounded-md text-sm font-medium transition-colors ${
            tab === "compose" ? "bg-white text-gray-900 shadow-sm" : "text-gray-500"
          }`}
        >
          {t("admin.email.compose")}
        </button>
        <button
          onClick={() => setTab("campaigns")}
          className={`flex-1 py-2 rounded-md text-sm font-medium transition-colors ${
            tab === "campaigns" ? "bg-white text-gray-900 shadow-sm" : "text-gray-500"
          }`}
        >
          {t("admin.email.campaigns")}
        </button>
      </div>

      {tab === "compose" && (
        <div className="space-y-6">
          <div className="bg-white rounded-xl shadow-sm p-6">
            <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
              {t("admin.email.compose")}
            </h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("admin.email.target")}</label>
                <select
                  value={target}
                  onChange={(e) => setTarget(e.target.value)}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                >
                  {TARGETS.map((tgt) => (
                    <option key={tgt.value} value={tgt.value}>
                      {tgt.label}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("admin.email.titleLabel")}</label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder={t("admin.email.subjectPlaceholder")}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">
                  {t("admin.email.contentHtml")}
                </label>
                <textarea
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  rows={10}
                  placeholder={t("admin.email.emailPlaceholder")}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm resize-none font-mono"
                />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">
                  {t("admin.email.scheduledSend")}
                </label>
                <input
                  type="datetime-local"
                  value={scheduleDate}
                  onChange={(e) => setScheduleDate(e.target.value)}
                  className="w-full px-3 py-2.5 border rounded-lg text-sm"
                />
              </div>
            </div>
          </div>

          <div className="flex gap-3">
            <button
              onClick={() => setShowPreview(!showPreview)}
              className="flex items-center justify-center gap-2 py-3 px-4 border rounded-lg text-sm font-medium text-gray-500 hover:bg-gray-50"
            >
              <Eye size={16} />
              {showPreview ? t("admin.email.closePreview") : t("admin.email.preview")}
            </button>
            <button
              onClick={handleSaveCampaign}
              disabled={!title.trim() || !content.trim()}
              className="flex items-center justify-center gap-2 py-3 px-4 border border-emerald-300 text-emerald-600 rounded-lg text-sm font-medium hover:bg-emerald-50 disabled:opacity-60"
            >
              <Plus size={16} />
              {t("admin.email.saveCampaign")}
            </button>
            <button
              onClick={handleSend}
              disabled={sending || !title.trim() || !content.trim()}
              className="flex-1 flex items-center justify-center gap-2 py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
            >
              <Send size={16} />
              {sending ? t("admin.email.sending") : t("admin.email.sendNow")}
            </button>
          </div>

          {showPreview && (
            <div className="bg-white rounded-xl shadow-sm p-6">
              <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
                {t("admin.email.preview")}
              </h2>
              <div className="border rounded-lg p-4">
                <div className="bg-[var(--color-primary)] text-white p-4 rounded-t-lg -m-4 mb-4">
                  <h3 className="font-bold">{title || t("admin.email.noTitle")}</h3>
                </div>
                <div
                  className="prose prose-sm max-w-none mt-4"
                  dangerouslySetInnerHTML={{
                    __html: content || `<p style='color:#999'>${t("admin.email.noContent")}</p>`,
                  }}
                />
              </div>
            </div>
          )}
        </div>
      )}

      {tab === "campaigns" && (
        <div>
          {campaignsLoading ? (
            <div className="flex items-center justify-center py-12">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
            </div>
          ) : campaigns.length === 0 ? (
            <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-400 text-sm">
              {t("admin.email.noCampaigns")}
            </div>
          ) : (
            <div className="space-y-3">
              {campaigns.map((c) => (
                <div
                  key={c.id}
                  className="bg-white rounded-xl shadow-sm p-5 flex items-center justify-between"
                >
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <h3 className="font-medium text-gray-900 truncate">
                        {c.title}
                      </h3>
                      <span
                        className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                          STATUS_COLORS[c.status] || STATUS_COLORS.Draft
                        }`}
                      >
                        {c.status}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 text-xs text-gray-400">
                      <span>
                        {t("admin.email.campaignTarget", { target: TARGETS.find((tgt) => tgt.value === c.target)?.label || c.target })}
                      </span>
                      {c.sentAt && (
                        <span>
                          {t("admin.email.campaignSent", { date: new Date(c.sentAt).toLocaleDateString("ko-KR") })}
                        </span>
                      )}
                      {c.sentCount > 0 && (
                        <span>
                          {t("admin.email.campaignStats", { success: c.sentCount, fail: c.failCount })}
                        </span>
                      )}
                      {c.scheduledAt && c.status === "Scheduled" && (
                        <span>
                          {t("admin.email.campaignScheduledAt", { date: new Date(c.scheduledAt).toLocaleString("ko-KR") })}
                        </span>
                      )}
                    </div>
                  </div>
                  {(c.status === "Draft" || c.status === "Scheduled") && (
                    <button
                      onClick={() => handleSendCampaign(c.id)}
                      className="ml-4 flex items-center gap-1 px-3 py-1.5 bg-[var(--color-primary)] text-white rounded-lg text-xs font-medium hover:opacity-90"
                    >
                      <Send size={12} /> {t("admin.email.send")}
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
