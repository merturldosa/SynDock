"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { Package, MapPin, ArrowLeft, Truck, Clock, Search, Download } from "lucide-react";
import toast from "react-hot-toast";
import { useTranslations } from "next-intl";
import { getOrderById, cancelOrder, getShippingTracking, downloadOrderReceipt, type TrackingEvent } from "@/lib/orderApi";
import { useAuthStore } from "@/stores/authStore";
import type { Order } from "@/types/order";

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
  discountAmount?: number;
  pointsUsed?: number;
  histories?: OrderHistory[];
  trackingNumber?: string | null;
  trackingCarrier?: string | null;
}

import { formatPrice, formatDate, formatDateShort } from "@/lib/format";

const STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  Confirmed: "bg-blue-100 text-blue-700",
  Processing: "bg-indigo-100 text-indigo-700",
  Shipped: "bg-purple-100 text-purple-700",
  Delivered: "bg-emerald-100 text-emerald-700",
  Cancelled: "bg-gray-100 text-gray-500",
  Refunded: "bg-red-100 text-red-700",
};

const PROGRESS_STEP_KEYS = ["Pending", "Confirmed", "Processing", "Shipped", "Delivered"] as const;

function getProgressIndex(status: string): number {
  const idx = PROGRESS_STEP_KEYS.indexOf(status as typeof PROGRESS_STEP_KEYS[number]);
  return idx >= 0 ? idx : -1;
}

