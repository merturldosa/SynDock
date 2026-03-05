"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import Image from "next/image";
import Link from "next/link";
import { Package, MapPin, ArrowLeft, Truck, Clock, Search } from "lucide-react";
import toast from "react-hot-toast";
import { getOrderById, cancelOrder, getShippingTracking, type TrackingEvent } from "@/lib/orderApi";
import { useAuthStore } from "@/stores/authStore";
import type { Order } from "@/types/order";
import { ORDER_STATUS_LABELS, type OrderStatusType } from "@/types/order";

interface OrderHistory {
  id: number;
  status: string;
  note: string | null;
  trackingNumber: string | null;
  trackingCarrier: string | null;
  createdBy: string;
  createdAt: string;
}

interface ExtendedOrder extends Order {
  histories?: OrderHistory[];
  trackingNumber?: string | null;
  trackingCarrier?: string | null;
}

import { formatPrice, formatDate } from "@/lib/format";

const STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  Confirmed: "bg-blue-100 text-blue-700",
  Processing: "bg-indigo-100 text-indigo-700",
  Shipped: "bg-purple-100 text-purple-700",
  Delivered: "bg-emerald-100 text-emerald-700",
  Cancelled: "bg-gray-100 text-gray-500",
  Refunded: "bg-red-100 text-red-700",
};

const TIMELINE_COLORS: Record<string, string> = {
  Pending: "bg-yellow-500",
  Confirmed: "bg-blue-500",
  Processing: "bg-indigo-500",
  Shipped: "bg-purple-500",
  Delivered: "bg-emerald-500",
  Cancelled: "bg-red-500",
  Refunded: "bg-gray-500",
};

