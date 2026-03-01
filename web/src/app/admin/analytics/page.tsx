"use client";

import { useEffect, useState } from "react";
import {
  getSalesAnalytics, getCustomerAnalytics, getProductPerformance, exportSalesReport,
  type SalesAnalytics, type DailySales, type CustomerAnalytics, type ProductPerformanceResult,
} from "@/lib/adminApi";
import { BarChart3, TrendingUp, TrendingDown, ShoppingCart, DollarSign, Users, Package, Download, ArrowUpRight, ArrowDownRight } from "lucide-react";
import Image from "next/image";

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

function ChangeBadge({ value }: { value: number | null | undefined }) {
  if (value == null) return null;
  const isPositive = value >= 0;
  return (
    <span className={`inline-flex items-center gap-0.5 text-xs font-medium px-1.5 py-0.5 rounded ${
      isPositive ? "bg-green-100 text-green-700" : "bg-red-100 text-red-700"
    }`}>
      {isPositive ? <ArrowUpRight size={12} /> : <ArrowDownRight size={12} />}
      {Math.abs(value)}%
    </span>
  );
}

// ── Sales Tab ──
function SalesTab() {
  const [analytics, setAnalytics] = useState<SalesAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [days, setDays] = useState(30);
  const [comparison, setComparison] = useState(false);
  const [customRange, setCustomRange] = useState(false);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [exporting, setExporting] = useState(false);

  const fetchData = () => {
    setLoading(true);
    if (customRange && startDate && endDate) {
      getSalesAnalytics(undefined as unknown as number, startDate, endDate, comparison)
        .then(setAnalytics).catch(() => {}).finally(() => setLoading(false));
    } else {
      getSalesAnalytics(days, undefined, undefined, comparison)
        .then(setAnalytics).catch(() => {}).finally(() => setLoading(false));
    }
  };

  useEffect(() => { fetchData(); }, [days, comparison, customRange, startDate, endDate]);

  const handleExport = async () => {
    if (!analytics || analytics.dailySales.length === 0) return;
    setExporting(true);
    try {
      const s = analytics.dailySales[0].date;
      const e = analytics.dailySales[analytics.dailySales.length - 1].date;
      await exportSalesReport(s, e);
    } catch { alert("내보내기에 실패했습니다."); }
    setExporting(false);
  };

  if (loading) return <LoadingSpinner />;
  if (!analytics) return <div className="text-center py-20 text-gray-500">데이터를 불러올 수 없습니다.</div>;

  const maxRevenue = Math.max(...analytics.dailySales.map((d) => d.revenue), 1);

  return (
    <div className="space-y-6">
      {/* Controls */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex gap-2 items-center flex-wrap">
          {!customRange && [7, 14, 30, 90].map((d) => (
            <button key={d} onClick={() => setDays(d)}
              className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${days === d ? "bg-[var(--color-primary)] text-white" : "bg-gray-100 text-gray-600 hover:bg-gray-200"}`}>
              {d}일
            </button>
          ))}
          <button onClick={() => setCustomRange(!customRange)}
            className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${customRange ? "bg-[var(--color-primary)] text-white" : "bg-gray-100 text-gray-600 hover:bg-gray-200"}`}>
            직접 선택
          </button>
          <label className="flex items-center gap-1.5 text-sm text-gray-600 ml-2">
            <input type="checkbox" checked={comparison} onChange={(e) => setComparison(e.target.checked)} className="rounded" />
            이전 기간 비교
          </label>
        </div>
        <button onClick={handleExport} disabled={exporting}
          className="flex items-center gap-1.5 px-4 py-2 bg-green-600 text-white rounded-lg text-sm font-medium hover:bg-green-700 disabled:opacity-60 transition-colors">
          <Download size={16} /> {exporting ? "내보내기 중..." : "CSV 내보내기"}
        </button>
      </div>

      {customRange && (
        <div className="flex items-center gap-3 flex-wrap">
          <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)}
            className="px-3 py-2 border border-gray-200 rounded-lg text-sm" />
          <span className="text-gray-400">~</span>
          <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)}
            className="px-3 py-2 border border-gray-200 rounded-lg text-sm" />
        </div>
      )}

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
          {comparison && <div className="mt-1"><ChangeBadge value={analytics.revenueChangePercent} /></div>}
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <ShoppingCart className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">총 주문수</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{analytics.totalOrders}건</p>
          {comparison && <div className="mt-1"><ChangeBadge value={analytics.ordersChangePercent} /></div>}
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
        <div className="flex mt-2">
          {analytics.dailySales.length > 0 && (
            <>
              <span className="text-xs text-gray-400">{analytics.dailySales[0].date.slice(5)}</span>
              <span className="flex-1" />
              {analytics.dailySales.length > 7 && (
                <>
                  <span className="text-xs text-gray-400">{analytics.dailySales[Math.floor(analytics.dailySales.length / 2)].date.slice(5)}</span>
                  <span className="flex-1" />
                </>
              )}
              <span className="text-xs text-gray-400">{analytics.dailySales[analytics.dailySales.length - 1].date.slice(5)}</span>
            </>
          )}
        </div>
        <div className="flex items-center gap-4 mt-4 text-xs text-gray-400">
          <div className="flex items-center gap-1"><div className="w-3 h-3 rounded bg-[var(--color-primary)]" /><span>평일</span></div>
          <div className="flex items-center gap-1"><div className="w-3 h-3 rounded bg-blue-300" /><span>주말</span></div>
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

// ── Customer Tab ──
function CustomerTab() {
  const [data, setData] = useState<CustomerAnalytics | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getCustomerAnalytics().then(setData).catch(() => {}).finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingSpinner />;
  if (!data) return <div className="text-center py-20 text-gray-500">데이터를 불러올 수 없습니다.</div>;

  const segmentColors = ["bg-blue-500", "bg-green-500", "bg-yellow-500", "bg-gray-400"];
  const totalSegmentCount = data.segments.reduce((s, seg) => s + seg.count, 0) || 1;

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <p className="text-sm text-gray-500 mb-1">전체 고객</p>
          <p className="text-2xl font-bold text-gray-900">{data.totalCustomers}명</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <p className="text-sm text-gray-500 mb-1">신규 고객 (30일)</p>
          <p className="text-2xl font-bold text-blue-600">{data.newCustomers30Days}명</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <p className="text-sm text-gray-500 mb-1">재구매 고객</p>
          <p className="text-2xl font-bold text-green-600">{data.returningCustomers}명</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Segment Donut (CSS) */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">고객 세그먼트</h3>
          <div className="flex items-center gap-6">
            {/* CSS Donut */}
            <div className="relative w-32 h-32 shrink-0">
              <svg viewBox="0 0 36 36" className="w-full h-full -rotate-90">
                {(() => {
                  let offset = 0;
                  const colors = ["#3B82F6", "#22C55E", "#EAB308", "#9CA3AF"];
                  return data.segments.map((seg, i) => {
                    const pct = (seg.count / totalSegmentCount) * 100;
                    const el = (
                      <circle key={i} cx="18" cy="18" r="15.915" fill="none" stroke={colors[i % colors.length]}
                        strokeWidth="3.5" strokeDasharray={`${pct} ${100 - pct}`} strokeDashoffset={-offset} />
                    );
                    offset += pct;
                    return el;
                  });
                })()}
              </svg>
              <div className="absolute inset-0 flex items-center justify-center">
                <span className="text-lg font-bold text-gray-900">{data.totalCustomers}</span>
              </div>
            </div>
            <div className="space-y-2 text-sm">
              {data.segments.map((seg, i) => (
                <div key={i} className="flex items-center gap-2">
                  <div className={`w-3 h-3 rounded-full ${segmentColors[i % segmentColors.length]}`} />
                  <span className="text-gray-700">{seg.segment}</span>
                  <span className="text-gray-400 ml-auto">{seg.count}명</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Spend Tiers */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">지출 등급 분포</h3>
          <div className="space-y-3">
            {data.spendTiers.map((tier, i) => {
              const maxCount = Math.max(...data.spendTiers.map((t) => t.count), 1);
              const pct = (tier.count / maxCount) * 100;
              return (
                <div key={i}>
                  <div className="flex justify-between text-sm mb-1">
                    <span className="text-gray-700">{tier.tier}</span>
                    <span className="text-gray-500">{tier.count}명 · {formatPrice(tier.totalSpent)}</span>
                  </div>
                  <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
                    <div className="h-full bg-[var(--color-primary)] rounded-full" style={{ width: `${pct}%` }} />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Top Customers */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="font-semibold text-gray-900">상위 10 고객</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left font-medium text-gray-500">#</th>
                <th className="px-6 py-3 text-left font-medium text-gray-500">고객</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">주문수</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">총 지출</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">마지막 주문</th>
              </tr>
            </thead>
            <tbody>
              {data.topCustomers.map((c, i) => (
                <tr key={c.userId} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-6 py-3 text-gray-400 font-medium">{i + 1}</td>
                  <td className="px-6 py-3">
                    <p className="font-medium text-gray-900">{c.name}</p>
                    <p className="text-xs text-gray-400">{c.email}</p>
                  </td>
                  <td className="px-6 py-3 text-right">{c.orderCount}건</td>
                  <td className="px-6 py-3 text-right font-medium text-gray-900">{formatPrice(c.totalSpent)}</td>
                  <td className="px-6 py-3 text-right text-gray-500 text-xs">
                    {new Date(c.lastOrderAt).toLocaleDateString("ko-KR")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// ── Product Tab ──
function ProductTab() {
  const [data, setData] = useState<ProductPerformanceResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [sort, setSort] = useState("revenue");
  const [page, setPage] = useState(1);

  useEffect(() => {
    setLoading(true);
    getProductPerformance(sort, page).then(setData).catch(() => {}).finally(() => setLoading(false));
  }, [sort, page]);

  if (loading) return <LoadingSpinner />;
  if (!data) return <div className="text-center py-20 text-gray-500">데이터를 불러올 수 없습니다.</div>;

  const sortOptions = [
    { value: "revenue", label: "매출순" },
    { value: "orders", label: "주문순" },
    { value: "views", label: "조회순" },
    { value: "conversion", label: "전환율순" },
    { value: "rating", label: "평점순" },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">총 {data.totalProducts}개 상품</p>
        <div className="flex gap-2">
          {sortOptions.map((opt) => (
            <button key={opt.value} onClick={() => { setSort(opt.value); setPage(1); }}
              className={`px-3 py-1.5 text-xs rounded-lg transition-colors ${sort === opt.value ? "bg-[var(--color-primary)] text-white" : "bg-gray-100 text-gray-600 hover:bg-gray-200"}`}>
              {opt.label}
            </button>
          ))}
        </div>
      </div>
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-gray-500">상품</th>
                <th className="px-4 py-3 text-left font-medium text-gray-500">카테고리</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">조회수</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">주문수</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">전환율</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">매출</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">평점</th>
              </tr>
            </thead>
            <tbody>
              {data.products.map((p) => (
                <tr key={p.productId} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <div className="relative w-8 h-8 rounded bg-gray-100 overflow-hidden shrink-0">
                        {p.imageUrl ? (
                          <Image src={p.imageUrl} alt="" fill className="object-cover" sizes="32px" unoptimized />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-xs opacity-30">📦</div>
                        )}
                      </div>
                      <span className="text-gray-900 truncate max-w-[200px]">{p.productName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-gray-500">{p.categoryName}</td>
                  <td className="px-4 py-3 text-right">{p.viewCount.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right">{p.orderCount}</td>
                  <td className="px-4 py-3 text-right">{p.conversionRate}%</td>
                  <td className="px-4 py-3 text-right font-medium">{formatPrice(p.revenue)}</td>
                  <td className="px-4 py-3 text-right">
                    {p.averageRating > 0 ? (
                      <span className="text-yellow-600">{p.averageRating.toFixed(1)}</span>
                    ) : (
                      <span className="text-gray-300">-</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {data.totalProducts > 20 && (
        <div className="flex items-center justify-center gap-2">
          <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}
            className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">이전</button>
          <span className="text-sm text-gray-500">{page} / {Math.ceil(data.totalProducts / 20)}</span>
          <button onClick={() => setPage((p) => p + 1)} disabled={page * 20 >= data.totalProducts}
            className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">다음</button>
        </div>
      )}
    </div>
  );
}

function LoadingSpinner() {
  return (
    <div className="flex items-center justify-center py-20">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
    </div>
  );
}

export default function AnalyticsPage() {
  const [tab, setTab] = useState<"sales" | "customers" | "products">("sales");

  const tabs = [
    { key: "sales" as const, label: "매출", icon: BarChart3 },
    { key: "customers" as const, label: "고객", icon: Users },
    { key: "products" as const, label: "상품", icon: Package },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">분석 대시보드</h1>
        <p className="text-sm text-gray-500 mt-1">매출, 고객, 상품 성과를 분석합니다</p>
      </div>

      {/* Tab Bar */}
      <div className="flex gap-1 bg-gray-100 rounded-xl p-1">
        {tabs.map((t) => {
          const Icon = t.icon;
          return (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`flex-1 flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                tab === t.key
                  ? "bg-white text-[var(--color-primary)] shadow-sm"
                  : "text-gray-500 hover:text-gray-700"
              }`}
            >
              <Icon size={16} />
              {t.label}
            </button>
          );
        })}
      </div>

      {/* Tab Content */}
      {tab === "sales" && <SalesTab />}
      {tab === "customers" && <CustomerTab />}
      {tab === "products" && <ProductTab />}
    </div>
  );
}
