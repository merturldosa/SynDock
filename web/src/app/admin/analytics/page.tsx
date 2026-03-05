"use client";

import { useEffect, useState } from "react";
import {
  getSalesAnalytics, getCustomerAnalytics, getProductPerformance, exportSalesReport,
  type SalesAnalytics, type DailySales, type CustomerAnalytics, type ProductPerformanceResult,
} from "@/lib/adminApi";
import { BarChart3, TrendingUp, TrendingDown, ShoppingCart, DollarSign, Users, Package, Download, ArrowUpRight, ArrowDownRight } from "lucide-react";
import Image from "next/image";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import { formatPrice, formatDateShort } from "@/lib/format";

function MiniBarChart({ data, maxValue, t }: { data: DailySales[]; maxValue: number; t: ReturnType<typeof useTranslations> }) {
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
              <p>{t("admin.analytics.tooltipSales", { amount: formatPrice(d.revenue) })}</p>
              <p>{t("admin.analytics.tooltipOrders", { count: d.orderCount })}</p>
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
  const t = useTranslations();
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
        .then(setAnalytics).catch(() => { toast.error(t("common.fetchError")); }).finally(() => setLoading(false));
    } else {
      getSalesAnalytics(days, undefined, undefined, comparison)
        .then(setAnalytics).catch(() => { toast.error(t("common.fetchError")); }).finally(() => setLoading(false));
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
    } catch { toast.error(t("admin.analytics.exportFailed")); }
    setExporting(false);
  };

  if (loading) return <LoadingSpinner />;
  if (!analytics) return <div className="text-center py-20 text-gray-500">{t("admin.analytics.loadFailed")}</div>;

  const maxRevenue = Math.max(...analytics.dailySales.map((d) => d.revenue), 1);

  return (
    <div className="space-y-6">
      {/* Controls */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex gap-2 items-center flex-wrap">
          {!customRange && [7, 14, 30, 90].map((d) => (
            <button key={d} onClick={() => setDays(d)}
              className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${days === d ? "bg-[var(--color-primary)] text-white" : "bg-gray-100 text-gray-600 hover:bg-gray-200"}`}>
              {t("admin.analytics.daysUnit", { days: d })}
            </button>
          ))}
          <button onClick={() => setCustomRange(!customRange)}
            className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${customRange ? "bg-[var(--color-primary)] text-white" : "bg-gray-100 text-gray-600 hover:bg-gray-200"}`}>
            {t("admin.analytics.customRange")}
          </button>
          <label className="flex items-center gap-1.5 text-sm text-gray-600 ml-2">
            <input type="checkbox" checked={comparison} onChange={(e) => setComparison(e.target.checked)} className="rounded" />
            {t("admin.analytics.comparePrevious")}
          </label>
        </div>
        <button onClick={handleExport} disabled={exporting}
          className="flex items-center gap-1.5 px-4 py-2 bg-green-600 text-white rounded-lg text-sm font-medium hover:bg-green-700 disabled:opacity-60 transition-colors">
          <Download size={16} /> {exporting ? t("admin.analytics.exporting") : t("admin.analytics.csvExport")}
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
            <span className="text-sm text-gray-500">{t("admin.analytics.totalSales")}</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(analytics.totalRevenue)}</p>
          {comparison && <div className="mt-1"><ChangeBadge value={analytics.revenueChangePercent} /></div>}
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <ShoppingCart className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">{t("admin.analytics.totalOrders")}</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{t("admin.analytics.orderCountUnit", { count: analytics.totalOrders })}</p>
          {comparison && <div className="mt-1"><ChangeBadge value={analytics.ordersChangePercent} /></div>}
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-10 h-10 rounded-lg bg-purple-50 flex items-center justify-center">
              <TrendingUp className="w-5 h-5 text-purple-600" />
            </div>
            <span className="text-sm text-gray-500">{t("admin.analytics.averageOrderValue")}</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(analytics.averageOrderValue)}</p>
        </div>
      </div>

      {/* Chart */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <div className="flex items-center gap-2 mb-6">
          <BarChart3 className="w-5 h-5 text-gray-600" />
          <h2 className="font-semibold text-gray-900">{t("admin.analytics.dailySalesTrend")}</h2>
        </div>
        <MiniBarChart data={analytics.dailySales} maxValue={maxRevenue} t={t} />
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
          <div className="flex items-center gap-1"><div className="w-3 h-3 rounded bg-[var(--color-primary)]" /><span>{t("admin.analytics.weekday")}</span></div>
          <div className="flex items-center gap-1"><div className="w-3 h-3 rounded bg-blue-300" /><span>{t("admin.analytics.weekend")}</span></div>
        </div>
      </div>

      {/* Daily Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="font-semibold text-gray-900">{t("admin.analytics.dailyDetail")}</h2>
        </div>
        <div className="overflow-x-auto max-h-[400px] overflow-y-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 sticky top-0">
              <tr>
                <th className="px-6 py-3 text-left font-medium text-gray-500">{t("admin.analytics.date")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("admin.analytics.orders")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("admin.analytics.sales")}</th>
              </tr>
            </thead>
            <tbody>
              {[...analytics.dailySales].reverse().map((d, i) => (
                <tr key={i} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-6 py-3 text-gray-700">{d.date}</td>
                  <td className="px-6 py-3 text-right text-gray-700">{t("admin.analytics.orderCountUnit", { count: d.orderCount })}</td>
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
  const t = useTranslations();
  const [data, setData] = useState<CustomerAnalytics | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getCustomerAnalytics().then(setData).catch(() => { toast.error(t("common.fetchError")); }).finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingSpinner />;
  if (!data) return <div className="text-center py-20 text-gray-500">{t("admin.analytics.loadFailed")}</div>;

  const segmentColors = ["bg-blue-500", "bg-green-500", "bg-yellow-500", "bg-gray-400"];
  const totalSegmentCount = data.segments.reduce((s, seg) => s + seg.count, 0) || 1;

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <p className="text-sm text-gray-500 mb-1">{t("admin.analytics.totalCustomers")}</p>
          <p className="text-2xl font-bold text-gray-900">{t("admin.analytics.personUnit", { count: data.totalCustomers })}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <p className="text-sm text-gray-500 mb-1">{t("admin.analytics.newCustomers30")}</p>
          <p className="text-2xl font-bold text-blue-600">{t("admin.analytics.personUnit", { count: data.newCustomers30Days })}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <p className="text-sm text-gray-500 mb-1">{t("admin.analytics.returningCustomers")}</p>
          <p className="text-2xl font-bold text-green-600">{t("admin.analytics.personUnit", { count: data.returningCustomers })}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Segment Donut (CSS) */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">{t("admin.analytics.customerSegment")}</h3>
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
                  <span className="text-gray-400 ml-auto">{t("admin.analytics.personUnit", { count: seg.count })}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Spend Tiers */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h3 className="font-semibold text-gray-900 mb-4">{t("admin.analytics.spendTiers")}</h3>
          <div className="space-y-3">
            {data.spendTiers.map((tier, i) => {
              const maxCount = Math.max(...data.spendTiers.map((t) => t.count), 1);
              const pct = (tier.count / maxCount) * 100;
              return (
                <div key={i}>
                  <div className="flex justify-between text-sm mb-1">
                    <span className="text-gray-700">{tier.tier}</span>
                    <span className="text-gray-500">{t("admin.analytics.personUnit", { count: tier.count })} · {formatPrice(tier.totalSpent)}</span>
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
          <h3 className="font-semibold text-gray-900">{t("admin.analytics.top10Customers")}</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left font-medium text-gray-500">#</th>
                <th className="px-6 py-3 text-left font-medium text-gray-500">{t("admin.analytics.customer")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("admin.analytics.orders")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("admin.analytics.totalSpent")}</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">{t("admin.analytics.lastOrder")}</th>
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
                  <td className="px-6 py-3 text-right">{t("admin.analytics.orderCountUnit", { count: c.orderCount })}</td>
                  <td className="px-6 py-3 text-right font-medium text-gray-900">{formatPrice(c.totalSpent)}</td>
                  <td className="px-6 py-3 text-right text-gray-500 text-xs">
                    {formatDateShort(c.lastOrderAt)}
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
  const t = useTranslations();
  const [data, setData] = useState<ProductPerformanceResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [sort, setSort] = useState("revenue");
  const [page, setPage] = useState(1);

  useEffect(() => {
    setLoading(true);
    getProductPerformance(sort, page).then(setData).catch(() => { toast.error(t("common.fetchError")); }).finally(() => setLoading(false));
  }, [sort, page]);

  if (loading) return <LoadingSpinner />;
  if (!data) return <div className="text-center py-20 text-gray-500">{t("admin.analytics.loadFailed")}</div>;

  const sortOptions = [
    { value: "revenue", label: t("admin.analytics.sortByRevenue") },
    { value: "orders", label: t("admin.analytics.sortByOrders") },
    { value: "views", label: t("admin.analytics.sortByViews") },
    { value: "conversion", label: t("admin.analytics.sortByConversion") },
    { value: "rating", label: t("admin.analytics.sortByRating") },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">{t("admin.analytics.totalProducts", { count: data.totalProducts })}</p>
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
                <th className="px-4 py-3 text-left font-medium text-gray-500">{t("admin.analytics.productCol")}</th>
                <th className="px-4 py-3 text-left font-medium text-gray-500">{t("admin.analytics.categoryCol")}</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">{t("admin.analytics.viewCountCol")}</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">{t("admin.analytics.orderCountCol")}</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">{t("admin.analytics.conversionRateCol")}</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">{t("admin.analytics.revenueCol")}</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">{t("admin.analytics.ratingCol")}</th>
              </tr>
            </thead>
            <tbody>
              {data.products.map((p) => (
                <tr key={p.productId} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <div className="relative w-8 h-8 rounded bg-gray-100 overflow-hidden shrink-0">
                        {p.imageUrl ? (
                          <Image src={p.imageUrl} alt="" fill className="object-cover" sizes="32px" />
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
            className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">{t("admin.analytics.prev")}</button>
          <span className="text-sm text-gray-500">{page} / {Math.ceil(data.totalProducts / 20)}</span>
          <button onClick={() => setPage((p) => p + 1)} disabled={page * 20 >= data.totalProducts}
            className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">{t("admin.analytics.next")}</button>
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
  const t = useTranslations();
  const [tab, setTab] = useState<"sales" | "customers" | "products">("sales");

  const tabs = [
    { key: "sales" as const, label: t("admin.analytics.tabSales"), icon: BarChart3 },
    { key: "customers" as const, label: t("admin.analytics.tabCustomers"), icon: Users },
    { key: "products" as const, label: t("admin.analytics.tabProducts"), icon: Package },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">{t("admin.analytics.dashboardTitle")}</h1>
        <p className="text-sm text-gray-500 mt-1">{t("admin.analytics.dashboardSubtitle")}</p>
      </div>

      {/* Tab Bar */}
      <div className="flex gap-1 bg-gray-100 rounded-xl p-1">
        {tabs.map((tabItem) => {
          const Icon = tabItem.icon;
          return (
            <button
              key={tabItem.key}
              onClick={() => setTab(tabItem.key)}
              className={`flex-1 flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                tab === tabItem.key
                  ? "bg-white text-[var(--color-primary)] shadow-sm"
                  : "text-gray-500 hover:text-gray-700"
              }`}
            >
              <Icon size={16} />
              {tabItem.label}
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
