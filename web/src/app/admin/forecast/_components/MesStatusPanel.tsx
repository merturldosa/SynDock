"use client";

import { useEffect, useState } from "react";
import {
  Server,
  RefreshCw,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Loader2,
} from "lucide-react";
import { useTranslations } from "next-intl";
import {
  getMesSyncStatus,
  triggerMesSync,
  getMesDiscrepancies,
  type MesSyncStatus,
  type MesStockDiscrepancy,
} from "@/lib/forecastApi";

export default function MesStatusPanel() {
  const t = useTranslations();
  const [status, setStatus] = useState<MesSyncStatus | null>(null);
  const [discrepancies, setDiscrepancies] = useState<MesStockDiscrepancy[]>([]);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);

  const loadData = async () => {
    setLoading(true);
    try {
      const [s, d] = await Promise.all([
        getMesSyncStatus(),
        getMesDiscrepancies().catch(() => []),
      ]);
      setStatus(s);
      setDiscrepancies(d);
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

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

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
