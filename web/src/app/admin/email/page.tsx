"use client";

import { useEffect, useState } from "react";
import { Send, Eye, Plus, BarChart3, FlaskConical } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  sendMarketingEmail,
  getCampaigns,
  createCampaign,
  sendCampaign,
  createAbTestCampaign,
  getCampaignAnalytics,
  getCampaignSummary,
  type CampaignDto,
  type CampaignAnalyticsDto,
  type CampaignSummaryDto,
} from "@/lib/adminApi";

const STATUS_COLORS: Record<string, string> = {
  Draft: "bg-gray-100 text-gray-600",
  Scheduled: "bg-blue-100 text-blue-700",
  Sending: "bg-yellow-100 text-yellow-700",
  Sent: "bg-emerald-100 text-emerald-700",
  Failed: "bg-red-100 text-red-700",
};

type TabType = "compose" | "abtest" | "campaigns" | "analytics";

export default function AdminEmailPage() {
  const t = useTranslations();

  const TARGETS = [
    { value: "all", label: t("admin.email.targetAll") },
    { value: "new_users", label: t("admin.email.targetNew") },
    { value: "vip", label: t("admin.email.targetVip") },
    { value: "inactive", label: t("admin.email.targetInactive") },
  ];

  const [tab, setTab] = useState<TabType>("compose");
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [target, setTarget] = useState("all");
  const [sending, setSending] = useState(false);
  const [showPreview, setShowPreview] = useState(false);

  // Campaign state
  const [campaigns, setCampaigns] = useState<CampaignDto[]>([]);
  const [campaignsLoading, setCampaignsLoading] = useState(false);
  const [scheduleDate, setScheduleDate] = useState("");

  // A/B Test state
  const [abTitle, setAbTitle] = useState("");
  const [abTarget, setAbTarget] = useState("all");
  const [abSchedule, setAbSchedule] = useState("");
  const [subjectA, setSubjectA] = useState("");
  const [contentA, setContentA] = useState("");
  const [subjectB, setSubjectB] = useState("");
  const [contentB, setContentB] = useState("");
  const [trafficA, setTrafficA] = useState(50);

  // Analytics state
  const [summary, setSummary] = useState<CampaignSummaryDto | null>(null);
  const [selectedAnalytics, setSelectedAnalytics] = useState<CampaignAnalyticsDto | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(false);

  const loadCampaigns = () => {
    setCampaignsLoading(true);
    getCampaigns()
      .then(setCampaigns)
      .catch(() => {})
      .finally(() => setCampaignsLoading(false));
  };

  const loadSummary = () => {
    setAnalyticsLoading(true);
    getCampaignSummary()
      .then(setSummary)
      .catch(() => {})
      .finally(() => setAnalyticsLoading(false));
  };

  useEffect(() => {
    if (tab === "campaigns") loadCampaigns();
    if (tab === "analytics") {
      loadSummary();
      loadCampaigns();
    }
  }, [tab]);

  const handleSend = async () => {
    if (!title.trim() || !content.trim()) {
      alert(t("admin.email.titleContentRequired"));
      return;
    }
    const targetLabel = TARGETS.find((tgt) => tgt.value === target)?.label || target;
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
      await createCampaign(title, content, target, scheduleDate || undefined);
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

  const handleCreateAbTest = async () => {
    if (!abTitle.trim() || !subjectA.trim() || !contentA.trim() || !subjectB.trim() || !contentB.trim()) {
      alert(t("admin.email.abTestRequired"));
      return;
    }
    try {
      await createAbTestCampaign(
        abTitle, abTarget, abSchedule || undefined,
        subjectA, contentA, subjectB, contentB, trafficA
      );
      alert(t("admin.email.abTestCreated"));
      setAbTitle(""); setSubjectA(""); setContentA(""); setSubjectB(""); setContentB(""); setAbSchedule("");
      setTab("campaigns");
    } catch {
      alert(t("admin.email.campaignSaveFailed"));
    }
  };

  const handleViewAnalytics = async (id: number) => {
    try {
      const data = await getCampaignAnalytics(id);
      setSelectedAnalytics(data);
    } catch {
      alert(t("admin.email.loadFailed"));
    }
  };

  return (
    <div className="max-w-4xl">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("admin.email.marketingEmail")}
      </h1>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 bg-gray-100 rounded-lg p-1">
        {(["compose", "abtest", "campaigns", "analytics"] as TabType[]).map((tabKey) => (
          <button
            key={tabKey}
            onClick={() => setTab(tabKey)}
            className={`flex-1 py-2 rounded-md text-sm font-medium transition-colors flex items-center justify-center gap-1 ${
              tab === tabKey ? "bg-white text-gray-900 shadow-sm" : "text-gray-500"
            }`}
          >
            {tabKey === "abtest" && <FlaskConical size={14} />}
            {tabKey === "analytics" && <BarChart3 size={14} />}
            {t(`admin.email.tab_${tabKey}`)}
          </button>
        ))}
      </div>

      {/* Compose Tab */}
      {tab === "compose" && (
        <div className="space-y-6">
          <div className="bg-white rounded-xl shadow-sm p-6">
            <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
              {t("admin.email.compose")}
            </h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("admin.email.target")}</label>
                <select value={target} onChange={(e) => setTarget(e.target.value)} className="w-full px-3 py-2.5 border rounded-lg text-sm">
                  {TARGETS.map((tgt) => (<option key={tgt.value} value={tgt.value}>{tgt.label}</option>))}
                </select>
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("admin.email.titleLabel")}</label>
                <input type="text" value={title} onChange={(e) => setTitle(e.target.value)} placeholder={t("admin.email.subjectPlaceholder")} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("admin.email.contentHtml")}</label>
                <textarea value={content} onChange={(e) => setContent(e.target.value)} rows={10} placeholder={t("admin.email.emailPlaceholder")} className="w-full px-3 py-2.5 border rounded-lg text-sm resize-none font-mono" />
              </div>
              <div>
                <label className="block text-sm text-gray-500 mb-1">{t("admin.email.scheduledSend")}</label>
                <input type="datetime-local" value={scheduleDate} onChange={(e) => setScheduleDate(e.target.value)} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
              </div>
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => setShowPreview(!showPreview)} className="flex items-center gap-2 py-3 px-4 border rounded-lg text-sm font-medium text-gray-500 hover:bg-gray-50">
              <Eye size={16} /> {showPreview ? t("admin.email.closePreview") : t("admin.email.preview")}
            </button>
            <button onClick={handleSaveCampaign} disabled={!title.trim() || !content.trim()} className="flex items-center gap-2 py-3 px-4 border border-emerald-300 text-emerald-600 rounded-lg text-sm font-medium hover:bg-emerald-50 disabled:opacity-60">
              <Plus size={16} /> {t("admin.email.saveCampaign")}
            </button>
            <button onClick={handleSend} disabled={sending || !title.trim() || !content.trim()} className="flex-1 flex items-center justify-center gap-2 py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60">
              <Send size={16} /> {sending ? t("admin.email.sending") : t("admin.email.sendNow")}
            </button>
          </div>
          {showPreview && (
            <div className="bg-white rounded-xl shadow-sm p-6">
              <h2 className="font-semibold text-[var(--color-secondary)] mb-4">{t("admin.email.preview")}</h2>
              <div className="border rounded-lg p-4">
                <div className="bg-[var(--color-primary)] text-white p-4 rounded-t-lg -m-4 mb-4">
                  <h3 className="font-bold">{title || t("admin.email.noTitle")}</h3>
                </div>
                <div className="prose prose-sm max-w-none mt-4" dangerouslySetInnerHTML={{ __html: content || `<p style='color:#999'>${t("admin.email.noContent")}</p>` }} />
              </div>
            </div>
          )}
        </div>
      )}

      {/* A/B Test Tab */}
      {tab === "abtest" && (
        <div className="space-y-6">
          <div className="bg-white rounded-xl shadow-sm p-6">
            <h2 className="font-semibold text-[var(--color-secondary)] mb-4 flex items-center gap-2">
              <FlaskConical size={18} /> {t("admin.email.abTestTitle")}
            </h2>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm text-gray-500 mb-1">{t("admin.email.campaignTitle")}</label>
                  <input type="text" value={abTitle} onChange={(e) => setAbTitle(e.target.value)} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
                </div>
                <div>
                  <label className="block text-sm text-gray-500 mb-1">{t("admin.email.target")}</label>
                  <select value={abTarget} onChange={(e) => setAbTarget(e.target.value)} className="w-full px-3 py-2.5 border rounded-lg text-sm">
                    {TARGETS.map((tgt) => (<option key={tgt.value} value={tgt.value}>{tgt.label}</option>))}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm text-gray-500 mb-1">{t("admin.email.scheduledSend")}</label>
                  <input type="datetime-local" value={abSchedule} onChange={(e) => setAbSchedule(e.target.value)} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
                </div>
                <div>
                  <label className="block text-sm text-gray-500 mb-1">{t("admin.email.trafficSplit")}</label>
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-medium text-blue-600">A: {trafficA}%</span>
                    <input type="range" min={10} max={90} value={trafficA} onChange={(e) => setTrafficA(Number(e.target.value))} className="flex-1" />
                    <span className="text-xs font-medium text-purple-600">B: {100 - trafficA}%</span>
                  </div>
                </div>
              </div>

              {/* Variant A */}
              <div className="border-l-4 border-blue-400 pl-4">
                <h3 className="text-sm font-bold text-blue-600 mb-2">{t("admin.email.variantA")}</h3>
                <input type="text" value={subjectA} onChange={(e) => setSubjectA(e.target.value)} placeholder={t("admin.email.subjectPlaceholder")} className="w-full px-3 py-2 border rounded-lg text-sm mb-2" />
                <textarea value={contentA} onChange={(e) => setContentA(e.target.value)} rows={6} placeholder={t("admin.email.emailPlaceholder")} className="w-full px-3 py-2 border rounded-lg text-sm resize-none font-mono" />
              </div>

              {/* Variant B */}
              <div className="border-l-4 border-purple-400 pl-4">
                <h3 className="text-sm font-bold text-purple-600 mb-2">{t("admin.email.variantB")}</h3>
                <input type="text" value={subjectB} onChange={(e) => setSubjectB(e.target.value)} placeholder={t("admin.email.subjectPlaceholder")} className="w-full px-3 py-2 border rounded-lg text-sm mb-2" />
                <textarea value={contentB} onChange={(e) => setContentB(e.target.value)} rows={6} placeholder={t("admin.email.emailPlaceholder")} className="w-full px-3 py-2 border rounded-lg text-sm resize-none font-mono" />
              </div>
            </div>
          </div>
          <button onClick={handleCreateAbTest} disabled={!abTitle.trim() || !subjectA.trim() || !subjectB.trim()} className="w-full flex items-center justify-center gap-2 py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60">
            <FlaskConical size={16} /> {t("admin.email.createAbTest")}
          </button>
        </div>
      )}

      {/* Campaigns Tab */}
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
                <div key={c.id} className="bg-white rounded-xl shadow-sm p-5 flex items-center justify-between">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <h3 className="font-medium text-gray-900 truncate">{c.title}</h3>
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[c.status] || STATUS_COLORS.Draft}`}>
                        {c.status}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 text-xs text-gray-400">
                      <span>{t("admin.email.campaignTarget", { target: TARGETS.find((tgt) => tgt.value === c.target)?.label || c.target })}</span>
                      {c.sentAt && <span>{t("admin.email.campaignSent", { date: new Date(c.sentAt).toLocaleDateString("ko-KR") })}</span>}
                      {c.sentCount > 0 && <span>{t("admin.email.campaignStats", { success: c.sentCount, fail: c.failCount })}</span>}
                      {c.scheduledAt && c.status === "Scheduled" && <span>{t("admin.email.campaignScheduledAt", { date: new Date(c.scheduledAt).toLocaleString("ko-KR") })}</span>}
                    </div>
                  </div>
                  <div className="flex items-center gap-2 ml-4">
                    {c.status === "Sent" && (
                      <button onClick={() => handleViewAnalytics(c.id)} className="flex items-center gap-1 px-3 py-1.5 border rounded-lg text-xs font-medium text-gray-600 hover:bg-gray-50">
                        <BarChart3 size={12} /> {t("admin.email.viewAnalytics")}
                      </button>
                    )}
                    {(c.status === "Draft" || c.status === "Scheduled") && (
                      <button onClick={() => handleSendCampaign(c.id)} className="flex items-center gap-1 px-3 py-1.5 bg-[var(--color-primary)] text-white rounded-lg text-xs font-medium hover:opacity-90">
                        <Send size={12} /> {t("admin.email.send")}
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Analytics Detail Modal */}
          {selectedAnalytics && (
            <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" onClick={() => setSelectedAnalytics(null)}>
              <div className="bg-white rounded-2xl shadow-xl max-w-2xl w-full max-h-[80vh] overflow-y-auto p-6" onClick={(e) => e.stopPropagation()}>
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-lg font-bold text-[var(--color-secondary)]">{selectedAnalytics.title}</h2>
                  <button onClick={() => setSelectedAnalytics(null)} className="text-gray-400 hover:text-gray-600 text-xl">&times;</button>
                </div>
                <div className="grid grid-cols-4 gap-3 mb-6">
                  <MetricCard label={t("admin.email.sent")} value={selectedAnalytics.sentCount.toLocaleString()} />
                  <MetricCard label={t("admin.email.openRate")} value={`${selectedAnalytics.openRate}%`} />
                  <MetricCard label={t("admin.email.clickRate")} value={`${selectedAnalytics.clickRate}%`} />
                  <MetricCard label={t("admin.email.conversionRate")} value={`${selectedAnalytics.conversionRate}%`} />
                </div>
                {selectedAnalytics.variants && selectedAnalytics.variants.length > 0 && (
                  <div>
                    <h3 className="text-sm font-bold text-gray-700 mb-3">{t("admin.email.abTestResults")}</h3>
                    <div className="space-y-3">
                      {selectedAnalytics.variants.map((v) => (
                        <div key={v.id} className={`border rounded-lg p-4 ${v.isWinner ? "border-emerald-400 bg-emerald-50" : ""}`}>
                          <div className="flex items-center justify-between mb-2">
                            <span className={`text-sm font-bold ${v.variantName === "A" ? "text-blue-600" : "text-purple-600"}`}>
                              {t("admin.email.variant")} {v.variantName} {v.isWinner && "- Winner"}
                            </span>
                            <span className="text-xs text-gray-400">{v.trafficPercent}% {t("admin.email.traffic")}</span>
                          </div>
                          <p className="text-sm text-gray-600 mb-2">{v.subjectLine}</p>
                          <div className="grid grid-cols-4 gap-2 text-xs">
                            <div><span className="text-gray-400">{t("admin.email.sent")}:</span> {v.sentCount}</div>
                            <div><span className="text-gray-400">{t("admin.email.openRate")}:</span> {v.openRate}%</div>
                            <div><span className="text-gray-400">{t("admin.email.clickRate")}:</span> {v.clickRate}%</div>
                            <div><span className="text-gray-400">{t("admin.email.conversionRate")}:</span> {v.conversionRate}%</div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Analytics Tab */}
      {tab === "analytics" && (
        <div className="space-y-6">
          {analyticsLoading ? (
            <div className="flex items-center justify-center py-12">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
            </div>
          ) : summary ? (
            <>
              <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
                <SummaryCard label={t("admin.email.totalCampaigns")} value={summary.totalCampaigns} />
                <SummaryCard label={t("admin.email.totalSent")} value={summary.totalSent.toLocaleString()} />
                <SummaryCard label={t("admin.email.avgOpenRate")} value={`${summary.avgOpenRate}%`} />
                <SummaryCard label={t("admin.email.avgClickRate")} value={`${summary.avgClickRate}%`} />
                <SummaryCard label={t("admin.email.totalRevenue")} value={`${summary.totalRevenue.toLocaleString()}원`} />
              </div>
              <div className="bg-white rounded-xl shadow-sm p-6">
                <h2 className="font-semibold text-[var(--color-secondary)] mb-4">{t("admin.email.campaignPerformance")}</h2>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-gray-400 border-b">
                        <th className="pb-2 font-medium">{t("admin.email.titleLabel")}</th>
                        <th className="pb-2 font-medium">{t("admin.email.statusLabel")}</th>
                        <th className="pb-2 font-medium text-right">{t("admin.email.sent")}</th>
                        <th className="pb-2 font-medium text-right">{t("admin.email.openRate")}</th>
                        <th className="pb-2 font-medium text-right">{t("admin.email.clickRate")}</th>
                        <th className="pb-2 font-medium"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {campaigns.filter(c => c.status === "Sent").map((c) => (
                        <tr key={c.id} className="hover:bg-gray-50">
                          <td className="py-3 font-medium">{c.title}</td>
                          <td className="py-3">
                            <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[c.status]}`}>
                              {c.status}
                            </span>
                          </td>
                          <td className="py-3 text-right">{c.sentCount.toLocaleString()}</td>
                          <td className="py-3 text-right">-</td>
                          <td className="py-3 text-right">-</td>
                          <td className="py-3 text-right">
                            <button onClick={() => handleViewAnalytics(c.id)} className="text-[var(--color-primary)] text-xs hover:underline">
                              {t("admin.email.details")}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </>
          ) : (
            <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-400 text-sm">
              {t("admin.email.noAnalytics")}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-gray-50 rounded-lg p-3 text-center">
      <p className="text-xs text-gray-400 mb-1">{label}</p>
      <p className="text-lg font-bold text-[var(--color-secondary)]">{value}</p>
    </div>
  );
}

function SummaryCard({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="bg-white rounded-xl shadow-sm p-4 text-center">
      <p className="text-xs text-gray-400 mb-1">{label}</p>
      <p className="text-xl font-bold text-[var(--color-secondary)]">{value}</p>
    </div>
  );
}
