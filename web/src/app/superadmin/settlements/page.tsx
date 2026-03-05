"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import {
  getCommissionSummary, getSettlements, createSettlement, processSettlement,
  type CommissionSummaryItem, type SettlementDto,
} from "@/lib/adminApi";
import { DollarSign, Building2, Clock, CheckCircle, AlertCircle, Plus, Send } from "lucide-react";
import toast from "react-hot-toast";
import { formatPrice } from "@/lib/format";

function StatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    Pending: "bg-yellow-100 text-yellow-700",
    Ready: "bg-blue-100 text-blue-700",
    Processing: "bg-orange-100 text-orange-700",
    Completed: "bg-green-100 text-green-700",
    Failed: "bg-red-100 text-red-700",
  };
  return (
    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${styles[status] || "bg-gray-100 text-gray-600"}`}>
      {status}
    </span>
  );
}

export default function SettlementsPage() {
  const t = useTranslations();
  const [summary, setSummary] = useState<CommissionSummaryItem[]>([]);
  const [selectedTenant, setSelectedTenant] = useState<string | null>(null);
  const [settlements, setSettlements] = useState<SettlementDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [settlementsLoading, setSettlementsLoading] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [periodStart, setPeriodStart] = useState("");
  const [periodEnd, setPeriodEnd] = useState("");
  const [processId, setProcessId] = useState<number | null>(null);
  const [transactionId, setTransactionId] = useState("");
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    getCommissionSummary()
      .then(setSummary)
      .catch(() => { toast.error(t("common.fetchError")); })
      .finally(() => setLoading(false));
  }, []);

  const loadSettlements = async (tenantName: string) => {
    const tenant = summary.find((s) => s.tenantName === tenantName);
    if (!tenant) return;
    setSelectedTenant(tenantName);
    setSettlementsLoading(true);
    // Use tenant name as slug approximation (lowercase)
    try {
      const data = await getSettlements(tenantName.toLowerCase());
      setSettlements(data);
    } catch {
      setSettlements([]);
    }
    setSettlementsLoading(false);
  };

  const handleCreate = async () => {
    if (!selectedTenant || !periodStart || !periodEnd) return;
    try {
      await createSettlement(selectedTenant.toLowerCase(), periodStart, periodEnd);
      setMessage(t("superadmin.settlements.created"));
      setShowCreate(false);
      loadSettlements(selectedTenant);
    } catch {
      setMessage(t("superadmin.settlements.createFailed"));
    }
  };

  const handleProcess = async () => {
    if (!processId || !transactionId) return;
    try {
      await processSettlement(processId, transactionId, "PlatformAdmin");
      setMessage(t("superadmin.settlements.processed"));
      setProcessId(null);
      setTransactionId("");
      if (selectedTenant) loadSettlements(selectedTenant);
    } catch {
      setMessage(t("superadmin.settlements.processFailed"));
    }
  };

  const totalPending = summary.reduce((s, t) => s + t.pendingSettlement, 0);
  const totalCommission = summary.reduce((s, t) => s + t.totalCommission, 0);
  const totalSettled = summary.reduce((s, t) => s + t.totalSettlementAmount, 0);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-blue-600 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">{t("superadmin.settlements.title")}</h1>
        <p className="text-sm text-gray-500 mt-1">{t("superadmin.settlements.subtitle")}</p>
      </div>

      {/* Toast */}
      {message && (
        <div className="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-3 rounded-lg text-sm flex items-center justify-between">
          {message}
          <button onClick={() => setMessage(null)} className="text-blue-500 hover:text-blue-700" aria-label="Dismiss message">×</button>
        </div>
      )}

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-green-600" />
            </div>
            <span className="text-sm text-gray-500">{t("superadmin.settlements.totalCommission")}</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(totalCommission)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <CheckCircle className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">{t("superadmin.settlements.totalSettled")}</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(totalSettled)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-orange-50 flex items-center justify-center">
              <Clock className="w-5 h-5 text-orange-600" />
            </div>
            <span className="text-sm text-gray-500">{t("superadmin.settlements.pendingBalance")}</span>
          </div>
          <p className="text-2xl font-bold text-orange-600">{formatPrice(totalPending)}</p>
        </div>
      </div>

      {/* Tenant Commission Summary */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="font-semibold text-gray-900">{t("superadmin.settlements.commissionByTenant")}</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left font-medium text-gray-500">{t("superadmin.settlements.tenant")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.orderCount")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.orderAmount")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.commission")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.settlementAmount")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.pending")}</th>
                <th className="px-6 py-3 text-center font-medium text-gray-500">{t("superadmin.settlements.manage")}</th>
              </tr>
            </thead>
            <tbody>
              {summary.length === 0 ? (
                <tr><td colSpan={7} className="px-6 py-8 text-center text-gray-400">{t("superadmin.settlements.noCommissionData")}</td></tr>
              ) : (
                summary.map((item) => (
                  <tr key={item.tenantId} className={`border-t border-gray-100 hover:bg-gray-50 ${selectedTenant === item.tenantName ? "bg-blue-50" : ""}`}>
                    <td className="px-6 py-3">
                      <div className="flex items-center gap-2">
                        <Building2 size={16} className="text-gray-400" />
                        <span className="font-medium text-gray-900">{item.tenantName}</span>
                      </div>
                    </td>
                    <td className="px-6 py-3 text-right">{t("common.orderCount", { count: item.totalOrders })}</td>
                    <td className="px-6 py-3 text-right">{formatPrice(item.totalOrderAmount)}</td>
                    <td className="px-6 py-3 text-right text-green-600 font-medium">{formatPrice(item.totalCommission)}</td>
                    <td className="px-6 py-3 text-right">{formatPrice(item.totalSettlementAmount)}</td>
                    <td className="px-6 py-3 text-right">
                      {item.pendingSettlement > 0 ? (
                        <span className="text-orange-600 font-medium">{formatPrice(item.pendingSettlement)}</span>
                      ) : (
                        <span className="text-gray-400">-</span>
                      )}
                    </td>
                    <td className="px-6 py-3 text-center">
                      <button
                        onClick={() => loadSettlements(item.tenantName)}
                        className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                      >
                        {t("superadmin.settlements.viewHistory")}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Selected Tenant Settlements */}
      {selectedTenant && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900">{t("superadmin.settlements.tenantHistory", { tenant: selectedTenant })}</h2>
            <button onClick={() => setShowCreate(true)}
              className="flex items-center gap-1 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700">
              <Plus size={14} /> {t("superadmin.settlements.createSettlement")}
            </button>
          </div>

          {/* Create Settlement Form */}
          {showCreate && (
            <div className="px-6 py-4 bg-blue-50 border-b border-blue-200">
              <div className="flex items-center gap-3 flex-wrap">
                <input type="date" value={periodStart} onChange={(e) => setPeriodStart(e.target.value)}
                  className="px-3 py-2 border rounded-lg text-sm" placeholder={t("common.startDate")} />
                <span className="text-gray-400">~</span>
                <input type="date" value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value)}
                  className="px-3 py-2 border rounded-lg text-sm" placeholder={t("common.endDate")} />
                <button onClick={handleCreate}
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700">{t("common.create")}</button>
                <button onClick={() => setShowCreate(false)}
                  className="px-4 py-2 bg-gray-200 text-gray-600 rounded-lg text-sm hover:bg-gray-300">{t("common.cancel")}</button>
              </div>
            </div>
          )}

          {settlementsLoading ? (
            <div className="flex items-center justify-center py-10">
              <div className="h-6 w-6 animate-spin rounded-full border-4 border-blue-600 border-t-transparent" />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left font-medium text-gray-500">{t("superadmin.settlements.period")}</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.orderCount")}</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.orderTotal")}</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.commission")}</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">{t("superadmin.settlements.settlementAmount")}</th>
                    <th className="px-4 py-3 text-center font-medium text-gray-500">{t("superadmin.settlements.status")}</th>
                    <th className="px-4 py-3 text-left font-medium text-gray-500">{t("superadmin.settlements.bankInfo")}</th>
                    <th className="px-4 py-3 text-center font-medium text-gray-500">{t("superadmin.settlements.actions")}</th>
                  </tr>
                </thead>
                <tbody>
                  {settlements.length === 0 ? (
                    <tr><td colSpan={8} className="px-4 py-8 text-center text-gray-400">{t("superadmin.settlements.noSettlements")}</td></tr>
                  ) : (
                    settlements.map((s) => (
                      <tr key={s.id} className="border-t border-gray-100 hover:bg-gray-50">
                        <td className="px-4 py-3 text-gray-700 whitespace-nowrap">
                          {s.periodStart.slice(0, 10)} ~ {s.periodEnd.slice(0, 10)}
                        </td>
                        <td className="px-4 py-3 text-right">{t("common.orderCount", { count: s.orderCount })}</td>
                        <td className="px-4 py-3 text-right">{formatPrice(s.totalOrderAmount)}</td>
                        <td className="px-4 py-3 text-right text-green-600">{formatPrice(s.totalCommission)}</td>
                        <td className="px-4 py-3 text-right font-medium">{formatPrice(s.totalSettlementAmount)}</td>
                        <td className="px-4 py-3 text-center"><StatusBadge status={s.status} /></td>
                        <td className="px-4 py-3 text-gray-500 text-xs">
                          {s.bankName ? `${s.bankName} ${s.bankAccount}` : "-"}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {(s.status === "Ready" || s.status === "Pending") && (
                            <button onClick={() => setProcessId(s.id)}
                              className="flex items-center gap-1 px-2 py-1 bg-green-600 text-white rounded text-xs hover:bg-green-700 mx-auto">
                              <Send size={12} /> {t("superadmin.settlements.process")}
                            </button>
                          )}
                          {s.status === "Completed" && s.transactionId && (
                            <span className="text-xs text-gray-400">TX: {s.transactionId}</span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Process Settlement Modal */}
      {processId && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl">
            <h3 className="text-lg font-semibold mb-4">{t("superadmin.settlements.processSettlement")}</h3>
            <p className="text-sm text-gray-500 mb-4">{t("superadmin.settlements.enterTransactionId")}</p>
            <input
              type="text"
              value={transactionId}
              onChange={(e) => setTransactionId(e.target.value)}
              placeholder={t("superadmin.settlements.transactionId")}
              className="w-full px-4 py-2 border rounded-lg text-sm mb-4"
            />
            <div className="flex gap-2 justify-end">
              <button onClick={() => { setProcessId(null); setTransactionId(""); }}
                className="px-4 py-2 bg-gray-200 text-gray-600 rounded-lg text-sm hover:bg-gray-300">{t("common.cancel")}</button>
              <button onClick={handleProcess} disabled={!transactionId}
                className="px-4 py-2 bg-green-600 text-white rounded-lg text-sm font-medium hover:bg-green-700 disabled:opacity-50">
                {t("superadmin.settlements.completeSettlement")}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
