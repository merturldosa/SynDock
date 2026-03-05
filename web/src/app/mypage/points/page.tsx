"use client";

import { useEffect, useState } from "react";
import { Coins, ArrowUpCircle, ArrowDownCircle } from "lucide-react";
import { useTranslations } from "next-intl";
import { getPointBalance, getPointHistory, type PointHistoryDto, type PagedPointHistory } from "@/lib/pointApi";
import { formatNumber, formatDateShort } from "@/lib/format";

const TYPE_CONFIG: Record<string, { labelKey: string; color: string; icon: typeof ArrowUpCircle }> = {
  Earned: { labelKey: "mypage.points.earned", color: "text-emerald-600", icon: ArrowUpCircle },
  Used: { labelKey: "mypage.points.used", color: "text-red-500", icon: ArrowDownCircle },
  Refund: { labelKey: "mypage.points.refund", color: "text-blue-500", icon: ArrowUpCircle },
  Bonus: { labelKey: "mypage.points.bonus", color: "text-orange-500", icon: ArrowUpCircle },
  Expired: { labelKey: "mypage.points.expired", color: "text-gray-400", icon: ArrowDownCircle },
};

export default function PointsPage() {
  const t = useTranslations();
  const [balance, setBalance] = useState(0);
  const [history, setHistory] = useState<PagedPointHistory | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      getPointBalance(),
      getPointHistory(page),
    ])
      .then(([bal, hist]) => {
        setBalance(bal.balance);
        setHistory(hist);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [page]);

  if (loading) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-8">
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("mypage.points.title")}
      </h1>

      {/* Balance Card */}
      <div className="bg-gradient-to-r from-[var(--color-secondary)] to-[var(--color-secondary-light)] rounded-xl p-6 mb-6 text-white">
        <div className="flex items-center gap-3">
          <Coins size={32} className="text-[var(--color-primary)]" />
          <div>
            <p className="text-sm text-white/60">{t("mypage.points.balance")}</p>
            <p className="text-3xl font-bold">{formatNumber(balance)}P</p>
          </div>
        </div>
      </div>

      {/* History */}
      <h2 className="font-semibold text-[var(--color-secondary)] mb-3">
        {t("mypage.points.history")}
      </h2>

      {!history || history.items.length === 0 ? (
        <div className="text-center py-12 text-gray-400">
          <p>{t("mypage.points.empty")}</p>
        </div>
      ) : (
        <>
          <div className="space-y-2">
            {history.items.map((item) => {
              const typeInfo = TYPE_CONFIG[item.transactionType] || TYPE_CONFIG.Earned;
              const Icon = typeInfo.icon;
              return (
                <div
                  key={item.id}
                  className="bg-white rounded-xl shadow-sm p-4 flex items-center gap-3"
                >
                  <Icon size={20} className={typeInfo.color} />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-900">
                      {item.description || t(typeInfo.labelKey)}
                    </p>
                    <p className="text-xs text-gray-400">
                      {formatDateShort(item.createdAt)}
                    </p>
                  </div>
                  <p className={`font-bold ${item.amount >= 0 ? "text-emerald-600" : "text-red-500"}`}>
                    {item.amount >= 0 ? "+" : ""}{formatNumber(item.amount)}P
                  </p>
                </div>
              );
            })}
          </div>

          {history.totalCount > 20 && (
            <div className="flex items-center justify-center gap-2 mt-6">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
              >
                {t("mypage.points.prev")}
              </button>
              <span className="text-sm text-gray-500">
                {page} / {Math.ceil(history.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page * 20 >= history.totalCount}
                className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
              >
                {t("mypage.points.next")}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
