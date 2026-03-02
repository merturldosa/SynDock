"use client";

import { useTranslations } from "next-intl";
import type { ForecastResult } from "@/lib/forecastApi";

interface Props {
  forecast: ForecastResult;
  height?: string;
}

export default function DemandChart({ forecast, height = "h-24" }: Props) {
  const t = useTranslations();
  const allDemand = [
    ...forecast.historicalDemand.slice(-14),
    ...forecast.forecastedDemand.slice(0, 14),
  ];
  const maxQty = Math.max(...allDemand.map((d) => d.quantity), 1);
  const historicalCount = forecast.historicalDemand.slice(-14).length;

  return (
    <div className={`flex items-end gap-[2px] ${height}`}>
      {allDemand.map((d, i) => {
        const pct = (d.quantity / maxQty) * 100;
        const isForecast = i >= historicalCount;
        return (
          <div
            key={i}
            className={`flex-1 rounded-t ${
              isForecast
                ? "bg-blue-300 opacity-70"
                : "bg-[var(--color-primary)]"
            }`}
            style={{ height: `${Math.max(pct, 2)}%` }}
            title={`${d.date.slice(5, 10)}: ${d.quantity}${t("common.items", { count: "" })} ${
              isForecast ? t("admin.forecast.forecastUnit") : ""
            }`}
          />
        );
      })}
    </div>
  );
}
