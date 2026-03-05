"use client";

import { useEffect, useState, useCallback } from "react";
import { Factory, CheckCircle, XCircle, Send, RefreshCw, X } from "lucide-react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";

interface ProductionSuggestion {
  id: number;
  productId: number;
  productName: string;
  currentStock: number;
  averageDailySales: number;
  estimatedDaysUntilStockout: number;
  suggestedQuantity: number;
  urgency: string;
  status: string;
  aiReason: string | null;
  trendAnalysis: string | null;
  confidenceScore: number | null;
  mesOrderId: string | null;
  approvedAt: string | null;
  approvedBy: string | null;
  createdAt: string;
}

const URGENCY_COLORS: Record<string, string> = {
  Critical: "bg-red-100 text-red-700",
  High: "bg-orange-100 text-orange-700",
  Normal: "bg-blue-100 text-blue-700",
  Low: "bg-gray-100 text-gray-600",
};

const STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  Approved: "bg-emerald-100 text-emerald-700",
  Rejected: "bg-red-100 text-red-700",
  Forwarded: "bg-blue-100 text-blue-700",
};

export default function ProductionPlanPage() {
  const t = useTranslations();
  const [suggestions, setSuggestions] = useState<ProductionSuggestion[]>([]);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [rejectModal, setRejectModal] = useState<{ id: number; reason: string } | null>(null);
  const [forwardModal, setForwardModal] = useState<number | null>(null);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const loadSuggestions = async () => {
    setLoading(true);
    try {
      const params = statusFilter ? { status: statusFilter } : {};
      const { data } = await api.get("/admin/mes/production-plan", { params });
      setSuggestions(data);
    } catch {}
    setLoading(false);
  };

  useEffect(() => { loadSuggestions(); }, [statusFilter]);

  const showMessage = useCallback((type: "success" | "error", text: string) => {
    setMessage({ type, text });
    setTimeout(() => setMessage(null), 3000);
  }, []);

  const handleGenerate = async () => {
    setGenerating(true);
    try {
      await api.post("/admin/mes/production-plan/generate");
      await loadSuggestions();
    } catch {
      showMessage("error", t("admin.productionPlan.generateFailed"));
    }
    setGenerating(false);
  };

  const handleApprove = async (id: number) => {
    try {
      await api.put(`/admin/mes/production-plan/${id}/approve`);
      await loadSuggestions();
    } catch {
      showMessage("error", t("admin.productionPlan.approveFailed"));
    }
  };

  const handleReject = async (id: number) => {
    setRejectModal({ id, reason: "" });
  };

  const submitReject = async () => {
    if (!rejectModal || !rejectModal.reason) return;
    try {
      await api.put(`/admin/mes/production-plan/${rejectModal.id}/reject`, { reason: rejectModal.reason });
      setRejectModal(null);
      await loadSuggestions();
    } catch {
      showMessage("error", t("admin.productionPlan.rejectFailed"));
    }
  };

  const handleForwardMes = async (id: number) => {
    setForwardModal(id);
  };

  const submitForward = async () => {
    if (forwardModal === null) return;
    try {
      const { data } = await api.post(`/admin/mes/production-plan/${forwardModal}/forward-mes`);
      setForwardModal(null);
      showMessage("success", t("admin.productionPlan.forwardSuccess", { mesOrderId: data.mesOrderId }));
      await loadSuggestions();
    } catch {
      showMessage("error", t("admin.productionPlan.forwardFailed"));
    }
  };

  const pendingCount = suggestions.filter(s => s.status === "Pending").length;
  const criticalCount = suggestions.filter(s => s.urgency === "Critical" && s.status === "Pending").length;

  return (
    <div className="max-w-5xl">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-secondary)] flex items-center gap-2">
            <Factory size={24} /> {t("admin.productionPlan.title")}
          </h1>
          <p className="text-sm text-gray-400 mt-1">{t("admin.productionPlan.subtitle")}</p>
        </div>
        <button
          onClick={handleGenerate}
          disabled={generating}
          className="flex items-center gap-2 px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
        >
          <RefreshCw size={16} className={generating ? "animate-spin" : ""} />
          {generating ? t("admin.productionPlan.generating") : t("admin.productionPlan.generate")}
        </button>
      </div>

      {/* Toast Message */}
      {message && (
        <div className={`fixed top-4 right-4 z-50 px-4 py-3 rounded-lg shadow-lg text-white text-sm ${
          message.type === "success" ? "bg-emerald-600" : "bg-red-600"
        }`}>
          {message.text}
        </div>
      )}

      {/* Reject Modal */}
      {rejectModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-md">
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-semibold">{t("admin.productionPlan.rejectReasonPrompt")}</h3>
              <button onClick={() => setRejectModal(null)} className="text-gray-400 hover:text-gray-600"><X size={20} /></button>
            </div>
            <textarea
              value={rejectModal.reason}
              onChange={(e) => setRejectModal({ ...rejectModal, reason: e.target.value })}
              className="w-full border rounded-lg p-3 text-sm mb-4 h-24 resize-none"
              autoFocus
            />
            <div className="flex gap-2 justify-end">
              <button onClick={() => setRejectModal(null)} className="px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 rounded-lg">
                {t("common.cancel")}
              </button>
              <button onClick={submitReject} disabled={!rejectModal.reason} className="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50">
                {t("admin.productionPlan.reject")}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Forward Confirm Modal */}
      {forwardModal !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-sm">
            <p className="text-sm mb-4">{t("admin.productionPlan.forwardConfirm")}</p>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setForwardModal(null)} className="px-4 py-2 text-sm text-gray-600 hover:bg-gray-100 rounded-lg">
                {t("common.cancel")}
              </button>
              <button onClick={submitForward} className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700">
                {t("common.confirm")}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <p className="text-xs text-gray-400">{t("admin.productionPlan.totalSuggestions")}</p>
          <p className="text-2xl font-bold text-[var(--color-secondary)]">{suggestions.length}</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <p className="text-xs text-gray-400">{t("admin.productionPlan.pending")}</p>
          <p className="text-2xl font-bold text-yellow-600">{pendingCount}</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <p className="text-xs text-gray-400">{t("admin.productionPlan.critical")}</p>
          <p className="text-2xl font-bold text-red-600">{criticalCount}</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <p className="text-xs text-gray-400">{t("admin.productionPlan.forwarded")}</p>
          <p className="text-2xl font-bold text-blue-600">{suggestions.filter(s => s.status === "Forwarded").length}</p>
        </div>
      </div>

      {/* Status Filter */}
      <div className="flex gap-2 mb-4">
        {["", "Pending", "Approved", "Rejected", "Forwarded"].map((status) => (
          <button
            key={status}
            onClick={() => setStatusFilter(status)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
              statusFilter === status
                ? "bg-[var(--color-primary)] text-white"
                : "bg-gray-100 text-gray-600 hover:bg-gray-200"
            }`}
          >
            {status || t("admin.productionPlan.all")}
          </button>
        ))}
      </div>

      {/* Suggestions List */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : suggestions.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-400 text-sm">
          {t("admin.productionPlan.noSuggestions")}
        </div>
      ) : (
        <div className="space-y-3">
          {suggestions.map((s) => (
            <div key={s.id} className="bg-white rounded-xl shadow-sm p-5">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <h3 className="font-semibold text-gray-900">{s.productName}</h3>
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${URGENCY_COLORS[s.urgency]}`}>
                      {s.urgency}
                    </span>
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[s.status]}`}>
                      {s.status}
                    </span>
                  </div>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm mb-2">
                    <div>
                      <span className="text-gray-400">{t("admin.productionPlan.currentStock")}:</span>{" "}
                      <span className="font-medium">{s.currentStock}</span>
                    </div>
                    <div>
                      <span className="text-gray-400">{t("admin.productionPlan.dailySales")}:</span>{" "}
                      <span className="font-medium">{s.averageDailySales.toFixed(1)}</span>
                    </div>
                    <div>
                      <span className="text-gray-400">{t("admin.productionPlan.daysUntilOut")}:</span>{" "}
                      <span className={`font-medium ${s.estimatedDaysUntilStockout <= 7 ? "text-red-600" : ""}`}>
                        {s.estimatedDaysUntilStockout}{t("admin.productionPlan.days")}
                      </span>
                    </div>
                    <div>
                      <span className="text-gray-400">{t("admin.productionPlan.suggested")}:</span>{" "}
                      <span className="font-bold text-[var(--color-primary)]">{s.suggestedQuantity}</span>
                    </div>
                  </div>
                  {s.aiReason && (
                    <p className="text-xs text-gray-500 bg-gray-50 rounded p-2 mt-2">
                      AI: {s.aiReason}
                    </p>
                  )}
                  {s.mesOrderId && (
                    <p className="text-xs text-blue-500 mt-1">MES Order: {s.mesOrderId}</p>
                  )}
                </div>
                {s.status === "Pending" && (
                  <div className="flex items-center gap-2 ml-4">
                    <button onClick={() => handleApprove(s.id)} className="p-2 text-emerald-600 hover:bg-emerald-50 rounded-lg" title={t("admin.productionPlan.approve")}>
                      <CheckCircle size={20} />
                    </button>
                    <button onClick={() => handleReject(s.id)} className="p-2 text-red-500 hover:bg-red-50 rounded-lg" title={t("admin.productionPlan.reject")}>
                      <XCircle size={20} />
                    </button>
                  </div>
                )}
                {s.status === "Approved" && (
                  <button onClick={() => handleForwardMes(s.id)} className="flex items-center gap-1 ml-4 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700">
                    <Send size={14} /> {t("admin.productionPlan.forwardToMes")}
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