export default function OrderDetailPage() {
  const params = useParams();
  const t = useTranslations();
  const { isAuthenticated } = useAuthStore();
  const [order, setOrder] = useState<ExtendedOrder | null>(null);
  const [loading, setLoading] = useState(true);
  const [cancelling, setCancelling] = useState(false);
  const [trackingEvents, setTrackingEvents] = useState<TrackingEvent[] | null>(null);
  const [trackingLoading, setTrackingLoading] = useState(false);
  const [trackingStatus, setTrackingStatus] = useState<string | null>(null);

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
        <p className="text-gray-500 mb-4">{t("order.detail.notFound")}</p>
        <Link href="/mypage/orders" className="text-[var(--color-primary)] hover:underline">{t("mypage.orders.title")}</Link>
      </div>
    );
  }

  const handleTrackShipping = async () => {
    setTrackingLoading(true);
    try {
      const result = await getShippingTracking(order.id);
      if (result.isSuccess) {
        setTrackingEvents(result.events);
        setTrackingStatus(result.currentStatus);
      } else {
        toast.error(result.error || t("order.detail.trackingFailed"));
      }
    } catch {
      toast.error(t("order.detail.trackingFailed"));
    }
    setTrackingLoading(false);
  };

  const canCancel = order.status === "Pending" || order.status === "Confirmed";
  const statusLabel = ORDER_STATUS_LABELS[order.status as OrderStatusType] || order.status;
  const statusColor = STATUS_COLORS[order.status] || "bg-gray-100 text-gray-500";

  const handleCancel = async () => {
    if (!window.confirm(t("order.detail.cancelConfirm"))) return;
    setCancelling(true);
    try {
      await cancelOrder(order.id);
      setOrder({ ...order, status: "Cancelled" });
    } catch {
      toast.error(t("order.detail.cancelFailed"));
    }
    setCancelling(false);
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <Link href="/mypage/orders" className="flex items-center gap-1 text-sm text-gray-500 hover:text-[var(--color-secondary)] mb-6">
        <ArrowLeft size={16} /> {t("mypage.orders.title")}
      </Link>

      {/* Order header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-secondary)]">{t("order.detail.title")}</h1>
          <p className="text-sm text-gray-500 mt-1">{t("order.detail.orderNumber")}: {order.orderNumber}</p>
          <p className="text-sm text-gray-400">{formatDate(order.createdAt)}</p>
        </div>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColor}`}>
          {statusLabel}
        </span>
      </div>

      {/* Tracking Info */}
      {order.trackingNumber && (
        <section className="bg-purple-50 border border-purple-200 rounded-xl p-5 mb-6">
          <div className="flex items-center justify-between mb-2">
            <h2 className="font-bold text-purple-800 flex items-center gap-2">
              <Truck size={18} /> {t("order.detail.tracking")}
            </h2>
            <button
              onClick={handleTrackShipping}
              disabled={trackingLoading}
              className="flex items-center gap-1 px-3 py-1.5 text-sm bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-60 transition-colors"
            >
              <Search size={14} />
              {trackingLoading ? t("order.detail.trackingLoading") : t("order.detail.trackRealtime")}
            </button>
          </div>
          <div className="text-sm text-purple-700 space-y-1">
            {order.trackingCarrier && (
              <p>{t("order.detail.carrier")}: <strong>{order.trackingCarrier}</strong></p>
            )}
            <p>{t("order.detail.trackingNumber")}: <strong className="font-mono">{order.trackingNumber}</strong></p>
            {trackingStatus && (
              <p className="mt-2 font-medium text-purple-900">{t("order.detail.currentStatus")}: {trackingStatus}</p>
            )}
          </div>

          {/* Tracking Events Timeline */}
          {trackingEvents && trackingEvents.length > 0 && (
            <div className="mt-4 border-t border-purple-200 pt-4">
              <div className="relative">
                <div className="absolute left-[9px] top-2 bottom-2 w-0.5 bg-purple-200" />
                <div className="space-y-3">
                  {trackingEvents.map((event, idx) => (
                    <div key={idx} className="relative pl-7">
                      <div className={`absolute left-0 top-1 w-[18px] h-[18px] rounded-full border-2 border-white shadow ${idx === 0 ? "bg-purple-600" : "bg-purple-300"}`} />
                      <div>
                        <p className="text-sm font-medium text-purple-900">{event.status}</p>
                        {event.description && <p className="text-xs text-purple-600">{event.description}</p>}
                        <p className="text-xs text-purple-400 mt-0.5">
                          {formatDate(event.time)}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </section>
      )}

      {/* Order Timeline */}
      {order.histories && order.histories.length > 0 && (
        <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
          <h2 className="font-bold text-[var(--color-secondary)] flex items-center gap-2 mb-4">
            <Clock size={18} /> {t("order.detail.timeline")}
          </h2>
          <div className="relative">
            <div className="absolute left-[9px] top-2 bottom-2 w-0.5 bg-gray-200" />
            <div className="space-y-4">
              {order.histories.map((h) => {
                const color = TIMELINE_COLORS[h.status] || "bg-gray-400";
                return (
                  <div key={h.id} className="relative pl-7">
                    <div className={`absolute left-0 top-1 w-[18px] h-[18px] rounded-full ${color} border-2 border-white shadow`} />
                    <div>
                      <p className="text-sm font-medium text-[var(--color-secondary)]">
                        {ORDER_STATUS_LABELS[h.status as OrderStatusType] || h.status}
                      </p>
                      {h.note && (
                        <p className="text-xs text-gray-500 mt-0.5">{h.note}</p>
                      )}
                      {h.trackingNumber && (
                        <p className="text-xs text-purple-600 mt-0.5 font-mono">
                          {t("order.detail.trackingNumber")}: {h.trackingNumber}
                        </p>
                      )}
                      <p className="text-xs text-gray-400 mt-1">{formatDate(h.createdAt)}</p>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </section>
      )}

      {/* Items */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] flex items-center gap-2 mb-4">
          <Package size={18} /> {t("order.detail.items")}
        </h2>
        <div className="space-y-3">
          {order.items.map((item) => (
            <div key={item.id} className="flex gap-3 py-3 border-b border-gray-50 last:border-0">
              <Link href={`/products/${item.productId}`} className="shrink-0">
                <div className="relative w-20 h-20 rounded-lg overflow-hidden bg-gray-100">
                  {item.primaryImageUrl ? (
                    <Image src={item.primaryImageUrl} alt={item.productName} fill className="object-cover" sizes="80px" />
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
            <MapPin size={18} /> {t("order.detail.shippingInfo")}
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
          <h2 className="font-bold text-[var(--color-secondary)] mb-2">{t("order.shippingMemo")}</h2>
          <p className="text-sm text-gray-600">{order.note}</p>
        </section>
      )}

      {/* Summary */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <div className="space-y-3 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-500">{t("order.subtotal")}</span>
            <span>{formatPrice(order.totalAmount)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">{t("order.shippingFee")}</span>
            <span>{order.shippingFee > 0 ? formatPrice(order.shippingFee) : t("common.free")}</span>
          </div>
          <div className="border-t pt-3 flex justify-between">
            <span className="font-bold text-[var(--color-secondary)]">{t("order.totalAmount")}</span>
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
          {cancelling ? t("order.detail.cancelling") : t("order.detail.cancelOrder")}
        </button>
      )}
    </div>
  );
}
