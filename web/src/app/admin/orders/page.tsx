"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Search, Download } from "lucide-react";
import {
  getAdminOrdersSearch, updateOrderStatus, bulkUpdateOrderStatus, exportOrders,
  type AdminOrderSummary, type AdminPagedOrders,
} from "@/lib/adminApi";

const STATUS_KEYS = ["Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded"] as const;
const STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  Confirmed: "bg-blue-100 text-blue-700",
  Processing: "bg-indigo-100 text-indigo-700",
  Shipped: "bg-purple-100 text-purple-700",
  Delivered: "bg-emerald-100 text-emerald-700",
  Cancelled: "bg-red-100 text-red-700",
  Refunded: "bg-gray-100 text-gray-700",
};

import { formatPrice } from "@/lib/format";

const STATUS_OPTIONS = ["Pending", "Confirmed", "Processing", "Shipped", "Delivered"];

export default function AdminOrdersPage() {
  const t = useTranslations();
  const [data, setData] = useState<AdminPagedOrders | null>(null);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [loading, setLoading] = useState(true);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [bulkStatus, setBulkStatus] = useState("Confirmed");
  const [bulkProcessing, setBulkProcessing] = useState(false);
  const [exporting, setExporting] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  const STATUS_LABELS: Record<string, string> = {
    Pending: t("admin.orders.statusPending"),
    Confirmed: t("admin.orders.statusConfirmed"),
    Processing: t("admin.orders.statusProcessing"),
    Shipped: t("admin.orders.statusShipped"),
    Delivered: t("admin.orders.statusDelivered"),
    Cancelled: t("admin.orders.statusCancelled"),
    Refunded: t("admin.orders.statusRefunded"),
  };

  const load = () => {
    setLoading(true);
    getAdminOrdersSearch(statusFilter || undefined, page, 20, searchQuery || undefined)
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    setSelectedIds(new Set());
  }, [page, statusFilter, searchQuery]);

  const handleSearchInput = (value: string) => {
    setSearchTerm(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setSearchQuery(value);
      setPage(1);
    }, 300);
  };

  const handleStatusChange = async (orderId: number, newStatus: string) => {
    try {
      await updateOrderStatus(orderId, newStatus);
      load();
    } catch {
      alert(t("admin.orders.updateFailed"));
    }
  };

  const toggleSelect = (id: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleSelectAll = () => {
    if (!data) return;
    if (selectedIds.size === data.items.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(data.items.map((o) => o.id)));
    }
  };

  const handleBulkUpdate = async () => {
    if (selectedIds.size === 0) return;
    if (!confirm(t("admin.orders.bulkConfirm", { count: selectedIds.size, status: STATUS_LABELS[bulkStatus] || bulkStatus })))
      return;

    setBulkProcessing(true);
    try {
      const result = await bulkUpdateOrderStatus(Array.from(selectedIds), bulkStatus);
      if (result.failCount > 0) {
        alert(t("admin.orders.bulkResult", { success: result.successCount, fail: result.failCount }) + "\n" + result.errors.join("\n"));
      }
      setSelectedIds(new Set());
      load();
    } catch {
      alert(t("admin.orders.bulkFailed"));
    }
    setBulkProcessing(false);
  };

  const handleExport = async () => {
    setExporting(true);
    try {
      await exportOrders({
        status: statusFilter || undefined,
        search: searchQuery || undefined,
      });
    } catch {
      alert(t("admin.orders.exportFailed"));
    }
    setExporting(false);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">{t("admin.orders.title")}</h1>
        <button
          onClick={handleExport}
          disabled={exporting}
          className="flex items-center gap-1.5 px-4 py-2 bg-green-600 text-white rounded-lg text-sm font-medium hover:bg-green-700 disabled:opacity-60 transition-colors"
        >
          <Download size={16} />
          {exporting ? t("admin.orders.exporting") : t("admin.orders.csvExport")}
        </button>
      </div>

      {/* Search */}
      <div className="relative mb-4">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => handleSearchInput(e.target.value)}
          placeholder={t("admin.orders.searchPlaceholder")}
          className="w-full pl-9 pr-4 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
        />
      </div>

      {/* Status Filter */}
      <div className="flex gap-2 mb-4 flex-wrap">
        <button
          onClick={() => { setStatusFilter(""); setPage(1); }}
          className={`px-3 py-1.5 text-sm rounded-full border ${
            !statusFilter ? "bg-[var(--color-secondary)] text-white" : "hover:bg-gray-50"
          }`}
        >
          {t("admin.orders.all")}
        </button>
        {STATUS_KEYS.map((key) => (
          <button
            key={key}
            onClick={() => { setStatusFilter(key); setPage(1); }}
            className={`px-3 py-1.5 text-sm rounded-full border ${
              statusFilter === key ? "bg-[var(--color-secondary)] text-white" : "hover:bg-gray-50"
            }`}
          >
            {STATUS_LABELS[key]}
          </button>
        ))}
      </div>

      {/* Bulk Actions */}
      {selectedIds.size > 0 && (
        <div className="flex items-center gap-3 mb-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
          <span className="text-sm text-blue-700 font-medium">
            {t("admin.orders.nSelected", { count: selectedIds.size })}
          </span>
          <select
            value={bulkStatus}
            onChange={(e) => setBulkStatus(e.target.value)}
            className="text-sm border rounded px-2 py-1"
          >
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {STATUS_LABELS[s] || s}
              </option>
            ))}
          </select>
          <button
            onClick={handleBulkUpdate}
            disabled={bulkProcessing}
            className="px-4 py-1.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
          >
            {bulkProcessing ? t("admin.orders.bulkProcessing") : t("admin.orders.bulkChange")}
          </button>
          <button
            onClick={() => setSelectedIds(new Set())}
            className="px-3 py-1.5 border rounded-lg text-sm hover:bg-gray-50"
          >
            {t("admin.orders.deselectAll")}
          </button>
        </div>
      )}

      {data && (
        <p className="text-sm text-gray-500 mb-3">
          {t("admin.orders.totalCount", { count: data.totalCount.toLocaleString() })}
        </p>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>{searchQuery ? t("admin.orders.searchNoResult", { query: searchQuery }) : t("admin.orders.noOrders")}</p>
        </div>
      ) : (
        <>
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="p-3 w-10">
                    <input
                      type="checkbox"
                      checked={selectedIds.size === data.items.length && data.items.length > 0}
                      onChange={toggleSelectAll}
                      className="rounded"
                    />
                  </th>
                  <th className="text-left p-3 font-medium text-gray-500">{t("admin.orders.orderNumber")}</th>
                  <th className="text-left p-3 font-medium text-gray-500">{t("admin.orders.customer")}</th>
                  <th className="text-left p-3 font-medium text-gray-500">{t("admin.orders.items")}</th>
                  <th className="text-right p-3 font-medium text-gray-500">{t("admin.orders.amount")}</th>
                  <th className="text-center p-3 font-medium text-gray-500">{t("admin.orders.status")}</th>
                  <th className="text-left p-3 font-medium text-gray-500">{t("admin.orders.orderDate")}</th>
                  <th className="text-center p-3 font-medium text-gray-500">{t("admin.orders.statusChange")}</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((order) => {
                  const statusColor = STATUS_COLORS[order.status] || "bg-gray-100 text-gray-700";
                  const statusLabel = STATUS_LABELS[order.status] || order.status;
                  return (
                    <tr key={order.id} className={`border-b last:border-0 hover:bg-gray-50 ${selectedIds.has(order.id) ? "bg-blue-50" : ""}`}>
                      <td className="p-3">
                        <input
                          type="checkbox"
                          checked={selectedIds.has(order.id)}
                          onChange={() => toggleSelect(order.id)}
                          className="rounded"
                        />
                      </td>
                      <td className="p-3">
                        <Link href={`/admin/orders/${order.id}`} className="text-[var(--color-primary)] hover:underline font-mono text-xs">
                          {order.orderNumber}
                        </Link>
                      </td>
                      <td className="p-3">
                        <p className="text-sm font-medium truncate max-w-[120px]">{order.customerName || "-"}</p>
                        <p className="text-xs text-gray-400 truncate max-w-[120px]">{order.customerEmail || ""}</p>
                      </td>
                      <td className="p-3">
                        <p className="line-clamp-1">
                          {order.firstProductName || t("admin.orders.items")}
                          {order.itemCount > 1 && ` ${t("admin.orders.andMore", { count: order.itemCount - 1 })}`}
                        </p>
                      </td>
                      <td className="p-3 text-right font-medium">{formatPrice(order.totalAmount)}</td>
                      <td className="p-3 text-center">
                        <span className={`px-2 py-0.5 text-xs rounded-full ${statusColor}`}>
                          {statusLabel}
                        </span>
                      </td>
                      <td className="p-3 text-gray-500 text-xs">
                        {new Date(order.createdAt).toLocaleDateString("ko-KR")}
                      </td>
                      <td className="p-3 text-center">
                        <select
                          value={order.status}
                          onChange={(e) => handleStatusChange(order.id, e.target.value)}
                          className="text-xs border rounded px-2 py-1"
                        >
                          {STATUS_OPTIONS.map((s) => (
                            <option key={s} value={s}>
                              {STATUS_LABELS[s] || s}
                            </option>
                          ))}
                        </select>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {data.totalCount > 20 && (
            <div className="flex items-center justify-center gap-2 mt-6">
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">
                {t("admin.orders.prev")}
              </button>
              <span className="text-sm text-gray-500">{page} / {Math.ceil(data.totalCount / 20)}</span>
              <button onClick={() => setPage((p) => p + 1)} disabled={page * 20 >= data.totalCount} className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">
                {t("admin.orders.next")}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
