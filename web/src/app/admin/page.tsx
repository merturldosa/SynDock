"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Package, FolderTree, ShoppingCart, Users, Ticket, TrendingUp } from "lucide-react";
import { getDashboardStats, type DashboardStats } from "@/lib/adminApi";

const STATUS_LABELS: Record<string, string> = {
  Pending: "결제 대기",
  Confirmed: "결제 완료",
  Processing: "처리 중",
  Shipped: "배송 중",
  Delivered: "배송 완료",
  Cancelled: "취소",
  Refunded: "환불",
};

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

export default function AdminDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getDashboardStats()
      .then(setStats)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!stats) return null;

  const cards = [
    { label: "상품", value: stats.totalProducts, icon: Package, href: "/admin/products", color: "bg-blue-500" },
    { label: "카테고리", value: stats.totalCategories, icon: FolderTree, href: "/admin/categories", color: "bg-emerald-500" },
    { label: "주문", value: stats.totalOrders, icon: ShoppingCart, href: "/admin/orders", color: "bg-purple-500" },
    { label: "매출", value: stats.totalRevenue, icon: TrendingUp, href: "/admin/orders", color: "bg-orange-500", isCurrency: true },
    { label: "회원", value: stats.totalUsers, icon: Users, href: "/admin/users", color: "bg-indigo-500" },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        관리자 대시보드
      </h1>

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
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">주문 현황</h2>
          {stats.ordersByStatus.length === 0 ? (
            <p className="text-sm text-gray-400">주문 데이터가 없습니다.</p>
          ) : (
            <div className="space-y-3">
              {stats.ordersByStatus.map((item) => {
                const maxCount = Math.max(...stats.ordersByStatus.map((s) => s.count));
                const pct = maxCount > 0 ? (item.count / maxCount) * 100 : 0;
                return (
                  <div key={item.status}>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-gray-600">{STATUS_LABELS[item.status] || item.status}</span>
                      <span className="font-medium">{item.count}건</span>
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

        {/* Top Products */}
        <div className="bg-white rounded-xl shadow-sm p-5">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">베스트셀러</h2>
          {stats.topProducts.length === 0 ? (
            <p className="text-sm text-gray-400">판매 데이터가 없습니다.</p>
          ) : (
            <div className="space-y-3">
              {stats.topProducts.map((product, i) => (
                <div key={product.productId} className="flex items-center gap-3">
                  <span className="text-sm font-bold text-gray-400 w-5">{i + 1}</span>
                  <div className="w-10 h-10 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0">
                    {product.imageUrl ? (
                      <Image src={product.imageUrl} alt={product.productName} width={40} height={40} className="object-cover w-full h-full" unoptimized />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-sm opacity-20">📦</div>
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium line-clamp-1">{product.productName}</p>
                    <p className="text-xs text-gray-400">{product.orderCount}건 판매</p>
                  </div>
                  <span className="text-sm font-medium">{formatPrice(product.totalSales)}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Recent Orders */}
      <div className="bg-white rounded-xl shadow-sm p-5 mb-8">
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-semibold text-[var(--color-secondary)]">최근 주문</h2>
          <Link href="/admin/orders" className="text-sm text-[var(--color-primary)] hover:underline">
            전체 보기
          </Link>
        </div>
        {stats.recentOrders.length === 0 ? (
          <p className="text-sm text-gray-400">주문이 없습니다.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="border-b">
              <tr>
                <th className="text-left pb-2 font-medium text-gray-500">주문번호</th>
                <th className="text-center pb-2 font-medium text-gray-500">상태</th>
                <th className="text-right pb-2 font-medium text-gray-500">금액</th>
                <th className="text-right pb-2 font-medium text-gray-500">일시</th>
              </tr>
            </thead>
            <tbody>
              {stats.recentOrders.map((order) => (
                <tr key={order.id} className="border-b last:border-0">
                  <td className="py-2.5">
                    <Link href={`/admin/orders/${order.id}`} className="text-[var(--color-primary)] hover:underline font-mono text-xs">
                      {order.orderNumber}
                    </Link>
                  </td>
                  <td className="py-2.5 text-center">
                    <span className="px-2 py-0.5 text-xs rounded-full bg-gray-100 text-gray-600">
                      {STATUS_LABELS[order.status] || order.status}
                    </span>
                  </td>
                  <td className="py-2.5 text-right font-medium">{formatPrice(order.totalAmount)}</td>
                  <td className="py-2.5 text-right text-gray-500 text-xs">
                    {new Date(order.createdAt).toLocaleDateString("ko-KR")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-xl shadow-sm p-5">
        <h2 className="font-semibold text-[var(--color-secondary)] mb-3">빠른 작업</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <Link href="/admin/products/new" className="flex items-center gap-2 p-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90">
            <Package size={16} /> 상품 등록
          </Link>
          <Link href="/admin/categories" className="flex items-center gap-2 p-3 bg-[var(--color-secondary)] text-white rounded-lg text-sm font-medium hover:opacity-90">
            <FolderTree size={16} /> 카테고리 관리
          </Link>
          <Link href="/admin/coupons/new" className="flex items-center gap-2 p-3 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50">
            <Ticket size={16} /> 쿠폰 생성
          </Link>
          <Link href="/admin/orders" className="flex items-center gap-2 p-3 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50">
            <ShoppingCart size={16} /> 주문 관리
          </Link>
        </div>
      </div>
    </div>
  );
}
