"use client";

import { useEffect, useState } from "react";
import { ShoppingCart, AlertCircle, Loader2, Send } from "lucide-react";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import {
  getPurchaseRecommendations,
  createAutoPurchaseOrder,
  type PurchaseRecommendation,
} from "@/lib/forecastApi";

const urgencyColors: Record<string, string> = {
  Critical: "text-red-700 bg-red-100",
  High: "text-orange-700 bg-orange-100",
  Medium: "text-yellow-700 bg-yellow-100",
  Low: "text-green-700 bg-green-100",
};

export default function PurchaseRecommendations() {
  const t = useTranslations();
  const [items, setItems] = useState<PurchaseRecommendation[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());
  const [showConfirm, setShowConfirm] = useState(false);
  const [ordering, setOrdering] = useState(false);
  const [orderResult, setOrderResult] = useState<string | null>(null);

  useEffect(() => {
    getPurchaseRecommendations(14)
      .then(setItems)
      .catch(() => { toast.error(t("common.fetchError")); })
      .finally(() => setLoading(false));
  }, []);

  const toggleSelect = (productId: number) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(productId)) next.delete(productId);
      else next.add(productId);
      return next;
    });
  };

  const toggleAll = () => {
    if (selectedIds.size === items.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(items.map((i) => i.productId)));
    }
  };

  const handleAutoOrder = async () => {
    setOrdering(true);
    setOrderResult(null);
    try {
      const result = await createAutoPurchaseOrder(Array.from(selectedIds));
      if (result.success) {
        setOrderResult(
          t("admin.forecast.purchase.orderSuccess", {
            count: result.productCount,
            qty: result.totalQuantity,
          })
        );
        setSelectedIds(new Set());
      } else {
        setOrderResult(result.errorMessage || t("admin.forecast.purchase.orderFailed"));
      }
    } catch {
      setOrderResult(t("admin.forecast.purchase.orderFailed"));
    }
    setOrdering(false);
    setShowConfirm(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="text-center py-12 text-gray-400">
        <ShoppingCart size={40} className="mx-auto mb-2 opacity-40" />
        <p>{t("admin.forecast.purchase.noRecommendations")}</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {/* Action bar */}
      <div className="flex items-center justify-between">
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
          <input
            type="checkbox"
            checked={selectedIds.size === items.length && items.length > 0}
            onChange={toggleAll}
            className="rounded border-gray-300"
          />
          {selectedIds.size > 0
            ? t("admin.forecast.purchase.nSelected", { count: selectedIds.size })
            : t("admin.forecast.purchase.selectAll")}
        </label>
        {selectedIds.size > 0 && (
          <button
            onClick={() => setShowConfirm(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90"
          >
            <Send size={14} />
            {t("admin.forecast.purchase.autoOrder")}
          </button>
        )}
      </div>

      {/* Order result toast */}
      {orderResult && (
        <div
          className={`rounded-lg p-3 text-sm ${
            orderResult.includes("성공") || orderResult.includes("Success")
              ? "bg-green-50 text-green-700"
              : "bg-red-50 text-red-700"
          }`}
        >
          {orderResult}
        </div>
      )}

      {/* Confirm modal */}
      {showConfirm && (
        <div className="fixed inset-0 bg-black/30 z-50 flex items-center justify-center">
          <div className="bg-white rounded-xl shadow-xl p-6 max-w-md w-full mx-4">
            <h3 className="font-semibold text-lg mb-2">
              {t("admin.forecast.purchase.confirmTitle")}
            </h3>
            <p className="text-sm text-gray-600 mb-4">
              {t("admin.forecast.purchase.confirmDesc", { count: selectedIds.size })}
            </p>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => setShowConfirm(false)}
                className="px-4 py-2 text-sm rounded-lg border hover:bg-gray-50"
                disabled={ordering}
              >
                {t("admin.forecast.purchase.cancel")}
              </button>
              <button
                onClick={handleAutoOrder}
                disabled={ordering}
                className="flex items-center gap-1.5 px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90 disabled:opacity-60"
              >
                {ordering && <Loader2 size={14} className="animate-spin" />}
                {ordering
                  ? t("admin.forecast.purchase.ordering")
                  : t("admin.forecast.purchase.confirmOrder")}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Recommendation cards */}
      {items.map((r) => (
        <div
          key={r.productId}
          className="bg-white rounded-lg border p-4 flex items-center justify-between"
        >
          <div className="flex items-center gap-3 flex-1">
            <input
              type="checkbox"
              checked={selectedIds.has(r.productId)}
              onChange={() => toggleSelect(r.productId)}
              className="rounded border-gray-300"
            />
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1">
                <span className="font-medium text-gray-900">
                  {r.productName}
                </span>
                {r.categoryName && (
                  <span className="text-xs text-gray-400">
                    {r.categoryName}
                  </span>
                )}
              </div>
              <p className="text-sm text-gray-500">{r.reason}</p>
            </div>
          </div>
          <div className="flex items-center gap-4 ml-4">
            <div className="text-right">
              <p className="text-xs text-gray-400">
                {t("admin.forecast.purchase.recommendedQty")}
              </p>
              <p className="text-lg font-bold text-[var(--color-primary)]">
                {r.recommendedOrderQuantity.toLocaleString()}
              </p>
            </div>
            <span
              className={`px-2 py-1 rounded-full text-xs font-medium ${
                urgencyColors[r.urgency] || "text-gray-700 bg-gray-100"
              }`}
            >
              {t(`admin.forecast.purchase.${r.urgency.toLowerCase()}`)}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}
