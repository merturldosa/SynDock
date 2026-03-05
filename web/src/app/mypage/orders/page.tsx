"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import toast from "react-hot-toast";
import { Package, ChevronRight } from "lucide-react";
import { useTranslations } from "next-intl";
import { getOrders } from "@/lib/orderApi";
import { useAuthStore } from "@/stores/authStore";
import type { OrderSummary } from "@/types/order";
import { ORDER_STATUS_LABELS, type OrderStatusType } from "@/types/order";
import { formatPrice, formatDateShort as formatDate } from "@/lib/format";

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
  const t = useTranslations();
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
      .catch(() => toast.error(t("common.fetchError")))
      .finally(() => setLoading(false));
  }, [isAuthenticated, page]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">{t("mypage.orders.title")}</h1>

      {orders.length === 0 ? (
        <div className="text-center py-20">
          <Package size={64} className="mx-auto text-gray-300 mb-6" />
          <p className="text-gray-500 mb-4">{t("mypage.orders.empty")}</p>
          <Link
            href="/products"
            className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
          >
            {t("mypage.orders.browseProducts")}
          </Link>
        </div>
      ) : (
        <>
          <div className="space-y-4">
            {orders.map((order) => {
              const statusLabel = ORDER_STATUS_LABELS[order.status as OrderStatusType] || order.status;
              const statusColor = STATUS_COLORS[order.status] || "bg-gray-100 text-gray-500";

              return (
                <Link key={order.id} href={`/mypage/orders/${order.id}`}>
                  <div className="bg-white rounded-xl shadow-sm p-5 hover:shadow-md transition-shadow flex items-center gap-4">
                    <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-gray-100 shrink-0">
                      {order.firstProductImageUrl ? (
                        <Image src={order.firstProductImageUrl} alt="" fill className="object-cover" sizes="64px" />
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
                        {order.itemCount > 1 && ` ${t("mypage.orders.andMore", { count: order.itemCount - 1 })}`}
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
