"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import Image from "next/image";
import Link from "next/link";
import Script from "next/script";
import { ShoppingCart, MapPin, Plus, Tag, Coins, Check, CreditCard, Banknote } from "lucide-react";
import toast from "react-hot-toast";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";
import { createOrder, getAddresses, createAddress, getPaymentClientKey } from "@/lib/orderApi";
import { CheckoutSteps } from "@/components/checkout/CheckoutSteps";
import { getMyCoupons, validateCoupon } from "@/lib/couponApi";
import { getPointBalance } from "@/lib/pointApi";
import { formatPrice } from "@/lib/format";
import type { Address } from "@/types/order";

export default function OrderPage() {
  const router = useRouter();
  const t = useTranslations();
  const { cart, isLoading: cartLoading, fetchCart } = useCartStore();
  const { isAuthenticated } = useAuthStore();
  const [addresses, setAddresses] = useState<Address[]>([]);
  const [selectedAddressId, setSelectedAddressId] = useState<number | null>(null);
  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [showNewAddress, setShowNewAddress] = useState(false);
  const [newAddress, setNewAddress] = useState({
    recipientName: "",
    phone: "",
    zipCode: "",
    address1: "",
    address2: "",
    isDefault: false,
  });

  // Coupon
  const [couponCode, setCouponCode] = useState("");
  const [appliedCoupon, setAppliedCoupon] = useState<{ code: string; name: string; discount: number } | null>(null);
  const [couponError, setCouponError] = useState("");
  const [myCoupons, setMyCoupons] = useState<Array<{ code: string; couponName: string; discountType: string; discountValue: number }>>([]);

  // Points
  const [pointBalance, setPointBalance] = useState(0);
  const [pointsToUse, setPointsToUse] = useState(0);

  // Payment
  const [paymentProvider, setPaymentProvider] = useState<string>("Mock");
  const [paymentClientKey, setPaymentClientKey] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      sessionStorage.setItem("returnTo", "/order");
      router.push("/login");
      return;
    }
    fetchCart();
    getAddresses().then((addrs) => {
      setAddresses(addrs);
      const def = addrs.find((a) => a.isDefault);
      if (def) setSelectedAddressId(def.id);
      else if (addrs.length > 0) setSelectedAddressId(addrs[0].id);
    }).catch(() => { toast.error(t("common.fetchError")); });
    getMyCoupons().then((coupons) => {
      setMyCoupons(coupons.map((c) => ({ code: c.code, couponName: c.name, discountType: c.discountType, discountValue: c.discountValue })));
    }).catch(() => { toast.error(t("common.fetchError")); });
    getPointBalance().then((b) => setPointBalance(b.balance)).catch(() => { toast.error(t("common.fetchError")); });
    getPaymentClientKey().then((res) => {
      setPaymentProvider(res.provider);
      setPaymentClientKey(res.clientKey);
    }).catch(() => { toast.error(t("common.fetchError")); });
  }, [isAuthenticated, fetchCart, router]);

  if (!isAuthenticated) return null;

  if (cartLoading && !cart) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <div className="w-12 h-12 border-4 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin mx-auto" />
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <ShoppingCart size={64} className="mx-auto text-gray-300 mb-6" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">{t("order.emptyCart")}</h1>
        <Link
          href="/products"
          className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity mt-4"
        >
          {t("cart.browseProducts")}
        </Link>
      </div>
    );
  }

  const handleSaveAddress = async () => {
    if (!newAddress.recipientName || !newAddress.phone || !newAddress.zipCode || !newAddress.address1) return;
    try {
      const { addressId } = await createAddress(newAddress as Omit<Address, "id">);
      const addrs = await getAddresses();
      setAddresses(addrs);
      setSelectedAddressId(addressId);
      setShowNewAddress(false);
      setNewAddress({ recipientName: "", phone: "", zipCode: "", address1: "", address2: "", isDefault: false });
    } catch {
      toast.error(t("order.saveFailed"));
    }
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      const { orderId, orderNumber } = await createOrder({
        shippingAddressId: selectedAddressId,
        note: note || null,
        couponCode: appliedCoupon?.code || null,
        pointsToUse: pointsToUse,
      });

      const finalAmount = Math.max(0, cart.totalAmount - (appliedCoupon?.discount ?? 0) - pointsToUse);

      // If TossPayments is configured and has a client key, use Widget SDK
      if (paymentProvider === "TossPayments" && paymentClientKey && finalAmount > 0) {
        const tossPayments = (window as unknown as Record<string, unknown>).TossPayments as ((clientKey: string) => { requestPayment: (method: string, options: Record<string, unknown>) => Promise<void> }) | undefined;
        if (!tossPayments) {
          toast.error(t("order.paymentLoading"));
          setSubmitting(false);
          return;
        }

        const widget = tossPayments(paymentClientKey);
        await widget.requestPayment("CARD", {
          amount: finalAmount,
          orderId: orderNumber,
          orderName: cart.items.length > 1
            ? `${cart.items[0].productName} ${t("order.andMore", { count: cart.items.length - 1 })}`
            : cart.items[0].productName,
          successUrl: `${window.location.origin}/order/success`,
          failUrl: `${window.location.origin}/order/fail`,
        });
      } else {
        // No payment gateway configured or zero-amount order — skip payment step
        router.push(`/order/complete?id=${orderId}`);
      }
    } catch {
      toast.error(t("order.orderFailed"));
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {paymentProvider === "TossPayments" && (
        <Script src="https://js.tosspayments.com/v1" strategy="afterInteractive" />
      )}
      <CheckoutSteps currentStep="order" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-8">{t("order.title")}</h1>

      {/* Shipping Address */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-bold text-[var(--color-secondary)] flex items-center gap-2">
            <MapPin size={18} /> {t("order.shippingAddress")}
          </h2>
          <button
            onClick={() => setShowNewAddress(!showNewAddress)}
            className="text-sm text-[var(--color-primary)] hover:underline flex items-center gap-1"
          >
            <Plus size={14} /> {t("order.addNewAddress")}
          </button>
        </div>

        {showNewAddress && (
          <div className="border border-[var(--color-primary)] rounded-lg p-4 mb-4 space-y-3">
            <input
              type="text"
              placeholder={t("order.recipientName")}
              value={newAddress.recipientName}
              onChange={(e) => setNewAddress({ ...newAddress, recipientName: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg text-sm"
            />
            <input
              type="text"
              placeholder={t("order.phone")}
              value={newAddress.phone}
              onChange={(e) => setNewAddress({ ...newAddress, phone: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg text-sm"
            />
            <div className="flex gap-2">
              <input
                type="text"
                placeholder={t("order.zipCode")}
                value={newAddress.zipCode}
                onChange={(e) => setNewAddress({ ...newAddress, zipCode: e.target.value })}
                className="w-32 px-3 py-2 border rounded-lg text-sm"
              />
              <input
                type="text"
                placeholder={t("order.baseAddress")}
                value={newAddress.address1}
                onChange={(e) => setNewAddress({ ...newAddress, address1: e.target.value })}
                className="flex-1 px-3 py-2 border rounded-lg text-sm"
              />
            </div>
            <input
              type="text"
              placeholder={t("order.detailAddress")}
              value={newAddress.address2}
              onChange={(e) => setNewAddress({ ...newAddress, address2: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg text-sm"
            />
            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 text-sm text-gray-500">
                <input
                  type="checkbox"
                  checked={newAddress.isDefault}
                  onChange={(e) => setNewAddress({ ...newAddress, isDefault: e.target.checked })}
                />
                {t("order.setDefault")}
              </label>
              <button
                onClick={handleSaveAddress}
                className="px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90"
              >
                {t("common.save")}
              </button>
            </div>
          </div>
        )}

        {addresses.length === 0 && !showNewAddress ? (
          <p className="text-gray-500 text-sm">{t("order.noAddress")}</p>
        ) : (
          <div className="space-y-2">
            {addresses.map((addr) => (
              <label
                key={addr.id}
                className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                  selectedAddressId === addr.id
                    ? "border-[var(--color-primary)] bg-[var(--color-primary)]/5"
                    : "border-gray-200 hover:border-gray-300"
                }`}
              >
                <input
                  type="radio"
                  name="address"
                  checked={selectedAddressId === addr.id}
                  onChange={() => setSelectedAddressId(addr.id)}
                  className="mt-1"
                />
                <div>
                  <p className="font-medium text-sm text-[var(--color-secondary)]">
                    {addr.recipientName}
                    {addr.isDefault && (
                      <span className="ml-2 text-xs text-[var(--color-primary)] font-normal">{t("mypage.addresses.defaultLabel")}</span>
                    )}
                  </p>
                  <p className="text-sm text-gray-500">{addr.phone}</p>
                  <p className="text-sm text-gray-500">[{addr.zipCode}] {addr.address1} {addr.address2}</p>
                </div>
              </label>
            ))}
          </div>
        )}
      </section>

      {/* Order Items */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] mb-4">{t("order.orderItems", { count: cart.totalQuantity })}</h2>
        <div className="space-y-3">
          {cart.items.map((item) => (
            <div key={item.id} className="flex gap-3 py-3 border-b border-gray-50 last:border-0">
              <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-gray-100 shrink-0">
                {item.primaryImageUrl ? (
                  <Image src={item.primaryImageUrl} alt={item.productName} fill className="object-cover" sizes="64px" />
                ) : (
                  <div className="w-full h-full flex items-center justify-center text-2xl opacity-30">📦</div>
                )}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-[var(--color-secondary)] line-clamp-1">{item.productName}</p>
                {item.variantName && <p className="text-xs text-gray-500">{item.variantName}</p>}
                <p className="text-xs text-gray-400 mt-0.5">{t("common.quantity", { count: item.quantity })}</p>
              </div>
              <p className="text-sm font-bold text-[var(--color-secondary)] shrink-0">{formatPrice(item.subTotal)}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Note */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] mb-3">{t("order.shippingMemo")}</h2>
        <textarea
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder={t("order.memoPlaceholder")}
          className="w-full px-3 py-2 border rounded-lg text-sm resize-none h-20"
        />
      </section>

      {/* Coupon */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] mb-3 flex items-center gap-2">
          <Tag size={18} /> {t("order.coupon")}
        </h2>
        {appliedCoupon ? (
          <div className="flex items-center justify-between p-3 bg-green-50 rounded-lg border border-green-200">
            <div className="flex items-center gap-2">
              <Check size={16} className="text-green-600" />
              <span className="text-sm text-green-800 font-medium">{appliedCoupon.name}</span>
              <span className="text-sm text-green-600">(-{formatPrice(appliedCoupon.discount)})</span>
            </div>
            <button
              onClick={() => { setAppliedCoupon(null); setCouponCode(""); }}
              className="text-xs text-gray-500 hover:text-red-500"
            >
              {t("common.cancel")}
            </button>
          </div>
        ) : (
          <>
            <div className="flex gap-2 mb-2">
              <input
                type="text"
                value={couponCode}
                onChange={(e) => { setCouponCode(e.target.value.toUpperCase()); setCouponError(""); }}
                placeholder={t("order.couponCode")}
                className="flex-1 px-3 py-2 border rounded-lg text-sm"
              />
              <button
                onClick={async () => {
                  if (!couponCode.trim()) return;
                  try {
                    const result = await validateCoupon(couponCode.trim(), cart.totalAmount);
                    if (result.isValid) {
                      const matched = myCoupons.find((c) => c.code === couponCode.trim());
                      setAppliedCoupon({ code: couponCode.trim(), name: matched?.couponName || couponCode.trim(), discount: result.discountAmount });
                      setCouponError("");
                    } else {
                      setCouponError(result.errorMessage || t("order.couponInvalid"));
                    }
                  } catch {
                    setCouponError(t("order.couponCheckFailed"));
                  }
                }}
                className="px-4 py-2 bg-[var(--color-secondary)] text-white rounded-lg text-sm hover:opacity-90"
              >
                {t("common.apply")}
              </button>
            </div>
            {couponError && <p className="text-xs text-red-500 mb-2">{couponError}</p>}
            {myCoupons.length > 0 && (
              <div className="space-y-1">
                <p className="text-xs text-gray-400 mb-1">{t("order.myCoupons")}</p>
                {myCoupons.map((c) => (
                  <button
                    key={c.code}
                    onClick={() => setCouponCode(c.code)}
                    className="w-full text-left px-3 py-2 text-sm rounded-lg border border-gray-100 hover:border-[var(--color-primary)] transition-colors"
                  >
                    <span className="font-medium">{c.couponName}</span>
                    <span className="text-gray-400 text-xs ml-2">
                      ({c.discountType === "Percentage" ? `${c.discountValue}%` : formatPrice(c.discountValue)} {t("order.discountLabel")})
                    </span>
                  </button>
                ))}
              </div>
            )}
          </>
        )}
      </section>

      {/* Points */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] mb-3 flex items-center gap-2">
          <Coins size={18} /> {t("order.points")}
        </h2>
        <div className="flex items-center gap-3">
          <input
            type="number"
            min={0}
            max={pointBalance}
            value={pointsToUse || ""}
            onChange={(e) => {
              const v = Math.min(Number(e.target.value) || 0, pointBalance);
              setPointsToUse(v);
            }}
            placeholder={t("order.pointsPlaceholder")}
            className="flex-1 px-3 py-2 border rounded-lg text-sm"
          />
          <button
            onClick={() => setPointsToUse(pointBalance)}
            className="px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50"
          >
            {t("order.pointsUseAll")}
          </button>
        </div>
        <p className="text-xs text-gray-400 mt-2">
          {t("order.pointsBalance")}: <span className="font-medium text-[var(--color-primary)]">{pointBalance.toLocaleString()}</span>P
        </p>
      </section>

      {/* Payment Method */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] mb-3 flex items-center gap-2">
          <CreditCard size={18} /> {t("order.paymentMethod")}
        </h2>
        <div className="grid grid-cols-2 gap-3">
          <button
            onClick={() => setPaymentProvider(paymentClientKey ? "TossPayments" : "Mock")}
            className={`p-4 rounded-lg border-2 text-left transition-all ${
              paymentProvider !== "Mock" || !paymentClientKey
                ? "border-[var(--color-primary)] bg-[var(--color-primary)]/5"
                : "border-gray-200 hover:border-gray-300"
            }`}
          >
            <CreditCard size={20} className="text-[var(--color-primary)] mb-1" />
            <p className="text-sm font-medium text-[var(--color-secondary)]">{t("order.cardPayment")}</p>
            <p className="text-xs text-gray-400">{t("order.cardPaymentDesc")}</p>
          </button>
          <button
            onClick={() => setPaymentProvider("Mock")}
            className={`p-4 rounded-lg border-2 text-left transition-all ${
              paymentProvider === "Mock" && paymentClientKey
                ? "border-[var(--color-primary)] bg-[var(--color-primary)]/5"
                : !paymentClientKey
                  ? "border-gray-200 bg-gray-50 text-gray-300 cursor-not-allowed"
                  : "border-gray-200 hover:border-gray-300"
            }`}
            disabled={!paymentClientKey}
          >
            <Banknote size={20} className="text-gray-400 mb-1" />
            <p className="text-sm font-medium text-[var(--color-secondary)]">{t("order.bankTransfer")}</p>
            <p className="text-xs text-gray-400">{t("order.bankTransferDesc")}</p>
          </button>
        </div>
      </section>

      {/* Summary + Submit */}
      <section className="bg-white rounded-xl shadow-sm p-6">
        <div className="space-y-3 text-sm mb-6">
          <div className="flex justify-between">
            <span className="text-gray-500">{t("order.subtotal")}</span>
            <span>{formatPrice(cart.totalAmount)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">{t("order.shippingFee")}</span>
            <span className="text-[var(--color-primary)]">{t("common.free")}</span>
          </div>
          {(appliedCoupon?.discount ?? 0) > 0 && (
            <div className="flex justify-between text-green-600">
              <span>{t("order.couponDiscount")}</span>
              <span>-{formatPrice(appliedCoupon!.discount)}</span>
            </div>
          )}
          {pointsToUse > 0 && (
            <div className="flex justify-between text-blue-600">
              <span>{t("order.pointsUsed")}</span>
              <span>-{formatPrice(pointsToUse)}</span>
            </div>
          )}
          <div className="border-t pt-3 flex justify-between">
            <span className="font-bold text-[var(--color-secondary)]">{t("order.totalAmount")}</span>
            <span className="font-bold text-xl text-[var(--color-primary)]">
              {formatPrice(Math.max(0, cart.totalAmount - (appliedCoupon?.discount ?? 0) - pointsToUse))}
            </span>
          </div>
        </div>

        <button
          onClick={handleSubmit}
          disabled={submitting}
          className="w-full py-4 bg-[var(--color-primary)] text-white rounded-xl font-bold text-lg hover:opacity-90 transition-opacity disabled:opacity-60"
        >
          {submitting ? t("order.processing") : t("order.pay", { amount: formatPrice(Math.max(0, cart.totalAmount - (appliedCoupon?.discount ?? 0) - pointsToUse)) })}
        </button>
      </section>
    </div>
  );
}
