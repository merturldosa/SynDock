"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getAdminOrders, updateOrderStatus, type OrderSummary, type PagedOrders } from "@/lib/adminApi";

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  Pending: { label: "결제 대기", color: "bg-yellow-100 text-yellow-700" },
  Confirmed: { label: "결제 완료", color: "bg-blue-100 text-blue-700" },
  Processing: { label: "처리 중", color: "bg-indigo-100 text-indigo-700" },
  Shipped: { label: "배송 중", color: "bg-purple-100 text-purple-700" },
  Delivered: { label: "배송 완료", color: "bg-emerald-100 text-emerald-700" },
  Cancelled: { label: "취소", color: "bg-red-100 text-red-700" },
  Refunded: { label: "환불", color: "bg-gray-100 text-gray-700" },
};

const STATUS_OPTIONS = ["Pending", "Confirmed", "Processing", "Shipped", "Delivered"];

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

export default function AdminOrdersPage() {
  const [data, setData] = useState<PagedOrders | null>(null);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState("");
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    getAdminOrders(statusFilter || undefined, page)
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [page, statusFilter]);

  const handleStatusChange = async (orderId: number, newStatus: string) => {
    try {
      await updateOrderStatus(orderId, newStatus);
      load();
    } catch {
      alert("상태 변경에 실패했습니다.");
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        주문 관리
      </h1>

      {/* Status Filter */}
      <div className="flex gap-2 mb-4 flex-wrap">
        <button
          onClick={() => { setStatusFilter(""); setPage(1); }}
          className={`px-3 py-1.5 text-sm rounded-full border ${
            !statusFilter ? "bg-[var(--color-secondary)] text-white" : "hover:bg-gray-50"
          }`}
        >
          전체
        </button>
        {Object.entries(STATUS_LABELS).map(([key, { label }]) => (
          <button
            key={key}
            onClick={() => { setStatusFilter(key); setPage(1); }}
            className={`px-3 py-1.5 text-sm rounded-full border ${
              statusFilter === key ? "bg-[var(--color-secondary)] text-white" : "hover:bg-gray-50"
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {data && (
        <p className="text-sm text-gray-500 mb-3">
          총 {data.totalCount.toLocaleString()}건
        </p>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>주문이 없습니다.</p>
        </div>
      ) : (
        <>
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="text-left p-3 font-medium text-gray-500">주문번호</th>
                  <th className="text-left p-3 font-medium text-gray-500">상품</th>
                  <th className="text-right p-3 font-medium text-gray-500">금액</th>
                  <th className="text-center p-3 font-medium text-gray-500">상태</th>
                  <th className="text-left p-3 font-medium text-gray-500">주문일</th>
                  <th className="text-center p-3 font-medium text-gray-500">상태 변경</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((order) => {
                  const status = STATUS_LABELS[order.status] || { label: order.status, color: "bg-gray-100 text-gray-700" };
                  return (
                    <tr key={order.id} className="border-b last:border-0 hover:bg-gray-50">
                      <td className="p-3">
                        <Link href={`/admin/orders/${order.id}`} className="text-[var(--color-primary)] hover:underline font-mono text-xs">
                          {order.orderNumber}
                        </Link>
                      </td>
                      <td className="p-3">
                        <p className="line-clamp-1">
                          {order.firstProductName || "상품"}
                          {order.itemCount > 1 && ` 외 ${order.itemCount - 1}건`}
                        </p>
                      </td>
                      <td className="p-3 text-right font-medium">{formatPrice(order.totalAmount)}</td>
                      <td className="p-3 text-center">
                        <span className={`px-2 py-0.5 text-xs rounded-full ${status.color}`}>
                          {status.label}
                        </span>
                      </td>
                      <td className="p-3 text-gray-500 text-xs">
                        {new Date(order.createdAt).toLocaleDateString("ko-KR")}
                      </td>
                      <td className="p-3 text-center">
                        <select
                          value={order.status}
                          onChange={(e) => handleStatusChange(order.id, e.target.value)}
                          className="text-xs border rounded px-2 py-1"
                        >
                          {STATUS_OPTIONS.map((s) => (
                            <option key={s} value={s}>
                              {STATUS_LABELS[s]?.label || s}
                            </option>
                          ))}
                        </select>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {data.totalCount > 20 && (
            <div className="flex items-center justify-center gap-2 mt-6">
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">
                이전
              </button>
              <span className="text-sm text-gray-500">{page} / {Math.ceil(data.totalCount / 20)}</span>
              <button onClick={() => setPage((p) => p + 1)} disabled={page * 20 >= data.totalCount} className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40">
                다음
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
