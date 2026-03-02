"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Calendar, Church, Star, ChevronRight } from "lucide-react";
import { useTranslations } from "next-intl";
import { getTodayLiturgy, getLiturgicalSeasons } from "@/lib/liturgyApi";
import type { LiturgyTodayDto, LiturgicalSeasonDto } from "@/types/liturgy";

const SEASON_KEYS = [
  "Advent",
  "Christmas",
  "OrdinaryTime1",
  "Lent",
  "Triduum",
  "Easter",
  "OrdinaryTime2",
] as const;

const COLOR_MAP: Record<string, { bg: string; text: string; border: string; accent: string }> = {
  purple: { bg: "bg-purple-50", text: "text-purple-900", border: "border-purple-300", accent: "bg-purple-600" },
  white: { bg: "bg-amber-50", text: "text-amber-900", border: "border-amber-300", accent: "bg-amber-500" },
  green: { bg: "bg-green-50", text: "text-green-900", border: "border-green-300", accent: "bg-green-600" },
  red: { bg: "bg-red-50", text: "text-red-900", border: "border-red-300", accent: "bg-red-600" },
};

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString("ko-KR", {
    month: "long",
    day: "numeric",
  });
}

export default function LiturgyPage() {
  const t = useTranslations();
  const [today, setToday] = useState<LiturgyTodayDto | null>(null);
  const [seasons, setSeasons] = useState<LiturgicalSeasonDto[]>([]);
  const [loading, setLoading] = useState(true);
  const currentYear = new Date().getFullYear();

  useEffect(() => {
    Promise.all([getTodayLiturgy(), getLiturgicalSeasons(currentYear)])
      .then(([todayData, seasonsData]) => {
        setToday(todayData);
        setSeasons(seasonsData);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [currentYear]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  const colorStyle = today
    ? COLOR_MAP[today.currentSeason.liturgicalColor] || COLOR_MAP.green
    : COLOR_MAP.green;

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Current Season Banner */}
      {today && (
        <div className={`rounded-2xl p-8 mb-8 border ${colorStyle.bg} ${colorStyle.border}`}>
          <div className="flex items-center gap-2 mb-2">
            <div className={`w-3 h-3 rounded-full ${colorStyle.accent}`} />
            <span className={`text-sm font-medium ${colorStyle.text} opacity-70`}>
              {t("liturgy.today")}
            </span>
          </div>
          <h1 className={`text-3xl font-bold mb-2 ${colorStyle.text}`}>
            {t(`liturgy.season.${today.currentSeason.seasonName}`)}
          </h1>
          <p className={`${colorStyle.text} opacity-70`}>
            {formatDate(today.currentSeason.startDate)} ~ {formatDate(today.currentSeason.endDate)}
          </p>
          <div className="mt-4 flex items-center gap-2">
            <span className={`text-sm ${colorStyle.text} opacity-60`}>{t("liturgy.liturgicalColor")}:</span>
            <div className={`w-6 h-6 rounded-full border-2 border-white shadow ${colorStyle.accent}`} />
            <span className={`text-sm font-medium ${colorStyle.text}`}>
              {t(`liturgy.color.${today.currentSeason.liturgicalColor}`)}
            </span>
          </div>
        </div>
      )}

      {/* Today's Saints */}
      {today && today.todaySaints.length > 0 && (
        <div className="bg-white rounded-2xl shadow-sm p-6 mb-8">
          <h2 className="text-xl font-bold text-[var(--color-secondary)] mb-4 flex items-center gap-2">
            <Star size={20} className="text-[var(--color-primary)]" />
            {t("liturgy.todaySaints")}
          </h2>
          <div className="space-y-3">
            {today.todaySaints.map((saint) => (
              <Link
                key={saint.id}
                href={`/saints/${saint.id}`}
                className="flex items-center justify-between p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
              >
                <div>
                  <h3 className="font-semibold text-[var(--color-secondary)]">
                    {saint.koreanName}
                  </h3>
                  {saint.latinName && (
                    <p className="text-sm text-gray-500 italic">{saint.latinName}</p>
                  )}
                  {saint.patronage && (
                    <p className="text-xs text-gray-400 mt-1">{t("liturgy.patronage")}: {saint.patronage}</p>
                  )}
                </div>
                <ChevronRight size={18} className="text-gray-400" />
              </Link>
            ))}
          </div>
        </div>
      )}

      {today && today.todaySaints.length === 0 && (
        <div className="bg-white rounded-2xl shadow-sm p-6 mb-8 text-center">
          <Church size={32} className="mx-auto mb-2 text-gray-300" />
          <p className="text-gray-400 text-sm">{t("liturgy.noSaintsToday")}</p>
        </div>
      )}

      {/* Annual Liturgical Calendar */}
      <div className="bg-white rounded-2xl shadow-sm p-6">
        <h2 className="text-xl font-bold text-[var(--color-secondary)] mb-4 flex items-center gap-2">
          <Calendar size={20} className="text-[var(--color-primary)]" />
          {t("liturgy.calendarYear", { year: currentYear })}
        </h2>
        <div className="space-y-3">
          {seasons.map((season, idx) => {
            const sColor = COLOR_MAP[season.liturgicalColor] || COLOR_MAP.green;
            const isCurrent =
              today && season.seasonName === today.currentSeason.seasonName;
            return (
              <div
                key={idx}
                className={`flex items-center gap-4 p-4 rounded-xl border transition-colors ${
                  isCurrent
                    ? `${sColor.bg} ${sColor.border} border-2`
                    : "border-gray-100 hover:bg-gray-50"
                }`}
              >
                <div className={`w-3 h-3 rounded-full flex-shrink-0 ${sColor.accent}`} />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className={`font-medium ${isCurrent ? sColor.text : "text-[var(--color-secondary)]"}`}>
                      {t(`liturgy.season.${season.seasonName}`)}
                    </span>
                    {isCurrent && (
                      <span className="text-xs px-2 py-0.5 bg-[var(--color-primary)] text-white rounded-full">
                        {t("liturgy.current")}
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-gray-500">
                    {formatDate(season.startDate)} ~ {formatDate(season.endDate)}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
