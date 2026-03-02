"use client";

import { useState } from "react";
import {
  Sparkles,
  Search,
  TrendingUp,
  Calendar,
  Lightbulb,
  Star,
  Loader2,
} from "lucide-react";
import { useTranslations } from "next-intl";
import { getProductForecastWithAi, type ForecastResult } from "@/lib/forecastApi";
import DemandChart from "./DemandChart";

export default function AiInsightPanel() {
  const t = useTranslations();
  const [searchId, setSearchId] = useState("");
  const [forecast, setForecast] = useState<ForecastResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSearch = async () => {
    const id = Number(searchId);
    if (!id) return;
    setLoading(true);
    setError("");
    try {
      const result = await getProductForecastWithAi(id, 30);
      setForecast(result);
    } catch {
      setError(t("admin.forecast.productNotFound"));
      setForecast(null);
    }
    setLoading(false);
  };

  const insight = forecast?.aiInsight;

  return (
    <div className="space-y-6">
      {/* Search */}
      <div className="flex items-center gap-2">
        <input
          type="number"
          value={searchId}
          onChange={(e) => setSearchId(e.target.value)}
          placeholder={t("admin.forecast.productId")}
          className="w-32 px-3 py-2 border rounded-lg text-sm"
          onKeyDown={(e) => e.key === "Enter" && handleSearch()}
        />
        <button
          onClick={handleSearch}
          disabled={loading}
          className="flex items-center gap-1.5 px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90 disabled:opacity-60"
        >
          {loading ? (
            <Loader2 size={14} className="animate-spin" />
          ) : (
            <Search size={14} />
          )}
          {loading
            ? t("admin.forecast.ai.loading")
            : t("admin.forecast.search")}
        </button>
      </div>

      {error && <p className="text-red-500 text-sm">{error}</p>}

      {forecast && (
        <div className="space-y-4">
          {/* Product info */}
          <div className="bg-white rounded-xl shadow-sm p-6">
            <h3 className="font-semibold text-[var(--color-secondary)] mb-4">
              {forecast.productName} (#{forecast.productId})
            </h3>

            <div className="grid grid-cols-3 gap-4 mb-4">
              <div className="bg-gray-50 rounded-lg p-3 text-center">
                <p className="text-xs text-gray-500">
                  {t("admin.forecast.currentStock")}
                </p>
                <p className="text-xl font-bold text-[var(--color-secondary)]">
                  {forecast.currentStock.toLocaleString()}
                </p>
              </div>
              <div className="bg-gray-50 rounded-lg p-3 text-center">
                <p className="text-xs text-gray-500">
                  {t("admin.forecast.dailyDemand")}
                </p>
                <p className="text-xl font-bold text-[var(--color-primary)]">
                  {forecast.averageDailyDemand}
                </p>
              </div>
              <div
                className={`rounded-lg p-3 text-center ${
                  forecast.estimatedDaysUntilStockout <= 14
                    ? "bg-red-50"
                    : "bg-green-50"
                }`}
              >
                <p className="text-xs text-gray-500">
                  {t("admin.forecast.stockoutEstimate")}
                </p>
                <p
                  className={`text-xl font-bold ${
                    forecast.estimatedDaysUntilStockout <= 14
                      ? "text-red-600"
                      : "text-green-600"
                  }`}
                >
                  {forecast.estimatedDaysUntilStockout >= 9999
                    ? t("admin.forecast.sufficient")
                    : t("admin.forecast.daysUntil", {
                        days: forecast.estimatedDaysUntilStockout,
                      })}
                </p>
              </div>
            </div>

            <DemandChart forecast={forecast} />
          </div>

          {/* AI Insight */}
          {insight ? (
            <div className="bg-gradient-to-br from-indigo-50 to-purple-50 rounded-xl shadow-sm p-6">
              <div className="flex items-center gap-2 mb-4">
                <Sparkles size={18} className="text-indigo-600" />
                <h3 className="font-semibold text-indigo-900">
                  AI {t("admin.forecast.ai.trendAnalysis")}
                </h3>
                <div className="ml-auto flex items-center gap-1">
                  <Star
                    size={14}
                    className="text-yellow-500"
                    fill="currentColor"
                  />
                  <span className="text-sm text-gray-600">
                    {t("admin.forecast.ai.confidence")}:{" "}
                    {Math.round(insight.confidenceScore * 100)}%
                  </span>
                </div>
              </div>

              <div className="space-y-4">
                {/* Trend */}
                <div className="flex gap-3">
                  <TrendingUp
                    size={16}
                    className="text-indigo-500 mt-0.5 shrink-0"
                  />
                  <div>
                    <p className="text-sm font-medium text-indigo-800 mb-1">
                      {t("admin.forecast.ai.trendAnalysis")}
                    </p>
                    <p className="text-sm text-gray-700">
                      {insight.trendAnalysis}
                    </p>
                  </div>
                </div>

                {/* Seasonal */}
                <div className="flex gap-3">
                  <Calendar
                    size={16}
                    className="text-indigo-500 mt-0.5 shrink-0"
                  />
                  <div>
                    <p className="text-sm font-medium text-indigo-800 mb-1">
                      {t("admin.forecast.ai.seasonalPatterns")}
                    </p>
                    <p className="text-sm text-gray-700">
                      {insight.seasonalPatterns}
                    </p>
                  </div>
                </div>

                {/* Event Impact */}
                {insight.eventImpact && (
                  <div className="flex gap-3">
                    <Calendar
                      size={16}
                      className="text-orange-500 mt-0.5 shrink-0"
                    />
                    <div>
                      <p className="text-sm font-medium text-orange-800 mb-1">
                        {t("admin.forecast.ai.eventImpact")}
                      </p>
                      <p className="text-sm text-gray-700">
                        {insight.eventImpact}
                      </p>
                    </div>
                  </div>
                )}

                {/* Recommendations */}
                {insight.recommendations.length > 0 && (
                  <div className="flex gap-3">
                    <Lightbulb
                      size={16}
                      className="text-yellow-500 mt-0.5 shrink-0"
                    />
                    <div>
                      <p className="text-sm font-medium text-indigo-800 mb-1">
                        {t("admin.forecast.ai.recommendations")}
                      </p>
                      <ul className="list-disc list-inside text-sm text-gray-700 space-y-1">
                        {insight.recommendations.map((r, i) => (
                          <li key={i}>{r}</li>
                        ))}
                      </ul>
                    </div>
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-gray-400 bg-gray-50 rounded-xl">
              <Sparkles size={32} className="mx-auto mb-2 opacity-40" />
              <p>{t("admin.forecast.ai.unavailable")}</p>
            </div>
          )}
        </div>
      )}

      {!forecast && !loading && !error && (
        <div className="text-center py-16 text-gray-400">
          <Sparkles size={48} className="mx-auto mb-3 opacity-30" />
          <p className="text-lg">
            {t("admin.forecast.ai.searchPrompt")}
          </p>
        </div>
      )}
    </div>
  );
}
