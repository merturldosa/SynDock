"use client";

import { useEffect, useState } from "react";
import { BarChart2, TrendingUp, TrendingDown, Minus, RefreshCw, Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  getAllAccuracies,
  updateAccuracy,
  type ForecastAccuracyResult,
} from "@/lib/forecastApi";

const mapeColor = (mape: number) => {
  if (mape < 15) return "text-green-700 bg-green-100";
  if (mape < 30) return "text-yellow-700 bg-yellow-100";
  if (mape < 50) return "text-orange-700 bg-orange-100";
  return "text-red-700 bg-red-100";
};

const mapeBarWidth = (mape: number) => {
  return `${Math.min(100, mape)}%`;
};

const mapeBarColor = (mape: number) => {
  if (mape < 15) return "bg-green-400";
  if (mape < 30) return "bg-yellow-400";
  if (mape < 50) return "bg-orange-400";
  return "bg-red-400";
};

export default function ForecastAccuracyPanel() {
  const t = useTranslations();
  const [accuracies, setAccuracies] = useState<ForecastAccuracyResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await getAllAccuracies();
      setAccuracies(data);
    } catch {
      setAccuracies([]);
    }
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleUpdate = async () => {
    setUpdating(true);
    try {
      await updateAccuracy();
      await loadData();
    } catch {
      // ignore
    }
    setUpdating(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  const overallMape =
    accuracies.length > 0
      ? accuracies.reduce((sum, a) => sum + a.mape, 0) / accuracies.length
      : 0;

  const bestAccuracy = accuracies.length > 0 ? accuracies[0] : null;
  const worstAccuracy =
    accuracies.length > 0 ? accuracies[accuracies.length - 1] : null;

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-xl shadow-sm p-5 text-center">
          <p className="text-xs text-gray-500 mb-1">
            {t("admin.forecast.accuracy.overallMape")}
          </p>
          <p
            className={`text-2xl font-bold ${
              overallMape < 15
                ? "text-green-600"
                : overallMape < 30
                  ? "text-yellow-600"
                  : "text-red-600"
            }`}
          >
            {overallMape.toFixed(1)}%
          </p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 text-center">
          <p className="text-xs text-gray-500 mb-1">
            {t("admin.forecast.accuracy.trackedProducts")}
          </p>
          <p className="text-2xl font-bold text-[var(--color-primary)]">
            {accuracies.length}
          </p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 text-center">
          <p className="text-xs text-gray-500 mb-1">
            {t("admin.forecast.accuracy.bestAccuracy")}
          </p>
          <p className="text-sm font-medium text-green-600 truncate">
            {bestAccuracy
              ? `${bestAccuracy.productName} (${bestAccuracy.mape.toFixed(1)}%)`
              : "-"}
          </p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-5 text-center">
          <p className="text-xs text-gray-500 mb-1">
            {t("admin.forecast.accuracy.worstAccuracy")}
          </p>
          <p className="text-sm font-medium text-red-600 truncate">
            {worstAccuracy
              ? `${worstAccuracy.productName} (${worstAccuracy.mape.toFixed(1)}%)`
              : "-"}
          </p>
        </div>
      </div>

      {/* Products Table */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <BarChart2 size={18} className="text-blue-500" />
            <h3 className="font-semibold text-[var(--color-secondary)]">
              {t("admin.forecast.accuracy.title")}
            </h3>
          </div>
          <button
            onClick={handleUpdate}
            disabled={updating}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90 disabled:opacity-60"
          >
            {updating ? (
              <Loader2 size={14} className="animate-spin" />
            ) : (
              <RefreshCw size={14} />
            )}
            {t("admin.forecast.accuracy.refresh")}
          </button>
        </div>

        {accuracies.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <BarChart2 size={40} className="mx-auto mb-2 opacity-40" />
            <p>{t("admin.forecast.accuracy.noData")}</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-gray-500">
                  <th className="py-3 pr-4 font-medium">
                    {t("admin.forecast.productCol")}
                  </th>
                  <th className="py-3 pr-4 font-medium text-right">MAPE</th>
                  <th className="py-3 pr-4 font-medium text-right">MAE</th>
                  <th className="py-3 pr-4 font-medium text-right">
                    {t("admin.forecast.accuracy.dataPoints")}
                  </th>
                  <th className="py-3 font-medium w-40">
                    {t("admin.forecast.accuracy.distribution")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {accuracies.map((a) => (
                  <tr key={a.productId} className="border-b last:border-0">
                    <td className="py-3 pr-4">
                      <div className="font-medium text-gray-900">
                        {a.productName}
                      </div>
                      <div className="text-xs text-gray-400">
                        #{a.productId}
                      </div>
                    </td>
                    <td className="py-3 pr-4 text-right">
                      <span
                        className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${mapeColor(a.mape)}`}
                      >
                        {a.mape.toFixed(1)}%
                      </span>
                    </td>
                    <td className="py-3 pr-4 text-right font-mono text-gray-600">
                      {a.mae.toFixed(1)}
                    </td>
                    <td className="py-3 pr-4 text-right text-gray-600">
                      {a.forecastCount}
                    </td>
                    <td className="py-3">
                      <div className="w-full bg-gray-100 rounded-full h-2">
                        <div
                          className={`h-2 rounded-full ${mapeBarColor(a.mape)}`}
                          style={{ width: mapeBarWidth(a.mape) }}
                        />
                      </div>
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
