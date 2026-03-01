"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Church, ChevronRight } from "lucide-react";
import { getTodayLiturgy } from "@/lib/liturgyApi";
import type { LiturgyTodayDto } from "@/types/liturgy";

const SEASON_LABELS: Record<string, string> = {
  Advent: "대림 시기",
  Christmas: "성탄 시기",
  OrdinaryTime1: "연중 시기",
  Lent: "사순 시기",
  Triduum: "성삼일",
  Easter: "부활 시기",
  OrdinaryTime2: "연중 시기",
};

const COLOR_BG: Record<string, string> = {
  purple: "bg-purple-600",
  white: "bg-amber-500",
  green: "bg-green-600",
  red: "bg-red-600",
};

export function LiturgySummary() {
  const [data, setData] = useState<LiturgyTodayDto | null>(null);

  useEffect(() => {
    getTodayLiturgy().then(setData).catch(() => {});
  }, []);

  if (!data) return null;

  return (
    <section className="max-w-7xl mx-auto px-4 py-8">
      <Link
        href="/liturgy"
        className="block bg-white rounded-2xl shadow-sm hover:shadow-md transition-shadow overflow-hidden"
      >
        <div className="flex items-stretch">
          <div className={`w-2 ${COLOR_BG[data.currentSeason.liturgicalColor] || "bg-green-600"}`} />
          <div className="flex-1 p-6 flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className={`w-12 h-12 rounded-full flex items-center justify-center ${COLOR_BG[data.currentSeason.liturgicalColor] || "bg-green-600"} text-white`}>
                <Church size={22} />
              </div>
              <div>
                <p className="text-xs text-gray-400 mb-0.5">오늘의 전례</p>
                <h3 className="font-bold text-[var(--color-secondary)]">
                  {SEASON_LABELS[data.currentSeason.seasonName] || data.currentSeason.seasonName}
                </h3>
                {data.todaySaints.length > 0 && (
                  <p className="text-sm text-[var(--color-primary)]">
                    축일: {data.todaySaints.map(s => s.koreanName).join(", ")}
                  </p>
                )}
              </div>
            </div>
            <ChevronRight size={20} className="text-gray-400" />
          </div>
        </div>
      </Link>
    </section>
  );
}
