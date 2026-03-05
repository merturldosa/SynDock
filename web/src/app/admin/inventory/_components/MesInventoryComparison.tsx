"use client";

import { useEffect, useState, useCallback } from "react";
import {
  RefreshCw,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Link2Off,
  ArrowDownToLine,
  Filter,
  Wifi,
  WifiOff,
} from "lucide-react";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import { formatDate } from "@/lib/format";
import {
  getMesSyncStatus,
  getMesInventoryComparison,
  syncMesProduct,
  triggerMesSync,
  type MesSyncStatus,
  type MesInventoryComparisonItem,
} from "@/lib/forecastApi";

type FilterType = "all" | "discrepancy" | "shop_only" | "mes_only";

export default function MesInventoryComparison() {
  const t = useTranslations();
  const [status, setStatus] = useState<MesSyncStatus | null>(null);
  const [items, setItems] = useState<MesInventoryComparisonItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [syncingProductId, setSyncingProductId] = useState<number | null>(null);
  const [filter, setFilter] = useState<FilterType>("all");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [s, c] = await Promise.all([
        getMesSyncStatus(),
        getMesInventoryComparison(),
      ]);
      setStatus(s);
      setItems(c);
    } catch {
      setStatus(null);
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const handleFullSync = async () => {
    setSyncing(true);
    try {
      await triggerMesSync();
      await load();
    } catch {
      // handled silently
    } finally {
      setSyncing(false);
    }
  };

  const handleSyncProduct = async (productId: number) => {
    setSyncingProductId(productId);
    try {
      await syncMesProduct(productId);
      await load();
    } catch {
      toast.error(t("admin.inventory.mes.syncFailed"));
    } finally {
      setSyncingProductId(null);
    }
  };

  const filtered = items.filter((item) => {
    if (filter === "all") return true;
    return item.status === filter;
  });

  const summary = {
    total: items.length,
    matched: items.filter((i) => i.status === "matched").length,
    discrepancy: items.filter((i) => i.status === "discrepancy").length,
    shopOnly: items.filter((i) => i.status === "shop_only").length,
    mesOnly: items.filter((i) => i.status === "mes_only").length,
  };

  const statusIcon = (s: string) => {
    switch (s) {
      case "matched":
        return <CheckCircle2 size={16} className="text-green-500" />;
      case "discrepancy":
        return <AlertTriangle size={16} className="text-amber-500" />;
      case "shop_only":
        return <Link2Off size={16} className="text-gray-400" />;
      case "mes_only":
        return <XCircle size={16} className="text-blue-500" />;
      default:
        return null;
    }
  };

  const statusLabel = (s: string) => {
    switch (s) {
      case "matched":
        return t("admin.inventory.mes.statusMatched");
      case "discrepancy":
        return t("admin.inventory.mes.statusDiscrepancy");
      case "shop_only":
        return t("admin.inventory.mes.statusShopOnly");
      case "mes_only":
        return t("admin.inventory.mes.statusMesOnly");
      default:
        return s;
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Connection Status Card */}
      <div
        className={`rounded-xl p-4 border ${
          status?.isConnected
            ? "bg-green-50 border-green-200"
            : "bg-red-50 border-red-200"
        }`}
      >
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            {status?.isConnected ? (
              <Wifi size={20} className="text-green-600" />
            ) : (
              <WifiOff size={20} className="text-red-500" />
            )}
            <div>
              <p className="font-semibold text-sm">
                {status?.isConnected
                  ? t("admin.inventory.mes.connected")
                  : t("admin.inventory.mes.disconnected")}
              </p>
              {status?.lastSyncAt && (
                <p className="text-xs text-gray-500">
                  {t("admin.inventory.mes.lastSync")}:{" "}
                  {formatDate(status.lastSyncAt)}
                </p>
              )}
            </div>
          </div>
          <button
            onClick={handleFullSync}
            disabled={syncing || !status?.isConnected}
            className="flex items-center gap-2 px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90 disabled:opacity-50"
          >
            <RefreshCw size={14} className={syncing ? "animate-spin" : ""} />
            {syncing
              ? t("admin.inventory.mes.syncing")
              : t("admin.inventory.mes.syncNow")}
          </button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
        <SummaryCard
          label={t("admin.inventory.mes.totalProducts")}
          value={summary.total}
          color="text-gray-700"
          bg="bg-gray-50"
        />
        <SummaryCard
          label={t("admin.inventory.mes.statusMatched")}
          value={summary.matched}
          color="text-green-700"
          bg="bg-green-50"
        />
        <SummaryCard
          label={t("admin.inventory.mes.statusDiscrepancy")}
          value={summary.discrepancy}
          color="text-amber-700"
          bg="bg-amber-50"
        />
        <SummaryCard
          label={t("admin.inventory.mes.statusShopOnly")}
          value={summary.shopOnly}
          color="text-gray-600"
          bg="bg-gray-50"
        />
        <SummaryCard
          label={t("admin.inventory.mes.statusMesOnly")}
          value={summary.mesOnly}
          color="text-blue-700"
          bg="bg-blue-50"
        />
      </div>

      {/* Filter */}
      <div className="flex items-center gap-2">
        <Filter size={14} className="text-gray-400" />
        <div className="flex gap-1">
          {(["all", "discrepancy", "shop_only", "mes_only"] as FilterType[]).map(
            (f) => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition ${
                  filter === f
                    ? "bg-[var(--color-primary)] text-white"
                    : "bg-gray-100 text-gray-600 hover:bg-gray-200"
                }`}
              >
                {f === "all"
                  ? t("admin.inventory.mes.filterAll")
                  : statusLabel(f)}
                {f !== "all" && (
                  <span className="ml-1 opacity-70">
                    ({f === "discrepancy" ? summary.discrepancy : f === "shop_only" ? summary.shopOnly : summary.mesOnly})
                  </span>
                )}
              </button>
            )
          )}
        </div>
      </div>

      {/* Comparison Table */}
      {filtered.length === 0 ? (
        <div className="text-center py-12 text-gray-400">
          <CheckCircle2 size={48} className="mx-auto mb-3 opacity-30" />
          <p>{t("admin.inventory.mes.noItems")}</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3 font-medium text-gray-500">
                  {t("admin.inventory.mes.productName")}
                </th>
                <th className="text-left p-3 font-medium text-gray-500">
                  {t("admin.inventory.mes.mesCode")}
                </th>
                <th className="text-center p-3 font-medium text-gray-500">
                  {t("admin.inventory.mes.shopStock")}
                </th>
                <th className="text-center p-3 font-medium text-gray-500">
                  {t("admin.inventory.mes.mesStock")}
                </th>
                <th className="text-center p-3 font-medium text-gray-500">
                  {t("admin.inventory.mes.difference")}
                </th>
                <th className="text-center p-3 font-medium text-gray-500">
                  {t("admin.inventory.mes.status")}
                </th>
                <th className="text-center p-3 font-medium text-gray-500">
                  {t("admin.inventory.mes.action")}
                </th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((item, idx) => (
                <tr
                  key={`${item.shopProductId ?? item.mesProductCode}-${idx}`}
                  className={`border-b last:border-0 hover:bg-gray-50 ${
                    item.status === "discrepancy"
                      ? "bg-amber-50/50"
                      : item.status === "mes_only"
                      ? "bg-blue-50/30"
                      : ""
                  }`}
                >
                  <td className="p-3 font-medium">{item.productName}</td>
                  <td className="p-3 text-gray-500 font-mono text-xs">
                    {item.mesProductCode || "-"}
                  </td>
                  <td className="p-3 text-center">
                    {item.status !== "mes_only"
                      ? item.shopStock.toLocaleString()
                      : "-"}
                  </td>
                  <td className="p-3 text-center">
                    {item.status !== "shop_only"
                      ? item.mesStock.toLocaleString()
                      : "-"}
                  </td>
                  <td className="p-3 text-center">
                    {item.status === "matched" ? (
                      <span className="text-green-600">0</span>
                    ) : item.status === "discrepancy" ? (
                      <span
                        className={`font-bold ${
                          item.difference > 0
                            ? "text-amber-600"
                            : "text-red-600"
                        }`}
                      >
                        {item.difference > 0
                          ? `+${item.difference}`
                          : item.difference}
                      </span>
                    ) : (
                      "-"
                    )}
                  </td>
                  <td className="p-3">
                    <div className="flex items-center justify-center gap-1.5">
                      {statusIcon(item.status)}
                      <span className="text-xs">{statusLabel(item.status)}</span>
                    </div>
                  </td>
                  <td className="p-3 text-center">
                    {item.shopProductId && item.status === "discrepancy" && (
                      <button
                        onClick={() => handleSyncProduct(item.shopProductId!)}
                        disabled={syncingProductId === item.shopProductId}
                        className="inline-flex items-center gap-1 px-2.5 py-1 bg-blue-600 text-white rounded text-xs hover:bg-blue-700 disabled:opacity-50"
                      >
                        <ArrowDownToLine size={12} />
                        {syncingProductId === item.shopProductId
                          ? t("admin.inventory.mes.syncing")
                          : t("admin.inventory.mes.syncToShop")}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function SummaryCard({
  label,
  value,
  color,
  bg,
}: {
  label: string;
  value: number;
  color: string;
  bg: string;
}) {
  return (
    <div className={`${bg} rounded-xl p-3 border`}>
      <p className="text-xs text-gray-500">{label}</p>
      <p className={`text-xl font-bold mt-1 ${color}`}>{value}</p>
    </div>
  );
}
