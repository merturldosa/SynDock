"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import { useTranslations } from "next-intl";
import { ChevronLeft, Truck, Clock, Package, RotateCcw, Download } from "lucide-react";
import api from "@/lib/api";
import { updateOrderStatus, updateShippingInfo, refundOrder } from "@/lib/adminApi";
import { downloadOrderReceipt } from "@/lib/orderApi";

interface OrderHistory {
  id: number;
  status: string;
  note: string | null;
  trackingNumber: string | null;
  trackingCarrier: string | null;
  createdBy: string;
  createdAt: string;
}

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
  histories: OrderHistory[] | null;
  trackingNumber: string | null;
  trackingCarrier: string | null;
}

const STATUS_COLORS: Record<string, string> = {
  Pending: "bg-yellow-500",
  Confirmed: "bg-blue-500",
  Processing: "bg-indigo-500",
  Shipped: "bg-purple-500",
  Delivered: "bg-emerald-500",
  Cancelled: "bg-red-500",
  Refunded: "bg-gray-500",
};

import { formatPrice, formatDate as formatDateTime } from "@/lib/format";

export default function AdminOrderDetailPage() {
  const params = useParams();
  const t = useTranslations();
  const id = Number(params.id);
  const [order, setOrder] = useState<OrderDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [trackingNumber, setTrackingNumber] = useState("");
  const [trackingCarrier, setTrackingCarrier] = useState("");
  const [shippingSaving, setShippingSaving] = useState(false);
  const [showRefundModal, setShowRefundModal] = useState(false);
  const [refundReason, setRefundReason] = useState("");
  const [refunding, setRefunding] = useState(false);

  const STATUS_LABELS: Record<string, string> = {
    Pending: t("admin.orders.statusPending"),
    Confirmed: t("admin.orders.statusConfirmed"),
    Processing: t("admin.orders.statusProcessing"),
    Shipped: t("admin.orders.statusShipped"),
    Delivered: t("admin.orders.statusDelivered"),
    Cancelled: t("admin.orders.statusCancelled"),
    Refunded: t("admin.orders.statusRefunded"),
  };

  useEffect(() => {
    api
      .get(`/order/${id}`)
      .then(({ data }) => {
        setOrder(data);
        if (data.trackingNumber) setTrackingNumber(data.trackingNumber);
        if (data.trackingCarrier) setTrackingCarrier(data.trackingCarrier);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [id]);

  const handleStatusChange = async (newStatus: string) => {
    try {
      await updateOrderStatus(id, newStatus);
      // Reload to get updated history
      const { data } = await api.get(`/order/${id}`);
      setOrder(data);
    } catch {
      alert(t("admin.orders.updateFailed"));
    }
  };

  const handleShippingSave = async () => {
    if (!trackingNumber.trim()) {
      alert(t("admin.orders.trackingNumberRequired"));
      return;
    }
    setShippingSaving(true);
    try {
      await updateShippingInfo(id, trackingNumber, trackingCarrier || undefined);
      const { data } = await api.get(`/order/${id}`);
      setOrder(data);
    } catch {
      alert(t("admin.orders.shippingSaveFailed"));
    }
    setShippingSaving(false);
  };

  const handleRefund = async () => {
    if (!refundReason.trim()) {
      alert(t("admin.orders.refundReasonRequired"));
      return;
    }
    setRefunding(true);
    try {
      await refundOrder(id, refundReason);
      const { data } = await api.get(`/order/${id}`);
      setOrder(data);
      setShowRefundModal(false);
      setRefundReason("");
    } catch {
      alert(t("admin.orders.refundFailed"));
    }
    setRefunding(false);
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
        {t("admin.orders.notFound")}
      </div>
    );
  }

  const showShippingForm = order.status === "Processing" || order.status === "Shipped";
  const canRefund = order.status === "Confirmed" || order.status === "Processing" || order.status === "Shipped" || order.status === "Delivered";

  return (
    <div className="max-w-4xl">
      <Link
        href="/admin/orders"
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ChevronLeft size={16} /> {t("admin.orders.orderList")}
      </Link>

      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
            {t("admin.orders.detail")}
          </h1>
          <p className="text-sm text-gray-500 font-mono mt-1">
            {order.orderNumber}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => downloadOrderReceipt(order.id, order.orderNumber)}
            className="flex items-center gap-1 px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-700 hover:bg-gray-50 transition-colors"
          >
            <Download size={14} />
            PDF
          </button>
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
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          {/* Order Items */}
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <div className="p-4 border-b font-medium text-gray-700 flex items-center gap-2">
              <Package size={18} /> {t("admin.orders.orderItems")}
            </div>
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
          <div className="bg-white rounded-xl shadow-sm p-5 space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-500">{t("admin.orders.subtotal")}</span>
              <span>
                {formatPrice(
                  order.items.reduce((sum, item) => sum + item.totalPrice, 0)
                )}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-500">{t("admin.orders.shippingFee")}</span>
              <span>{formatPrice(order.shippingFee)}</span>
            </div>
            {order.discountAmount > 0 && (
              <div className="flex justify-between text-red-500">
                <span>{t("admin.orders.couponDiscount")}</span>
                <span>-{formatPrice(order.discountAmount)}</span>
              </div>
            )}
            {order.pointsUsed > 0 && (
              <div className="flex justify-between text-red-500">
                <span>{t("admin.orders.pointsUsed")}</span>
                <span>-{formatPrice(order.pointsUsed)}</span>
              </div>
            )}
            <div className="flex justify-between pt-2 border-t font-bold text-lg">
              <span>{t("admin.orders.paymentAmount")}</span>
              <span className="text-[var(--color-primary)]">
                {formatPrice(order.totalAmount)}
              </span>
            </div>
          </div>

          {/* Shipping Info Form */}
          {showShippingForm && (
            <div className="bg-white rounded-xl shadow-sm p-5">
              <h3 className="font-medium text-gray-700 mb-3 flex items-center gap-2">
                <Truck size={18} /> {t("admin.orders.shippingInput")}
              </h3>
              <div className="grid grid-cols-2 gap-3 mb-3">
                <div>
                  <label className="block text-xs text-gray-500 mb-1">{t("admin.orders.trackingCarrier")}</label>
                  <select
                    value={trackingCarrier}
                    onChange={(e) => setTrackingCarrier(e.target.value)}
                    className="w-full px-3 py-2 border rounded-lg text-sm"
                  >
                    <option value="">{t("admin.orders.selectCarrier")}</option>
                    <option value="CJ대한통운">CJ대한통운</option>
                    <option value="한진택배">한진택배</option>
                    <option value="롯데택배">롯데택배</option>
                    <option value="로젠택배">로젠택배</option>
                    <option value="우체국택배">우체국택배</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">{t("admin.orders.trackingNumber")}</label>
                  <input
                    type="text"
                    value={trackingNumber}
                    onChange={(e) => setTrackingNumber(e.target.value)}
                    placeholder={t("admin.orders.trackingNumberInput")}
                    className="w-full px-3 py-2 border rounded-lg text-sm"
                  />
                </div>
              </div>
              <button
                onClick={handleShippingSave}
                disabled={shippingSaving}
                className="w-full py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
              >
                {shippingSaving ? t("admin.orders.savingShipping") : t("admin.orders.saveShipping")}
              </button>
            </div>
          )}

          {/* Refund Button */}
          {canRefund && (
            <div className="bg-white rounded-xl shadow-sm p-5">
              <button
                onClick={() => setShowRefundModal(true)}
                className="w-full py-2 border border-red-300 text-red-600 rounded-lg text-sm font-medium hover:bg-red-50 transition-colors flex items-center justify-center gap-2"
              >
                <RotateCcw size={16} /> {t("admin.orders.refundProcess")}
              </button>
            </div>
          )}

          {/* Refund Modal */}
          {showRefundModal && (
            <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
              <div className="bg-white rounded-xl shadow-lg p-6 w-full max-w-md mx-4">
                <h3 className="text-lg font-bold text-[var(--color-secondary)] mb-4">{t("admin.orders.refundTitle")}</h3>
                <p className="text-sm text-gray-500 mb-3">
                  {t("admin.orders.refundDesc", { orderNumber: order.orderNumber })}
                </p>
                <textarea
                  value={refundReason}
                  onChange={(e) => setRefundReason(e.target.value)}
                  placeholder={t("admin.orders.refundReason")}
                  className="w-full px-3 py-2 border rounded-lg text-sm resize-none h-24 mb-4"
                />
                <div className="flex gap-3">
                  <button
                    onClick={handleRefund}
                    disabled={refunding}
                    className="flex-1 py-2 bg-red-500 text-white rounded-lg text-sm font-medium hover:bg-red-600 disabled:opacity-60"
                  >
                    {refunding ? t("admin.orders.refunding") : t("admin.orders.refundConfirm")}
                  </button>
                  <button
                    onClick={() => { setShowRefundModal(false); setRefundReason(""); }}
                    className="flex-1 py-2 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
                  >
                    {t("common.cancel")}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Shipping Address */}
          {order.shippingAddress && (
            <div className="bg-white rounded-xl shadow-sm p-5">
              <h3 className="font-medium text-gray-700 mb-2">{t("admin.orders.shippingAddress")}</h3>
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

          {order.note && (
            <div className="bg-white rounded-xl shadow-sm p-5">
              <h3 className="font-medium text-gray-700 mb-2">{t("admin.orders.orderNote")}</h3>
              <p className="text-sm text-gray-600">{order.note}</p>
            </div>
          )}
        </div>

        {/* Timeline Sidebar */}
        <div className="space-y-6">
          {/* Tracking Info */}
          {order.trackingNumber && (
            <div className="bg-white rounded-xl shadow-sm p-5">
              <h3 className="font-medium text-gray-700 mb-3 flex items-center gap-2">
                <Truck size={18} /> {t("admin.orders.trackingInfo")}
              </h3>
              <div className="text-sm space-y-1">
                {order.trackingCarrier && (
                  <p className="text-gray-600">{t("admin.orders.carrier")}: <strong>{order.trackingCarrier}</strong></p>
                )}
                <p className="text-gray-600">
                  {t("admin.orders.trackingLabel")}: <strong className="font-mono">{order.trackingNumber}</strong>
                </p>
              </div>
            </div>
          )}

          {/* Order Timeline */}
          <div className="bg-white rounded-xl shadow-sm p-5">
            <h3 className="font-medium text-gray-700 mb-4 flex items-center gap-2">
              <Clock size={18} /> {t("admin.orders.orderTimeline")}
            </h3>
            {order.histories && order.histories.length > 0 ? (
              <div className="relative">
                <div className="absolute left-[9px] top-2 bottom-2 w-0.5 bg-gray-200" />
                <div className="space-y-4">
                  {order.histories.map((h) => {
                    const color = STATUS_COLORS[h.status] || "bg-gray-400";
                    return (
                      <div key={h.id} className="relative pl-7">
                        <div
                          className={`absolute left-0 top-1 w-[18px] h-[18px] rounded-full ${color} border-2 border-white shadow`}
                        />
                        <div>
                          <p className="text-sm font-medium text-[var(--color-secondary)]">
                            {STATUS_LABELS[h.status] || h.status}
                          </p>
                          {h.note && (
                            <p className="text-xs text-gray-500 mt-0.5">
                              {h.note}
                            </p>
                          )}
                          {h.trackingNumber && (
                            <p className="text-xs text-purple-600 mt-0.5 font-mono">
                              {t("admin.orders.trackingLabel")}: {h.trackingNumber}
                            </p>
                          )}
                          <p className="text-xs text-gray-400 mt-1">
                            {formatDateTime(h.createdAt)}
                          </p>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ) : (
              <p className="text-sm text-gray-400 text-center py-4">
                {t("admin.orders.noTimeline")}
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