export default function MypageOrderDetailPage() {
  const t = useTranslations();
  const params = useParams();
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

  if (!isAuthenticated) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500 mb-4">{t("cart.loginRequired")}</p>
        <Link href="/login" className="text-[var(--color-primary)] hover:underline">{t("cart.loginAction")}</Link>
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

  if (!order) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500 mb-4">{t("mypage.orders.notFound")}</p>
        <Link href="/mypage/orders" className="text-[var(--color-primary)] hover:underline">{t("mypage.orders.viewOrders")}</Link>
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
        toast.error(result.error || t("mypage.orders.trackingFailed"));
      }
    } catch {
      toast.error(t("mypage.orders.trackingFailed"));
    }
    setTrackingLoading(false);
  };

  const canCancel = order.status === "Pending" || order.status === "Confirmed";
  const isCancelled = order.status === "Cancelled" || order.status === "Refunded";
  const statusLabel = t(`order.status.${order.status}`);
  const statusColor = STATUS_COLORS[order.status] || "bg-gray-100 text-gray-500";
  const progressIndex = getProgressIndex(order.status);

  const handleCancel = async () => {
    if (!window.confirm(t("mypage.orders.cancelConfirm"))) return;
    setCancelling(true);
    try {
      await cancelOrder(order.id);
      setOrder({ ...order, status: "Cancelled" });
    } catch {
      toast.error(t("mypage.orders.cancelFailed"));
    }
    setCancelling(false);
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link href="/mypage" className="hover:text-[var(--color-primary)]">{t("mypage.title")}</Link>
        <span>/</span>
        <Link href="/mypage/orders" className="hover:text-[var(--color-primary)]">{t("mypage.orders.title")}</Link>
        <span>/</span>
        <span className="text-[var(--color-secondary)] font-medium">{t("mypage.orders.detail")}</span>
      </div>

      {/* Order Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-secondary)]">{t("mypage.orders.detail")}</h1>
          <p className="text-sm text-gray-500 mt-1">{t("mypage.orders.orderNumber")}: {order.orderNumber}</p>
          <p className="text-sm text-gray-400">{formatDate(order.createdAt)}</p>
        </div>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColor}`}>
          {statusLabel}
        </span>
      </div>

      {/* Cancelled/Refunded Warning */}
      {isCancelled && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-4 mb-6">
          <p className="text-red-700 font-medium">
            {order.status === "Cancelled" ? t("mypage.orders.cancelledNotice") : t("mypage.orders.refundedNotice")}
          </p>
        </div>
      )}

      {/* Progress Bar */}
      {!isCancelled && progressIndex >= 0 && (
        <div className="bg-white rounded-xl shadow-sm p-6 mb-6">
          <div className="flex items-center justify-between">
            {PROGRESS_STEP_KEYS.map((stepKey, i) => {
              const isActive = i <= progressIndex;
              const isCurrent = i === progressIndex;
              return (
                <div key={stepKey} className="flex-1 flex flex-col items-center relative">
                  {i > 0 && (
                    <div className={`absolute top-4 right-1/2 w-full h-0.5 -z-0 ${
                      i <= progressIndex ? "bg-[var(--color-primary)]" : "bg-gray-200"
                    }`} />
                  )}
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold z-10 ${
                    isCurrent
                      ? "bg-[var(--color-primary)] text-white ring-4 ring-[var(--color-primary)]/20"
                      : isActive
                        ? "bg-[var(--color-primary)] text-white"
                        : "bg-gray-200 text-gray-400"
                  }`}>
                    {i + 1}
                  </div>
                  <span className={`text-xs mt-2 font-medium ${
                    isActive ? "text-[var(--color-primary)]" : "text-gray-400"
                  }`}>
                    {t(`mypage.orders.status.${stepKey}`)}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Tracking Info */}
      {order.trackingNumber && (
        <section className="bg-purple-50 border border-purple-200 rounded-xl p-5 mb-6">
          <div className="flex items-center justify-between mb-2">
            <h2 className="font-bold text-purple-800 flex items-center gap-2">
              <Truck size={18} /> {t("mypage.orders.tracking")}
            </h2>
            <button
              onClick={handleTrackShipping}
              disabled={trackingLoading}
              className="flex items-center gap-1 px-3 py-1.5 text-sm bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-60 transition-colors"
            >
              <Search size={14} />
              {trackingLoading ? t("mypage.orders.tracking_loading") : t("mypage.orders.trackRealtime")}
            </button>
          </div>
          <div className="text-sm text-purple-700 space-y-1">
            {order.trackingCarrier && (
              <p>{t("mypage.orders.carrier")}: <strong>{order.trackingCarrier}</strong></p>
            )}
            <p>{t("mypage.orders.trackingNumber")}: <strong className="font-mono">{order.trackingNumber}</strong></p>
            {trackingStatus && (
              <p className="mt-2 font-medium text-purple-900">{t("mypage.orders.currentStatus")}: {trackingStatus}</p>
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
            <Clock size={18} /> {t("mypage.orders.timeline")}
          </h2>
          <div className="relative">
            <div className="absolute left-[9px] top-2 bottom-2 w-0.5 bg-gray-200" />
            <div className="space-y-4">
              {order.histories.map((h) => (
                <div key={h.id} className="relative pl-7">
                  <div className={`absolute left-0 top-1 w-[18px] h-[18px] rounded-full border-2 border-white shadow ${
                    h.status === "Cancelled" || h.status === "Refunded" ? "bg-red-500" : "bg-[var(--color-primary)]"
                  }`} />
                  <div>
                    <p className="text-sm font-medium text-[var(--color-secondary)]">
                      {t(`order.status.${h.status}`)}
                    </p>
                    {h.note && <p className="text-xs text-gray-500 mt-0.5">{h.note}</p>}
                    {h.trackingNumber && (
                      <p className="text-xs text-purple-600 mt-0.5 font-mono">
                        {t("mypage.orders.trackingNumber")}: {h.trackingNumber}
                      </p>
                    )}
                    <p className="text-xs text-gray-400 mt-1">{formatDate(h.createdAt)}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* Items */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] flex items-center gap-2 mb-4">
          <Package size={18} /> {t("mypage.orders.orderItems")}
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
                <p className="text-xs text-gray-400 mt-0.5">{formatPrice(item.unitPrice)} x {t("common.quantity", { count: item.quantity })}</p>
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
            <MapPin size={18} /> {t("mypage.orders.shippingInfo")}
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
          <h2 className="font-bold text-[var(--color-secondary)] mb-2">{t("mypage.orders.shippingMemo")}</h2>
          <p className="text-sm text-gray-600">{order.note}</p>
        </section>
      )}

      {/* Summary */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <div className="space-y-3 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-500">{t("cart.subtotal")}</span>
            <span>{formatPrice(order.totalAmount)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">{t("cart.shippingFee")}</span>
            <span>{order.shippingFee > 0 ? formatPrice(order.shippingFee) : t("common.free")}</span>
          </div>
          {(order.discountAmount ?? 0) > 0 && (
            <div className="flex justify-between text-red-500">
              <span>{t("mypage.coupons.discount")}</span>
              <span>-{formatPrice(order.discountAmount ?? 0)}</span>
            </div>
          )}
          {(order.pointsUsed ?? 0) > 0 && (
            <div className="flex justify-between text-orange-500">
              <span>{t("order.pointsUsed")}</span>
              <span>-{formatPrice(order.pointsUsed ?? 0)}</span>
            </div>
          )}
          <div className="border-t pt-3 flex justify-between">
            <span className="font-bold text-[var(--color-secondary)]">{t("cart.totalAmount")}</span>
            <span className="font-bold text-lg text-[var(--color-primary)]">
              {formatPrice(order.totalAmount + order.shippingFee - (order.discountAmount ?? 0) - (order.pointsUsed ?? 0))}
            </span>
          </div>
        </div>
      </section>

      {/* Actions */}
      <div className="flex gap-3">
        <button
          onClick={() => downloadOrderReceipt(order.id, order.orderNumber)}
          className="flex-1 py-3 flex items-center justify-center gap-2 border border-gray-300 text-gray-700 rounded-xl font-medium hover:bg-gray-50 transition-colors"
        >
          <Download size={16} />
          {t("mypage.orders.downloadReceipt")}
        </button>
        {canCancel && (
          <button
            onClick={handleCancel}
            disabled={cancelling}
            className="flex-1 py-3 border border-red-300 text-red-500 rounded-xl font-medium hover:bg-red-50 transition-colors disabled:opacity-60"
          >
            {cancelling ? t("mypage.orders.cancelling") : t("mypage.orders.cancelOrder")}
          </button>
        )}
      </div>
    </div>
  );
}
