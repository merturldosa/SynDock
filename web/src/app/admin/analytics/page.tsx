"use client";

import { useEffect, useState } from "react";
import { getSalesAnalytics, type SalesAnalytics, type DailySales } from "@/lib/adminApi";
import { BarChart3, TrendingUp, ShoppingCart, DollarSign } from "lucide-react";

function formatPrice(n: number) {
  return n.toLocaleString("ko-KR") + "원";
}

function MiniBarChart({ data, maxValue }: { data: DailySales[]; maxValue: number }) {
  if (!data.length) return null;

  return (
    <div className="flex items-end gap-[2px] h-48">
      {data.map((d, i) => {
        const height = maxValue > 0 ? (d.revenue / maxValue) * 100 : 0;
        const day = new Date(d.date);
        const isWeekend = day.getDay() === 0 || day.getDay() === 6;
        return (
          <div key={i} className="flex-1 flex flex-col items-center gap-1 group relative">
            <div
              className={`w-full rounded-t transition-all ${
                isWeekend ? "bg-blue-300 hover:bg-blue-400" : "bg-[var(--color-primary)] hover:opacity-80"
              }`}
              style={{ height: `${Math.max(height, 2)}%` }}
            />
            {/* Tooltip */}
            <div className="absolute bottom-full mb-2 hidden group-hover:block bg-gray-800 text-white text-xs rounded px-2 py-1 whitespace-nowrap z-10">
              <p>{d.date}</p>
              <p>매출: {formatPrice(d.revenue)}</p>
              <p>주문: {d.orderCount}건</p>
            </div>
          </div>
        );
      })}
    </div>
  );
}

export default function AnalyticsPage() {
  const [analytics, setAnalytics] = useState<SalesAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [days, setDays] = useState(30);

  const fetchData = (d: number) => {
    setLoading(true);
    getSalesAnalytics(d)
      .then(setAnalytics)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchData(days);
  }, [days]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!analytics) {
    return <div className="text-center py-20 text-gray-500">데이터를 불러올 수 없습니다.</div>;
  }

  const maxRevenue = Math.max(...analytics.dailySales.map((d) => d.revenue), 1);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">매출 분석</h1>
          <p className="text-sm text-gray-500 mt-1">최근 {days}일간의 매출 데이터</p>
        </div>
        <div className="flex gap-2">
          {[7, 14, 30, 90].map((d) => (
            <button
              key={d}
              onClick={() => setDays(d)}
              className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${
                days === d
                  ? "bg-[var(--color-primary)] text-white"
                  : "bg-gray-100 text-gray-600 hover:bg-gray-200"
              }`}
            >
              {d}일
            </button>
          ))}
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-green-600" />
            </div>
            <span className="text-sm text-gray-500">총 매출</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(analytics.totalRevenue)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <ShoppingCart className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">총 주문수</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{analytics.totalOrders}건</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-10 h-10 rounded-lg bg-purple-50 flex items-center justify-center">
              <TrendingUp className="w-5 h-5 text-purple-600" />
            </div>
            <span className="text-sm text-gray-500">평균 주문가</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(analytics.averageOrderValue)}</p>
        </div>
      </div>

      {/* Chart */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <div className="flex items-center gap-2 mb-6">
          <BarChart3 className="w-5 h-5 text-gray-600" />
          <h2 className="font-semibold text-gray-900">일별 매출 추이</h2>
        </div>

        <MiniBarChart data={analytics.dailySales} maxValue={maxRevenue} />

        {/* X-axis labels */}
        <div className="flex mt-2">
          {analytics.dailySales.length > 0 && (
            <>
              <span className="text-xs text-gray-400">{analytics.dailySales[0].date.slice(5)}</span>
              <span className="flex-1" />
              {analytics.dailySales.length > 7 && (
                <>
                  <span className="text-xs text-gray-400">
                    {analytics.dailySales[Math.floor(analytics.dailySales.length / 2)].date.slice(5)}
                  </span>
                  <span className="flex-1" />
                </>
              )}
              <span className="text-xs text-gray-400">
                {analytics.dailySales[analytics.dailySales.length - 1].date.slice(5)}
              </span>
            </>
          )}
        </div>

        <div className="flex items-center gap-4 mt-4 text-xs text-gray-400">
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded bg-[var(--color-primary)]" />
            <span>평일</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded bg-blue-300" />
            <span>주말</span>
          </div>
        </div>
      </div>

      {/* Daily Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="font-semibold text-gray-900">일별 상세 데이터</h2>
        </div>
        <div className="overflow-x-auto max-h-[400px] overflow-y-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 sticky top-0">
              <tr>
                <th className="px-6 py-3 text-left font-medium text-gray-500">날짜</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">주문수</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">매출</th>
              </tr>
            </thead>
            <tbody>
              {[...analytics.dailySales].reverse().map((d, i) => (
                <tr key={i} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-6 py-3 text-gray-700">{d.date}</td>
                  <td className="px-6 py-3 text-right text-gray-700">{d.orderCount}건</td>
                  <td className="px-6 py-3 text-right font-medium text-gray-900">{formatPrice(d.revenue)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
