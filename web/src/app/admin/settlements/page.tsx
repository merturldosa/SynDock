"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import {
  getMySettlements, getMyCommissions, getMyCommissionSettings,
  type SettlementDto, type CommissionDto, type CommissionSettingDto,
} from "@/lib/adminApi";
import { DollarSign, Clock, CheckCircle, FileText, Settings2, Receipt } from "lucide-react";
import { formatPrice } from "@/lib/format";

function StatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    Pending: "bg-yellow-100 text-yellow-700",
    Ready: "bg-blue-100 text-blue-700",
    Processing: "bg-orange-100 text-orange-700",
    Completed: "bg-green-100 text-green-700",
    Failed: "bg-red-100 text-red-700",
    Settled: "bg-green-100 text-green-700",
    Paid: "bg-emerald-100 text-emerald-700",
  };
  return (
    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${styles[status] || "bg-gray-100 text-gray-600"}`}>
      {status}
    </span>
  );
}

type TabType = "settlements" | "commissions" | "settings";

export default function AdminSettlementsPage() {
  const t = useTranslations("admin.settlements");
  const [activeTab, setActiveTab] = useState<TabType>("settlements");
  const [settlements, setSettlements] = useState<SettlementDto[]>([]);
  const [commissions, setCommissions] = useState<CommissionDto[]>([]);
  const [settings, setSettings] = useState<CommissionSettingDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<string>("");

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    if (activeTab === "settlements") {
      setLoading(true);
      getMySettlements(statusFilter || undefined)
        .then(setSettlements)
        .catch(() => setSettlements([]))
        .finally(() => setLoading(false));
    } else if (activeTab === "commissions") {
      setLoading(true);
      getMyCommissions(statusFilter || undefined)
        .then(setCommissions)
        .catch(() => setCommissions([]))
        .finally(() => setLoading(false));
    }
  }, [activeTab, statusFilter]);

  const loadData = async () => {
    try {
      const [settData, commData, settingsData] = await Promise.all([
        getMySettlements(),
        getMyCommissions(),
        getMyCommissionSettings(),
      ]);
      setSettlements(settData);
      setCommissions(commData);
      setSettings(settingsData);
    } catch {
      // fallback
    }
    setLoading(false);
  };

  const totalSettled = settlements
    .filter((s) => s.status === "Completed")
    .reduce((sum, s) => sum + s.totalSettlementAmount, 0);
  const totalPending = settlements
    .filter((s) => s.status !== "Completed" && s.status !== "Failed")
    .reduce((sum, s) => sum + s.totalSettlementAmount, 0);
  const totalCommission = commissions.reduce((sum, c) => sum + c.commissionAmount, 0);

  const tabs: { key: TabType; label: string; icon: typeof FileText }[] = [
    { key: "settlements", label: t("tabSettlements"), icon: FileText },
    { key: "commissions", label: t("tabCommissions"), icon: Receipt },
    { key: "settings", label: t("tabSettings"), icon: Settings2 },
  ];

  if (loading && settlements.length === 0) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">{t("title")}</h1>
        <p className="text-sm text-gray-500 mt-1">{t("subtitle")}</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <CheckCircle className="w-5 h-5 text-green-600" />
            </div>
            <span className="text-sm text-gray-500">{t("totalSettled")}</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(totalSettled)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-orange-50 flex items-center justify-center">
              <Clock className="w-5 h-5 text-orange-600" />
            </div>
            <span className="text-sm text-gray-500">{t("pendingAmount")}</span>
          </div>
          <p className="text-2xl font-bold text-orange-600">{formatPrice(totalPending)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">{t("totalCommission")}</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(totalCommission)}</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-gray-200">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => { setActiveTab(tab.key); setStatusFilter(""); }}
            className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
              activeTab === tab.key
                ? "border-[var(--color-primary)] text-[var(--color-primary)]"
                : "border-transparent text-gray-500 hover:text-gray-700"
            }`}
          >
            <tab.icon size={16} />
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {activeTab === "settlements" && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900">{t("settlementHistory")}</h2>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="px-3 py-1.5 border rounded-lg text-sm text-gray-600"
            >
              <option value="">{t("allStatus")}</option>
              <option value="Pending">Pending</option>
              <option value="Ready">Ready</option>
              <option value="Processing">Processing</option>
              <option value="Completed">Completed</option>
              <option value="Failed">Failed</option>
            </select>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left font-medium text-gray-500">{t("period")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("orderCount")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("orderTotal")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("commission")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("settlementAmount")}</th>
                  <th className="px-4 py-3 text-center font-medium text-gray-500">{t("status")}</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-500">{t("bankInfo")}</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-500">{t("settledAt")}</th>
                </tr>
              </thead>
              <tbody>
                {settlements.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center text-gray-400">
                      {t("noSettlements")}
                    </td>
                  </tr>
                ) : (
                  settlements.map((s) => (
                    <tr key={s.id} className="border-t border-gray-100 hover:bg-gray-50">
                      <td className="px-4 py-3 text-gray-700 whitespace-nowrap">
                        {s.periodStart.slice(0, 10)} ~ {s.periodEnd.slice(0, 10)}
                      </td>
                      <td className="px-4 py-3 text-right">{s.orderCount}{t("countUnit")}</td>
                      <td className="px-4 py-3 text-right">{formatPrice(s.totalOrderAmount)}</td>
                      <td className="px-4 py-3 text-right text-green-600">{formatPrice(s.totalCommission)}</td>
                      <td className="px-4 py-3 text-right font-medium">{formatPrice(s.totalSettlementAmount)}</td>
                      <td className="px-4 py-3 text-center"><StatusBadge status={s.status} /></td>
                      <td className="px-4 py-3 text-gray-500 text-xs">
                        {s.bankName ? `${s.bankName} ${s.bankAccount}` : "-"}
                      </td>
                      <td className="px-4 py-3 text-gray-500 text-xs">
                        {s.settledAt ? new Date(s.settledAt).toLocaleDateString("ko-KR") : "-"}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === "commissions" && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900">{t("commissionHistory")}</h2>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="px-3 py-1.5 border rounded-lg text-sm text-gray-600"
            >
              <option value="">{t("allStatus")}</option>
              <option value="Pending">Pending</option>
              <option value="Settled">Settled</option>
              <option value="Paid">Paid</option>
            </select>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left font-medium text-gray-500">{t("orderId")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("orderAmount")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("commissionRate")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("commissionAmount")}</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-500">{t("settlementAmount")}</th>
                  <th className="px-4 py-3 text-center font-medium text-gray-500">{t("status")}</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-500">{t("date")}</th>
                </tr>
              </thead>
              <tbody>
                {commissions.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-gray-400">
                      {t("noCommissions")}
                    </td>
                  </tr>
                ) : (
                  commissions.map((c) => (
                    <tr key={c.id} className="border-t border-gray-100 hover:bg-gray-50">
                      <td className="px-4 py-3 text-gray-700">#{c.orderId}</td>
                      <td className="px-4 py-3 text-right">{formatPrice(c.orderAmount)}</td>
                      <td className="px-4 py-3 text-right">{(c.commissionRate * 100).toFixed(1)}%</td>
                      <td className="px-4 py-3 text-right text-green-600">{formatPrice(c.commissionAmount)}</td>
                      <td className="px-4 py-3 text-right font-medium">{formatPrice(c.settlementAmount)}</td>
                      <td className="px-4 py-3 text-center"><StatusBadge status={c.status} /></td>
                      <td className="px-4 py-3 text-gray-500 text-xs">
                        {new Date(c.createdAt).toLocaleDateString("ko-KR")}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === "settings" && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="font-semibold text-gray-900">{t("commissionSettings")}</h2>
          </div>
          {settings.length === 0 ? (
            <div className="px-6 py-8 text-center text-gray-400">{t("noSettings")}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left font-medium text-gray-500">{t("scope")}</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">{t("commissionRate")}</th>
                    <th className="px-4 py-3 text-center font-medium text-gray-500">{t("settlementCycle")}</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">{t("minAmount")}</th>
                    <th className="px-4 py-3 text-left font-medium text-gray-500">{t("bankInfo")}</th>
                  </tr>
                </thead>
                <tbody>
                  {settings.map((s) => (
                    <tr key={s.id} className="border-t border-gray-100 hover:bg-gray-50">
                      <td className="px-4 py-3 text-gray-700">
                        {s.productId
                          ? `${t("product")} #${s.productId}`
                          : s.categoryId
                            ? `${t("category")} #${s.categoryId}`
                            : t("defaultSetting")}
                      </td>
                      <td className="px-4 py-3 text-right font-medium">{(s.commissionRate * 100).toFixed(1)}%</td>
                      <td className="px-4 py-3 text-center">{s.settlementCycle}</td>
                      <td className="px-4 py-3 text-right">{formatPrice(s.minSettlementAmount)}</td>
                      <td className="px-4 py-3 text-gray-500 text-xs">
                        {s.bankName ? `${s.bankName} ${s.bankAccount} (${s.bankHolder})` : "-"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
