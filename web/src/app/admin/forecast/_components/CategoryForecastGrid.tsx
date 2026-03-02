"use client";

import { useEffect, useState } from "react";
import { Layers, Package } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  getCategoryForecasts,
  type CategoryForecastResult,
} from "@/lib/forecastApi";

export default function CategoryForecastGrid() {
  const t = useTranslations();
  const [categories, setCategories] = useState<CategoryForecastResult[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getCategoryForecasts(30)
      .then(setCategories)
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

  if (categories.length === 0) {
    return (
      <div className="text-center py-12 text-gray-400">
        <Layers size={40} className="mx-auto mb-2 opacity-40" />
        <p>{t("admin.forecast.category.noCategories")}</p>
      </div>
    );
  }

  const stockoutColor = (days: number) => {
    if (days <= 7) return "text-red-600 bg-red-50 border-red-200";
    if (days <= 14) return "text-orange-600 bg-orange-50 border-orange-200";
    if (days <= 30) return "text-yellow-600 bg-yellow-50 border-yellow-200";
    return "text-green-600 bg-green-50 border-green-200";
  };

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {categories.map((cat) => (
        <div
          key={cat.categoryId}
          className={`rounded-xl border p-5 ${stockoutColor(cat.minDaysUntilStockout)}`}
        >
          <div className="flex items-center gap-2 mb-3">
            <Layers size={18} />
            <h3 className="font-semibold">{cat.categoryName}</h3>
          </div>

          <div className="grid grid-cols-2 gap-3 mb-3 text-sm">
            <div>
              <p className="text-xs opacity-70">
                {t("admin.forecast.category.productCount")}
              </p>
              <p className="font-bold">{cat.productCount}</p>
            </div>
            <div>
              <p className="text-xs opacity-70">
                {t("admin.forecast.category.totalStock")}
              </p>
              <p className="font-bold">{cat.totalStock.toLocaleString()}</p>
            </div>
            <div>
              <p className="text-xs opacity-70">
                {t("admin.forecast.dailyDemand")}
              </p>
              <p className="font-bold">{cat.totalAverageDailyDemand}</p>
            </div>
            <div>
              <p className="text-xs opacity-70">
                {t("admin.forecast.stockoutEstimate")}
              </p>
              <p className="font-bold">
                {cat.minDaysUntilStockout >= 9999
                  ? t("admin.forecast.sufficient")
                  : t("admin.forecast.daysUntil", {
                      days: cat.minDaysUntilStockout,
                    })}
              </p>
            </div>
          </div>

          {cat.topProducts.length > 0 && (
            <div className="border-t border-current/10 pt-2">
              <p className="text-xs opacity-70 mb-1">
                {t("admin.forecast.category.topProducts")}
              </p>
              {cat.topProducts.slice(0, 3).map((p) => (
                <div
                  key={p.productId}
                  className="flex items-center justify-between text-xs py-0.5"
                >
                  <span className="truncate flex-1">{p.productName}</span>
                  <span className="font-mono ml-2">
                    {p.estimatedDaysUntilStockout >= 9999
                      ? "-"
                      : `${p.estimatedDaysUntilStockout}d`}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}
