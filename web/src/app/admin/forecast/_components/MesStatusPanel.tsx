"use client";

import { useEffect, useState } from "react";
import {
  Server,
  RefreshCw,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Loader2,
  ChevronDown,
  ChevronUp,
  Clock,
  History,
} from "lucide-react";
import { useTranslations } from "next-intl";
import {
  getMesSyncStatus,
  triggerMesSync,
  getMesDiscrepancies,
  getMesSyncHistory,
  type MesSyncStatus,
  type MesStockDiscrepancy,
  type MesSyncHistory,
} from "@/lib/forecastApi";

export default function MesStatusPanel() {
  const t = useTranslations();
  const [status, setStatus] = useState<MesSyncStatus | null>(null);
  const [discrepancies, setDiscrepancies] = useState<MesStockDiscrepancy[]>([]);
  const [syncHistory, setSyncHistory] = useState<MesSyncHistory[]>([]);
  const [historyTotal, setHistoryTotal] = useState(0);
  const [historyPage, setHistoryPage] = useState(1);
  const [expandedRow, setExpandedRow] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);

  const loadData = async () => {
    setLoading(true);
    try {
      const [s, d, h] = await Promise.all([
        getMesSyncStatus(),
        getMesDiscrepancies().catch(() => []),
        getMesSyncHistory(1, 10).catch(() => ({ items: [], total: 0, page: 1, pageSize: 10 })),
      ]);
      setStatus(s);
      setDiscrepancies(d);
      setSyncHistory(h.items);
      setHistoryTotal(h.total);
    } catch {
      setStatus({
        isConnected: false,
        lastSyncAt: null,
        syncedProductCount: 0,
        errorMessage: t("admin.forecast.mes.notConfigured"),
      });
    }
    setLoading(false);
  };

  const loadHistoryPage = async (page: number) => {
    try {
      const h = await getMesSyncHistory(page, 10);
      setSyncHistory(h.items);
      setHistoryTotal(h.total);
      setHistoryPage(page);
      setExpandedRow(null);
    } catch {
      // ignore
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleSync = async () => {
    setSyncing(true);
    try {
      await triggerMesSync();
      await loadData();
    } catch {
      // ignore
    }
    setSyncing(false);
  };

  const statusBadge = (s: string) => {
    switch (s) {
      case "Completed":
        return "text-green-700 bg-green-100";
      case "Running":
        return "text-blue-700 bg-blue-100";
      case "Failed":
        return "text-red-700 bg-red-100";
      default:
        return "text-gray-700 bg-gray-100";
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  const totalPages = Math.ceil(historyTotal / 10);

  return (
    <div className="space-y-6">
      {/* Connection Status */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Server size={18} className="text-gray-600" />
            <h3 className="font-semibold text-[var(--color-secondary)]">
              {t("admin.forecast.mes.title")}
            </h3>
          </div>
          <button
            onClick={handleSync}
            disabled={syncing || !status?.isConnected}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90 disabled:opacity-60"
          >
            {syncing ? (
              <Loader2 size={14} className="animate-spin" />
            ) : (
              <RefreshCw size={14} />
            )}
            {syncing
              ? t("admin.forecast.mes.syncing")
              : t("admin.forecast.mes.syncNow")}
          </button>
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div className="bg-gray-50 rounded-lg p-4 text-center">
            <div className="flex items-center justify-center gap-1.5 mb-1">
              {status?.isConnected ? (
                <CheckCircle size={16} className="text-green-500" />
              ) : (
                <XCircle size={16} className="text-red-500" />
              )}
              <span
                className={`text-sm font-medium ${
                  status?.isConnected ? "text-green-700" : "text-red-700"
                }`}
              >
                {status?.isConnected
                  ? t("admin.forecast.mes.connected")
                  : t("admin.forecast.mes.disconnected")}
              </span>
            </div>
          </div>

          <div className="bg-gray-50 rounded-lg p-4 text-center">
            <p className="text-xs text-gray-500 mb-1">
              {t("admin.forecast.mes.lastSync")}
            </p>
            <p className="text-sm font-medium">
              {status?.lastSyncAt
                ? new Date(status.lastSyncAt).toLocaleString()
                : "-"}
            </p>
          </div>

          <div className="bg-gray-50 rounded-lg p-4 text-center">
            <p className="text-xs text-gray-500 mb-1">
              {t("admin.forecast.mes.syncedProducts")}
            </p>
            <p className="text-lg font-bold text-[var(--color-primary)]">
              {status?.syncedProductCount ?? 0}
            </p>
          </div>
        </div>

        {status?.errorMessage && !status.isConnected && (
          <div className="mt-3 flex items-center gap-2 text-sm text-orange-600 bg-orange-50 rounded-lg p-3">
            <AlertTriangle size={14} />
            <span>{status.errorMessage}</span>
          </div>
        )}
      </div>

      {/* Sync History */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <div className="flex items-center gap-2 mb-4">
          <History size={18} className="text-blue-500" />
          <h3 className="font-semibold text-[var(--color-secondary)]">
            {t("admin.forecast.mes.syncHistory")}
          </h3>
          <span className="text-xs text-gray-400 ml-auto">
            {t("admin.forecast.mes.totalRecords", { count: historyTotal })}
          </span>
        </div>

        {syncHistory.length === 0 ? (
          <div className="text-center py-8 text-gray-400">
            <Clock size={32} className="mx-auto mb-2 opacity-40" />
            <p>{t("admin.forecast.mes.noHistory")}</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-gray-500">
                    <th className="py-3 pr-4 font-medium">{t("admin.forecast.mes.syncTime")}</th>
                    <th className="py-3 pr-4 font-medium text-right">{t("admin.forecast.mes.elapsed")}</th>
                    <th className="py-3 pr-4 font-medium text-right">{t("admin.forecast.mes.successFail")}</th>
                    <th className="py-3 pr-4 font-medium">{t("admin.forecast.mes.statusLabel")}</th>
                    <th className="py-3 font-medium w-10"></th>
                  </tr>
                </thead>
                <tbody>
                  {syncHistory.map((h) => (
                    <>
                      <tr
                        key={h.id}
                        className="border-b last:border-0 cursor-pointer hover:bg-gray-50"
                        onClick={() => setExpandedRow(expandedRow === h.id ? null : h.id)}
                      >
                        <td className="py-3 pr-4">
                          {new Date(h.startedAt).toLocaleString()}
                        </td>
                        <td className="py-3 pr-4 text-right font-mono text-gray-600">
                          {(h.elapsedMs / 1000).toFixed(1)}s
                        </td>
                        <td className="py-3 pr-4 text-right">
                          <span className="text-green-600">{h.successCount}</span>
                          {" / "}
                          <span className={h.failedCount > 0 ? "text-red-600" : "text-gray-400"}>
                            {h.failedCount}
                          </span>
                        </td>
                        <td className="py-3 pr-4">
                          <span
                            className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${statusBadge(h.status)}`}
                          >
                            {h.status}
                          </span>
                        </td>
                        <td className="py-3">
                          {(h.errorDetailsJson || h.conflictDetailsJson) && (
                            expandedRow === h.id ? (
                              <ChevronUp size={14} className="text-gray-400" />
                            ) : (
                              <ChevronDown size={14} className="text-gray-400" />
                            )
                          )}
                        </td>
                      </tr>
                      {expandedRow === h.id && (h.errorDetailsJson || h.conflictDetailsJson) && (
                        <tr key={`${h.id}-detail`}>
                          <td colSpan={5} className="py-3 px-4 bg-gray-50">
                            {h.errorDetailsJson && (
                              <div className="mb-2">
                                <p className="text-xs font-medium text-red-600 mb-1">
                                  {t("admin.forecast.mes.errorDetails")}
                                </p>
                                <pre className="text-xs text-gray-600 whitespace-pre-wrap bg-white rounded p-2 border">
                                  {JSON.stringify(JSON.parse(h.errorDetailsJson), null, 2)}
                                </pre>
                              </div>
                            )}
                            {h.conflictDetailsJson && (
                              <div>
                                <p className="text-xs font-medium text-orange-600 mb-1">
                                  {t("admin.forecast.mes.conflictDetails")}
                                </p>
                                <pre className="text-xs text-gray-600 whitespace-pre-wrap bg-white rounded p-2 border">
                                  {JSON.stringify(JSON.parse(h.conflictDetailsJson), null, 2)}
                                </pre>
                              </div>
                            )}
                          </td>
                        </tr>
                      )}
                    </>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-center gap-2 mt-4">
                <button
                  onClick={() => loadHistoryPage(historyPage - 1)}
                  disabled={historyPage <= 1}
                  className="px-3 py-1 text-sm rounded border disabled:opacity-40 hover:bg-gray-50"
                >
                  {t("admin.forecast.mes.prev")}
                </button>
                <span className="text-sm text-gray-500">
                  {historyPage} / {totalPages}
                </span>
                <button
                  onClick={() => loadHistoryPage(historyPage + 1)}
                  disabled={historyPage >= totalPages}
                  className="px-3 py-1 text-sm rounded border disabled:opacity-40 hover:bg-gray-50"
                >
                  {t("admin.forecast.mes.next")}
                </button>
              </div>
            )}
          </>
        )}
      </div>

      {/* Discrepancies */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <div className="flex items-center gap-2 mb-4">
          <AlertTriangle size={18} className="text-orange-500" />
          <h3 className="font-semibold text-[var(--color-secondary)]">
            {t("admin.forecast.mes.discrepancies")}
          </h3>
        </div>

        {discrepancies.length === 0 ? (
          <div className="text-center py-8 text-gray-400">
            <CheckCircle size={32} className="mx-auto mb-2 opacity-40" />
            <p>{t("admin.forecast.mes.noDiscrepancies")}</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-gray-500">
                  <th className="py-3 pr-4 font-medium">
                    {t("admin.forecast.productCol")}
                  </th>
                  <th className="py-3 pr-4 font-medium text-right">
                    {t("admin.forecast.mes.shopStock")}
                  </th>
                  <th className="py-3 pr-4 font-medium text-right">
                    {t("admin.forecast.mes.mesStock")}
                  </th>
                  <th className="py-3 font-medium text-right">
                    {t("admin.forecast.mes.difference")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {discrepancies.map((d) => (
                  <tr
                    key={d.productId}
                    className="border-b last:border-0"
                  >
                    <td className="py-3 pr-4">
                      <div className="font-medium text-gray-900">
                        {d.productName}
                      </div>
                      <div className="text-xs text-gray-400">
                        {d.productCode}
                      </div>
                    </td>
                    <td className="py-3 pr-4 text-right font-mono">
                      {d.shopStock.toLocaleString()}
                    </td>
                    <td className="py-3 pr-4 text-right font-mono">
                      {d.mesStock.toLocaleString()}
                    </td>
                    <td
                      className={`py-3 text-right font-mono font-medium ${
                        d.difference > 0
                          ? "text-green-600"
                          : "text-red-600"
                      }`}
                    >
                      {d.difference > 0 ? "+" : ""}
                      {d.difference}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
