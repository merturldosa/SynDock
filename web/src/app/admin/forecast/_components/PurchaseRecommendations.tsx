"use client";

import { useEffect, useState } from "react";
import { ShoppingCart, AlertCircle } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  getPurchaseRecommendations,
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

  useEffect(() => {
    getPurchaseRecommendations(14)
      .then(setItems)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

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
      {items.map((r) => (
        <div
          key={r.productId}
          className="bg-white rounded-lg border p-4 flex items-center justify-between"
        >
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
