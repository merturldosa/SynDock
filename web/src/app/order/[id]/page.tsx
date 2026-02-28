"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { Package, MapPin, ArrowLeft } from "lucide-react";
import { getOrderById, cancelOrder } from "@/lib/orderApi";
import { useAuthStore } from "@/stores/authStore";
import type { Order } from "@/types/order";
import { ORDER_STATUS_LABELS, type OrderStatusType } from "@/types/order";

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("ko-KR", {
    year: "numeric", month: "long", day: "numeric", hour: "2-digit", minute: "2-digit",
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

export default function OrderDetailPage() {
  const params = useParams();
  const { isAuthenticated } = useAuthStore();
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    const id = Number(params.id);
    if (!id || !isAuthenticated) return;
    setLoading(true);
    getOrderById(id)
      .then(setOrder)
      .catch(() => setOrder(null))
      .finally(() => setLoading(false));
  }, [params.id, isAuthenticated]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!order) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500 mb-4">주문을 찾을 수 없습니다.</p>
        <Link href="/mypage/orders" className="text-[var(--color-primary)] hover:underline">주문내역 보기</Link>
      </div>
    );
  }

  const canCancel = order.status === "Pending" || order.status === "Confirmed";
  const statusLabel = ORDER_STATUS_LABELS[order.status as OrderStatusType] || order.status;
  const statusColor = STATUS_COLORS[order.status] || "bg-gray-100 text-gray-500";

  const handleCancel = async () => {
    if (!confirm("주문을 취소하시겠습니까?")) return;
    setCancelling(true);
    try {
      await cancelOrder(order.id);
      setOrder({ ...order, status: "Cancelled" });
    } catch {
      alert("주문 취소에 실패했습니다.");
    }
    setCancelling(false);
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <Link href="/mypage/orders" className="flex items-center gap-1 text-sm text-gray-500 hover:text-[var(--color-secondary)] mb-6">
        <ArrowLeft size={16} /> 주문내역
      </Link>

      {/* Order header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-secondary)]">주문 상세</h1>
          <p className="text-sm text-gray-500 mt-1">주문번호: {order.orderNumber}</p>
          <p className="text-sm text-gray-400">{formatDate(order.createdAt)}</p>
        </div>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColor}`}>
          {statusLabel}
        </span>
      </div>

      {/* Items */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] flex items-center gap-2 mb-4">
          <Package size={18} /> 주문 상품
        </h2>
        <div className="space-y-3">
          {order.items.map((item) => (
            <div key={item.id} className="flex gap-3 py-3 border-b border-gray-50 last:border-0">
              <Link href={`/products/${item.productId}`} className="shrink-0">
                <div className="relative w-20 h-20 rounded-lg overflow-hidden bg-gray-100">
                  {item.primaryImageUrl ? (
                    <Image src={item.primaryImageUrl} alt={item.productName} fill className="object-cover" sizes="80px" unoptimized />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-3xl opacity-30">📦</div>
                  )}
                </div>
              </Link>
              <div className="flex-1 min-w-0">
                <Link href={`/products/${item.productId}`} className="text-sm font-medium text-[var(--color-secondary)] hover:text-[var(--color-primary)] transition-colors line-clamp-1">
                  {item.productName}
                </Link>
                {item.variantName && <p className="text-xs text-gray-500">{item.variantName}</p>}
                <p className="text-xs text-gray-400 mt-0.5">{formatPrice(item.unitPrice)} x {item.quantity}개</p>
              </div>
              <p className="text-sm font-bold text-[var(--color-secondary)] shrink-0">{formatPrice(item.totalPrice)}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Shipping */}
      {order.shippingAddress && (
        <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
          <h2 className="font-bold text-[var(--color-secondary)] flex items-center gap-2 mb-3">
            <MapPin size={18} /> 배송지 정보
          </h2>
          <div className="text-sm text-gray-600 space-y-1">
            <p className="font-medium">{order.shippingAddress.recipientName}</p>
            <p>{order.shippingAddress.phone}</p>
            <p>[{order.shippingAddress.zipCode}] {order.shippingAddress.address1} {order.shippingAddress.address2}</p>
          </div>
        </section>
      )}

      {order.note && (
        <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
          <h2 className="font-bold text-[var(--color-secondary)] mb-2">배송 메모</h2>
          <p className="text-sm text-gray-600">{order.note}</p>
        </section>
      )}

      {/* Summary */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <div className="space-y-3 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-500">상품 금액</span>
            <span>{formatPrice(order.totalAmount)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">배송비</span>
            <span>{order.shippingFee > 0 ? formatPrice(order.shippingFee) : "무료"}</span>
          </div>
          <div className="border-t pt-3 flex justify-between">
            <span className="font-bold text-[var(--color-secondary)]">총 결제금액</span>
            <span className="font-bold text-lg text-[var(--color-primary)]">{formatPrice(order.totalAmount + order.shippingFee)}</span>
          </div>
        </div>
      </section>

      {/* Cancel */}
      {canCancel && (
        <button
          onClick={handleCancel}
          disabled={cancelling}
          className="w-full py-3 border border-red-300 text-red-500 rounded-xl font-medium hover:bg-red-50 transition-colors disabled:opacity-60"
        >
          {cancelling ? "취소 처리 중..." : "주문 취소"}
        </button>
      )}
    </div>
  );
}
