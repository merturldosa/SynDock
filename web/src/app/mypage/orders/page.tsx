"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Package, ChevronRight } from "lucide-react";
import { getOrders } from "@/lib/orderApi";
import { useAuthStore } from "@/stores/authStore";
import type { OrderSummary } from "@/types/order";
import { ORDER_STATUS_LABELS, type OrderStatusType } from "@/types/order";

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("ko-KR", {
    year: "numeric", month: "short", day: "numeric",
  });
}

const STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  Confirmed: "bg-blue-100 text-blue-700",
  Processing: "bg-indigo-100 text-indigo-700",
  Shipped: "bg-purple-100 text-purple-700",
  Delivered: "bg-emerald-100 text-emerald-700",
  Cancelled: "bg-gray-100 text-gray-500",
  Refunded: "bg-red-100 text-red-700",
};

export default function OrdersPage() {
  const { isAuthenticated } = useAuthStore();
  const [orders, setOrders] = useState<OrderSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 10;

  useEffect(() => {
    if (!isAuthenticated) return;
    setLoading(true);
    getOrders({ page, pageSize })
      .then((res) => {
        setOrders(res.items);
        setTotalCount(res.totalCount);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [isAuthenticated, page]);

  if (!isAuthenticated) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500 mb-4">로그인 후 이용할 수 있습니다.</p>
        <Link href="/login" className="text-[var(--color-primary)] hover:underline">로그인하기</Link>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link href="/mypage" className="hover:text-[var(--color-primary)]">마이페이지</Link>
        <span>/</span>
        <span className="text-[var(--color-secondary)] font-medium">주문내역</span>
      </div>

      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">주문내역</h1>

      {orders.length === 0 ? (
        <div className="text-center py-20">
          <Package size={64} className="mx-auto text-gray-300 mb-6" />
          <p className="text-gray-500 mb-4">주문내역이 없습니다.</p>
          <Link
            href="/products"
            className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
          >
            상품 둘러보기
          </Link>
        </div>
      ) : (
        <>
          <div className="space-y-4">
            {orders.map((order) => {
              const statusLabel = ORDER_STATUS_LABELS[order.status as OrderStatusType] || order.status;
              const statusColor = STATUS_COLORS[order.status] || "bg-gray-100 text-gray-500";

              return (
                <Link key={order.id} href={`/order/${order.id}`}>
                  <div className="bg-white rounded-xl shadow-sm p-5 hover:shadow-md transition-shadow flex items-center gap-4">
                    <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-gray-100 shrink-0">
                      {order.firstProductImageUrl ? (
                        <Image src={order.firstProductImageUrl} alt="" fill className="object-cover" sizes="64px" unoptimized />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-2xl opacity-30">📦</div>
                      )}
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColor}`}>
                          {statusLabel}
                        </span>
                        <span className="text-xs text-gray-400">{formatDate(order.createdAt)}</span>
                      </div>
                      <p className="font-medium text-sm text-[var(--color-secondary)] line-clamp-1">
                        {order.firstProductName}
                        {order.itemCount > 1 && ` 외 ${order.itemCount - 1}건`}
                      </p>
                      <p className="text-sm font-bold text-[var(--color-primary)] mt-0.5">
                        {formatPrice(order.totalAmount)}
                      </p>
                    </div>

                    <ChevronRight size={20} className="text-gray-300 shrink-0" />
                  </div>
                </Link>
              );
            })}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex justify-center gap-2 mt-8">
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                <button
                  key={p}
                  onClick={() => setPage(p)}
                  className={`w-9 h-9 rounded-lg text-sm font-medium transition-colors ${
                    p === page
                      ? "bg-[var(--color-primary)] text-white"
                      : "text-gray-500 hover:bg-gray-100"
                  }`}
                >
                  {p}
                </button>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}
