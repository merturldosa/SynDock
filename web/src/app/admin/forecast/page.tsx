"use client";

import { useEffect, useState } from "react";
import {
  TrendingUp,
  AlertTriangle,
  Package,
  Layers,
  Sparkles,
  Server,
  BarChart2,
} from "lucide-react";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import { getLowStockForecasts, type ForecastResult } from "@/lib/forecastApi";
import DemandChart from "./_components/DemandChart";
import PurchaseRecommendations from "./_components/PurchaseRecommendations";
import CategoryForecastGrid from "./_components/CategoryForecastGrid";
import AiInsightPanel from "./_components/AiInsightPanel";
import MesStatusPanel from "./_components/MesStatusPanel";
import ForecastAccuracyPanel from "./_components/ForecastAccuracyPanel";

type Tab = "overview" | "categories" | "aiInsights" | "mesIntegration" | "accuracy";

export default function ForecastPage() {
  const t = useTranslations();
  const [activeTab, setActiveTab] = useState<Tab>("overview");
  const [lowStock, setLowStock] = useState<ForecastResult[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getLowStockForecasts(30)
      .then(setLowStock)
      .catch(() => { toast.error(t("common.fetchError")); })
      .finally(() => setLoading(false));
  }, []);

  const stockoutColor = (days: number) => {
    if (days <= 7) return "text-red-600 bg-red-50";
    if (days <= 14) return "text-orange-600 bg-orange-50";
    return "text-yellow-600 bg-yellow-50";
  };

  const tabs: { key: Tab; icon: React.ElementType; label: string }[] = [
    {
      key: "overview",
      icon: TrendingUp,
      label: t("admin.forecast.tabs.overview"),
    },
    {
      key: "categories",
      icon: Layers,
      label: t("admin.forecast.tabs.categories"),
    },
    {
      key: "aiInsights",
      icon: Sparkles,
      label: t("admin.forecast.tabs.aiInsights"),
    },
    {
      key: "accuracy",
      icon: BarChart2,
      label: t("admin.forecast.tabs.accuracy"),
    },
    {
      key: "mesIntegration",
      icon: Server,
      label: t("admin.forecast.tabs.mesIntegration"),
    },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
        {t("admin.forecast.title")}
      </h1>

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-100 rounded-xl p-1">
        {tabs.map(({ key, icon: Icon, label }) => (
          <button
            key={key}
            onClick={() => setActiveTab(key)}
            className={`flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium transition-colors flex-1 justify-center ${
              activeTab === key
                ? "bg-white text-[var(--color-primary)] shadow-sm"
                : "text-gray-500 hover:text-gray-700"
            }`}
          >
            <Icon size={16} />
            {label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {activeTab === "overview" && (
        <div className="space-y-6">
          {/* Purchase Recommendations */}
          <div className="bg-white rounded-xl shadow-sm p-6">
            <div className="flex items-center gap-2 mb-4">
              <Package size={18} className="text-blue-500" />
              <h2 className="font-semibold text-[var(--color-secondary)]">
                {t("admin.forecast.purchase.title")}
              </h2>
            </div>
            <PurchaseRecommendations />
          </div>

          {/* Low Stock Alerts */}
          <div className="bg-white rounded-xl shadow-sm p-6">
            <div className="flex items-center gap-2 mb-4">
              <AlertTriangle size={18} className="text-orange-500" />
              <h2 className="font-semibold text-[var(--color-secondary)]">
                {t("admin.forecast.stockoutWarning")}
              </h2>
            </div>

            {loading ? (
              <div className="flex items-center justify-center py-12">
                <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
              </div>
            ) : lowStock.length === 0 ? (
              <div className="text-center py-12 text-gray-400">
                <Package size={40} className="mx-auto mb-2 opacity-40" />
                <p>{t("admin.forecast.noStockoutProducts")}</p>
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
                        {t("admin.forecast.currentStockCol")}
                      </th>
                      <th className="py-3 pr-4 font-medium text-right">
                        {t("admin.forecast.dailyDemandCol")}
                      </th>
                      <th className="py-3 pr-4 font-medium text-right">
                        {t("admin.forecast.stockoutCol")}
                      </th>
                      <th className="py-3 font-medium">
                        {t("admin.forecast.trendCol")}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {lowStock.map((f) => (
                      <tr key={f.productId} className="border-b last:border-0">
                        <td className="py-3 pr-4">
                          <div className="font-medium text-gray-900">
                            {f.productName}
                          </div>
                          <div className="text-xs text-gray-400">
                            #{f.productId}
                          </div>
                        </td>
                        <td className="py-3 pr-4 text-right font-mono">
                          {f.currentStock.toLocaleString()}
                        </td>
                        <td className="py-3 pr-4 text-right font-mono">
                          {f.averageDailyDemand}
                        </td>
                        <td className="py-3 pr-4 text-right">
                          <span
                            className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${stockoutColor(
                              f.estimatedDaysUntilStockout
                            )}`}
                          >
                            <TrendingUp size={12} />
                            {t("admin.forecast.daysUntil", {
                              days: f.estimatedDaysUntilStockout,
                            })}
                          </span>
                        </td>
                        <td className="py-3 w-40">
                          <DemandChart forecast={f} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {activeTab === "categories" && <CategoryForecastGrid />}

      {activeTab === "aiInsights" && <AiInsightPanel />}

      {activeTab === "accuracy" && <ForecastAccuracyPanel />}

      {activeTab === "mesIntegration" && <MesStatusPanel />}
    </div>
  );
}
