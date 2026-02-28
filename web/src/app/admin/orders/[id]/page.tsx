"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import { ChevronLeft } from "lucide-react";
import api from "@/lib/api";
import { updateOrderStatus } from "@/lib/adminApi";

interface OrderDetail {
  id: number;
  orderNumber: string;
  status: string;
  items: {
    id: number;
    productId: number;
    productName: string;
    primaryImageUrl: string | null;
    variantId: number | null;
    variantName: string | null;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
  }[];
  totalAmount: number;
  shippingFee: number;
  discountAmount: number;
  pointsUsed: number;
  note: string | null;
  shippingAddress: {
    recipientName: string;
    phone: string;
    zipCode: string;
    address1: string;
    address2: string | null;
  } | null;
  createdAt: string;
}

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

export default function AdminOrderDetailPage() {
  const params = useParams();
  const id = Number(params.id);
  const [order, setOrder] = useState<OrderDetail | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get(`/order/${id}`)
      .then(({ data }) => setOrder(data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [id]);

  const handleStatusChange = async (newStatus: string) => {
    try {
      await updateOrderStatus(id, newStatus);
      setOrder((prev) => (prev ? { ...prev, status: newStatus } : null));
    } catch {
      alert("상태 변경에 실패했습니다.");
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!order) {
    return (
      <div className="text-center py-20 text-gray-400">
        주문을 찾을 수 없습니다.
      </div>
    );
  }

  return (
    <div className="max-w-3xl">
      <Link
        href="/admin/orders"
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ChevronLeft size={16} /> 주문 목록
      </Link>

      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
            주문 상세
          </h1>
          <p className="text-sm text-gray-500 font-mono mt-1">
            {order.orderNumber}
          </p>
        </div>
        <select
          value={order.status}
          onChange={(e) => handleStatusChange(e.target.value)}
          className="border rounded-lg px-3 py-2 text-sm"
        >
          {Object.entries(STATUS_LABELS).map(([key, label]) => (
            <option key={key} value={key}>
              {label}
            </option>
          ))}
        </select>
      </div>

      {/* Order Items */}
      <div className="bg-white rounded-xl shadow-sm mb-6 overflow-hidden">
        <div className="p-4 border-b font-medium text-gray-700">주문 상품</div>
        {order.items.map((item) => (
          <div
            key={item.id}
            className="flex items-center gap-4 p-4 border-b last:border-0"
          >
            <div className="w-14 h-14 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0">
              {item.primaryImageUrl ? (
                <Image
                  src={item.primaryImageUrl}
                  alt={item.productName}
                  width={56}
                  height={56}
                  className="object-cover w-full h-full"
                  unoptimized
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center text-lg opacity-20">
                  📦
                </div>
              )}
            </div>
            <div className="flex-1 min-w-0">
              <p className="font-medium text-[var(--color-secondary)] line-clamp-1">
                {item.productName}
              </p>
              {item.variantName && (
                <p className="text-xs text-gray-400">{item.variantName}</p>
              )}
              <p className="text-sm text-gray-500">
                {formatPrice(item.unitPrice)} x {item.quantity}
              </p>
            </div>
            <p className="font-medium">{formatPrice(item.totalPrice)}</p>
          </div>
        ))}
      </div>

      {/* Summary */}
      <div className="bg-white rounded-xl shadow-sm p-5 mb-6 space-y-2 text-sm">
        <div className="flex justify-between">
          <span className="text-gray-500">상품 합계</span>
          <span>
            {formatPrice(
              order.items.reduce((sum, item) => sum + item.totalPrice, 0)
            )}
          </span>
        </div>
        <div className="flex justify-between">
          <span className="text-gray-500">배송비</span>
          <span>{formatPrice(order.shippingFee)}</span>
        </div>
        {order.discountAmount > 0 && (
          <div className="flex justify-between text-red-500">
            <span>쿠폰 할인</span>
            <span>-{formatPrice(order.discountAmount)}</span>
          </div>
        )}
        {order.pointsUsed > 0 && (
          <div className="flex justify-between text-red-500">
            <span>포인트 사용</span>
            <span>-{formatPrice(order.pointsUsed)}</span>
          </div>
        )}
        <div className="flex justify-between pt-2 border-t font-bold text-lg">
          <span>결제 금액</span>
          <span className="text-[var(--color-primary)]">
            {formatPrice(order.totalAmount)}
          </span>
        </div>
      </div>

      {/* Shipping Address */}
      {order.shippingAddress && (
        <div className="bg-white rounded-xl shadow-sm p-5 mb-6">
          <h3 className="font-medium text-gray-700 mb-2">배송지 정보</h3>
          <div className="text-sm space-y-1 text-gray-600">
            <p>
              {order.shippingAddress.recipientName} /{" "}
              {order.shippingAddress.phone}
            </p>
            <p>
              ({order.shippingAddress.zipCode}){" "}
              {order.shippingAddress.address1}
            </p>
            {order.shippingAddress.address2 && (
              <p>{order.shippingAddress.address2}</p>
            )}
          </div>
        </div>
      )}

      {/* Note */}
      {order.note && (
        <div className="bg-white rounded-xl shadow-sm p-5">
          <h3 className="font-medium text-gray-700 mb-2">주문 메모</h3>
          <p className="text-sm text-gray-600">{order.note}</p>
        </div>
      )}
    </div>
  );
}
