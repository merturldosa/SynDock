"use client";

import { useEffect, useState, useCallback, useRef } from "react";
import Link from "next/link";
import Image from "next/image";
import { useTranslations } from "next-intl";
import { Package, FolderTree, ShoppingCart, Users, TrendingUp, AlertTriangle, Warehouse, BarChart3 } from "lucide-react";
import { getDashboardStats, getSalesAnalytics, type DashboardStats, type DailySales } from "@/lib/adminApi";
import { useAdminDashboardStore } from "@/stores/adminDashboardStore";
import { formatPrice } from "@/lib/format";

export default function AdminDashboard() {
  const t = useTranslations();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [trend, setTrend] = useState<DailySales[]>([]);
  const [loading, setLoading] = useState(true);
  const [pulse, setPulse] = useState(false);
  const [toast, setToast] = useState<string | null>(null);
  const { lastEvent, connect, disconnect } = useAdminDashboardStore();
  const toastTimer = useRef<NodeJS.Timeout | null>(null);

  const STATUS_LABELS: Record<string, string> = {
    Pending: t("admin.orders.statusPending"),
    Confirmed: t("admin.orders.statusConfirmed"),
    Processing: t("admin.orders.statusProcessing"),
    Shipped: t("admin.orders.statusShipped"),
    Delivered: t("admin.orders.statusDelivered"),
    Cancelled: t("admin.orders.statusCancelled"),
    Refunded: t("admin.orders.statusRefunded"),
  };

  const loadStats = useCallback(() => {
    getDashboardStats().then(setStats).catch(() => {});
  }, []);

  useEffect(() => {
    loadStats();
    getSalesAnalytics(7).then((d) => setTrend(d.dailySales || [])).catch(() => {});
    setLoading(false);

    const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null;
    if (token) {
      connect(token);
    }

    return () => {
      disconnect();
    };
  }, []);

  // React to real-time events
  useEffect(() => {
    if (!lastEvent) return;

    // Reload dashboard stats
    loadStats();

    // Pulse animation
    setPulse(true);
    setTimeout(() => setPulse(false), 2000);

    // Toast notification
    let msg: string;
    switch (lastEvent.type) {
      case "NewOrder":
        msg = t("admin.dashboard.newOrder", { orderNumber: lastEvent.orderNumber, amount: formatPrice(lastEvent.totalAmount || 0) });
        break;
      case "OrderStatusChanged":
        msg = t("admin.dashboard.orderStatusChanged", { orderNumber: lastEvent.orderNumber, status: STATUS_LABELS[lastEvent.newStatus || ""] || lastEvent.newStatus || "" });
        break;
      case "MesSyncCompleted":
        msg = t("admin.dashboard.mesSyncCompleted", { synced: lastEvent.syncedCount || 0, failed: lastEvent.failedCount || 0 });
        break;
      case "AutoReorderTriggered":
        msg = t("admin.dashboard.autoReorderTriggered", { orderNumber: lastEvent.orderNumber, items: lastEvent.itemCount || 0, quantity: lastEvent.totalQuantity || 0 });
        break;
      default:
        msg = t("admin.dashboard.notification");
    }

    setToast(msg);
    if (toastTimer.current) clearTimeout(toastTimer.current);
    toastTimer.current = setTimeout(() => setToast(null), 5000);
  }, [lastEvent, loadStats]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!stats) return null;

  const cards = [
    { label: t("admin.dashboard.products"), value: stats.totalProducts, icon: Package, href: "/admin/products", color: "bg-blue-500" },
    { label: t("admin.dashboard.categories"), value: stats.totalCategories, icon: FolderTree, href: "/admin/categories", color: "bg-emerald-500" },
    { label: t("admin.dashboard.orders"), value: stats.totalOrders, icon: ShoppingCart, href: "/admin/orders", color: "bg-purple-500" },
    { label: t("admin.dashboard.revenue"), value: stats.totalRevenue, icon: TrendingUp, href: "/admin/orders", color: "bg-orange-500", isCurrency: true },
    { label: t("admin.dashboard.users"), value: stats.totalUsers, icon: Users, href: "/admin/users", color: "bg-indigo-500" },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("admin.dashboard.title")}
      </h1>

      {/* Toast */}
      {toast && (
        <div className="fixed top-4 right-4 z-50 bg-[var(--color-primary)] text-white px-5 py-3 rounded-xl shadow-lg text-sm font-medium animate-[slideIn_0.3s_ease-out]">
          {toast}
        </div>
      )}

      {/* Today Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div className={`bg-white rounded-xl shadow-sm p-5 border-l-4 border-blue-500 transition-all ${pulse ? "ring-2 ring-blue-300 ring-opacity-50" : ""}`}>
          <p className="text-sm text-gray-500">{t("admin.dashboard.todayOrders")}</p>
          <p className={`text-2xl font-bold text-[var(--color-secondary)] ${pulse ? "animate-pulse" : ""}`}>{t("common.items", { count: stats.todayOrders })}</p>
        </div>
        <div className={`bg-white rounded-xl shadow-sm p-5 border-l-4 border-green-500 transition-all ${pulse ? "ring-2 ring-green-300 ring-opacity-50" : ""}`}>
          <p className="text-sm text-gray-500">{t("admin.dashboard.todaySales")}</p>
          <p className={`text-2xl font-bold text-[var(--color-secondary)] ${pulse ? "animate-pulse" : ""}`}>{formatPrice(stats.todayRevenue)}</p>
        </div>
        <Link href="/admin/inventory" className="bg-white rounded-xl shadow-sm p-5 border-l-4 border-red-500 hover:shadow-md transition-shadow">
          <div className="flex items-center justify-between">
            <p className="text-sm text-gray-500">{t("admin.dashboard.lowStockWarning")}</p>
            {stats.lowStockCount > 0 && (
              <span className="w-6 h-6 bg-red-500 text-white text-xs font-bold rounded-full flex items-center justify-center">
                {stats.lowStockCount}
              </span>
            )}
          </div>
          <div className="flex items-center gap-2">
            <p className="text-2xl font-bold text-[var(--color-secondary)]">{t("common.items", { count: stats.lowStockCount })}</p>
            {stats.lowStockCount > 0 && <AlertTriangle size={20} className="text-red-500" />}
          </div>
        </Link>
        <div className="bg-white rounded-xl shadow-sm p-5 border-l-4 border-purple-500">
          <p className="text-sm text-gray-500">{t("admin.dashboard.avgOrderAmount")}</p>
          <p className="text-2xl font-bold text-[var(--color-secondary)]">
            {stats.totalOrders > 0 ? formatPrice(Math.round(stats.totalRevenue / stats.totalOrders)) : formatPrice(0)}
          </p>
        </div>
      </div>

      {/* 7-Day Revenue Trend */}
      {trend.length > 1 && (
        <Link href="/admin/analytics" className="bg-white rounded-xl shadow-sm p-5 mb-6 hover:shadow-md transition-shadow block">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <BarChart3 size={18} className="text-[var(--color-primary)]" />
              <h2 className="font-semibold text-[var(--color-secondary)]">{t("admin.dashboard.weeklyTrend")}</h2>
            </div>
            <span className="text-xs text-[var(--color-primary)]">{t("admin.dashboard.viewDetails")} →</span>
          </div>
          <div className="flex items-end gap-1 h-16">
            {trend.map((d, i) => {
              const max = Math.max(...trend.map((t) => t.revenue), 1);
              const h = (d.revenue / max) * 100;
              return (
                <div key={i} className="flex-1 flex flex-col items-center gap-0.5 group relative">
                  <div
                    className="w-full rounded-t bg-[var(--color-primary)] opacity-70 hover:opacity-100 transition-opacity"
                    style={{ height: `${Math.max(h, 4)}%` }}
                  />
                  <span className="text-[10px] text-gray-400">{d.date.slice(8)}</span>
                  <div className="absolute bottom-full mb-1 hidden group-hover:block bg-gray-800 text-white text-[10px] rounded px-1.5 py-0.5 whitespace-nowrap z-10">
                    {formatPrice(d.revenue)}
                  </div>
                </div>
              );
            })}
          </div>
        </Link>
      )}

      {/* Stat Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
        {cards.map((card) => (
          <Link
            key={card.label}
            href={card.href}
            className="bg-white rounded-xl shadow-sm p-5 hover:shadow-md transition-shadow"
          >
            <div className="flex items-center justify-between mb-3">
              <span className="text-sm text-gray-500">{card.label}</span>
              <div className={`w-10 h-10 ${card.color} rounded-lg flex items-center justify-center`}>
                <card.icon size={20} className="text-white" />
              </div>
            </div>
            <p className="text-2xl font-bold text-[var(--color-secondary)]">
              {"isCurrency" in card && card.isCurrency
                ? formatPrice(card.value)
                : card.value.toLocaleString()}
            </p>
          </Link>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        {/* Order Status Distribution */}
        <div className="bg-white rounded-xl shadow-sm p-5">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">{t("admin.dashboard.orderStatus")}</h2>
          {stats.ordersByStatus.length === 0 ? (
            <p className="text-sm text-gray-400">{t("admin.dashboard.noOrderData")}</p>
          ) : (
            <div className="space-y-3">
              {stats.ordersByStatus.map((item) => {
                const maxCount = Math.max(...stats.ordersByStatus.map((s) => s.count));
                const pct = maxCount > 0 ? (item.count / maxCount) * 100 : 0;
                return (
                  <div key={item.status}>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-gray-600">{STATUS_LABELS[item.status] || item.status}</span>
                      <span className="font-medium">{t("common.items", { count: item.count })}</span>
                    </div>
                    <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-[var(--color-primary)] rounded-full transition-all"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Category Sales */}
        <div className="bg-white rounded-xl shadow-sm p-5">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">{t("admin.dashboard.categorySalesTop5")}</h2>
          {stats.categorySales.length === 0 ? (
            <p className="text-sm text-gray-400">{t("admin.dashboard.noSalesData")}</p>
          ) : (
            <div className="space-y-3">
              {stats.categorySales.map((cat) => {
                const maxSales = Math.max(...stats.categorySales.map((c) => c.totalSales));
                const pct = maxSales > 0 ? (cat.totalSales / maxSales) * 100 : 0;
                return (
                  <div key={cat.categoryName}>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-gray-600">{cat.categoryName}</span>
                      <span className="font-medium">{formatPrice(cat.totalSales)}</span>
                    </div>
                    <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-emerald-500 rounded-full transition-all"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <p className="text-xs text-gray-400 mt-0.5">{t("admin.dashboard.nOrders", { count: cat.orderCount })}</p>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        {/* Top Products */}
        <div className="bg-white rounded-xl shadow-sm p-5">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">{t("admin.dashboard.bestSellers")}</h2>
          {stats.topProducts.length === 0 ? (
            <p className="text-sm text-gray-400">{t("admin.dashboard.noSalesHistory")}</p>
          ) : (
            <div className="space-y-3">
              {stats.topProducts.map((product, i) => (
                <div key={product.productId} className="flex items-center gap-3">
                  <span className="text-sm font-bold text-gray-400 w-5">{i + 1}</span>
                  <div className="w-10 h-10 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0">
                    {product.imageUrl ? (
                      <Image src={product.imageUrl} alt={product.productName} width={40} height={40} className="object-cover w-full h-full" />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-sm opacity-20">📦</div>
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium line-clamp-1">{product.productName}</p>
                    <p className="text-xs text-gray-400">{t("admin.dashboard.nSold", { count: product.orderCount })}</p>
                  </div>
                  <span className="text-sm font-medium">{formatPrice(product.totalSales)}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Recent Orders */}
        <div className="bg-white rounded-xl shadow-sm p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-semibold text-[var(--color-secondary)]">{t("admin.dashboard.recentOrders")}</h2>
            <Link href="/admin/orders" className="text-sm text-[var(--color-primary)] hover:underline">
              {t("admin.dashboard.viewAll")}
            </Link>
          </div>
          {stats.recentOrders.length === 0 ? (
            <p className="text-sm text-gray-400">{t("admin.dashboard.noOrders")}</p>
          ) : (
            <div className="space-y-2">
              {stats.recentOrders.map((order) => (
                <Link
                  key={order.id}
                  href={`/admin/orders/${order.id}`}
                  className="flex items-center justify-between p-2 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  <div>
                    <p className="text-xs font-mono text-[var(--color-primary)]">{order.orderNumber}</p>
                    <p className="text-xs text-gray-400">{new Date(order.createdAt).toLocaleDateString("ko-KR")}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium">{formatPrice(order.totalAmount)}</p>
                    <span className="text-xs text-gray-500">{STATUS_LABELS[order.status] || order.status}</span>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-xl shadow-sm p-5">
        <h2 className="font-semibold text-[var(--color-secondary)] mb-3">{t("admin.dashboard.quickActions")}</h2>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
          <Link href="/admin/products/new" className="flex items-center gap-2 p-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90">
            <Package size={16} /> {t("admin.products.addNew")}
          </Link>
          <Link href="/admin/categories" className="flex items-center gap-2 p-3 bg-[var(--color-secondary)] text-white rounded-lg text-sm font-medium hover:opacity-90">
            <FolderTree size={16} /> {t("admin.categories.title")}
          </Link>
          <Link href="/admin/inventory" className="flex items-center gap-2 p-3 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50">
            <Warehouse size={16} /> {t("admin.inventory.title")}
          </Link>
          <Link href="/admin/coupons/new" className="flex items-center gap-2 p-3 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50">
            <Package size={16} /> {t("admin.coupons.addNew")}
          </Link>
          <Link href="/admin/orders" className="flex items-center gap-2 p-3 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50">
            <ShoppingCart size={16} /> {t("admin.orders.title")}
          </Link>
        </div>
      </div>
    </div>
  );
}
