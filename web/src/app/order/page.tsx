"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import Link from "next/link";
import Script from "next/script";
import { ShoppingCart, MapPin, Plus, Tag, Coins, Check } from "lucide-react";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";
import { createOrder, getAddresses, createAddress, getPaymentClientKey } from "@/lib/orderApi";
import { getMyCoupons, validateCoupon } from "@/lib/couponApi";
import { getPointBalance } from "@/lib/pointApi";
import type { Address } from "@/types/order";

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

export default function OrderPage() {
  const router = useRouter();
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
    }).catch(() => {});
    getMyCoupons().then((coupons) => {
      setMyCoupons(coupons.map((c) => ({ code: c.code, couponName: c.name, discountType: c.discountType, discountValue: c.discountValue })));
    }).catch(() => {});
    getPointBalance().then((b) => setPointBalance(b.balance)).catch(() => {});
    getPaymentClientKey().then((res) => {
      setPaymentProvider(res.provider);
      setPaymentClientKey(res.clientKey);
    }).catch(() => {});
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
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">주문할 상품이 없습니다</h1>
        <Link
          href="/products"
          className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity mt-4"
        >
          상품 둘러보기
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
      alert("배송지 저장에 실패했습니다.");
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
          alert("결제 모듈을 불러오는 중입니다. 잠시 후 다시 시도해 주세요.");
          setSubmitting(false);
          return;
        }

        const widget = tossPayments(paymentClientKey);
        await widget.requestPayment("카드", {
          amount: finalAmount,
          orderId: orderNumber,
          orderName: cart.items.length > 1
            ? `${cart.items[0].productName} 외 ${cart.items.length - 1}건`
            : cart.items[0].productName,
          successUrl: `${window.location.origin}/order/success`,
          failUrl: `${window.location.origin}/order/fail`,
        });
      } else {
        // Mock provider — go directly to complete
        router.push(`/order/complete?id=${orderId}`);
      }
    } catch {
      alert("주문에 실패했습니다. 다시 시도해 주세요.");
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {paymentProvider === "TossPayments" && (
        <Script src="https://js.tosspayments.com/v1" strategy="afterInteractive" />
      )}
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-8">주문서</h1>

      {/* Shipping Address */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-bold text-[var(--color-secondary)] flex items-center gap-2">
            <MapPin size={18} /> 배송지
          </h2>
          <button
            onClick={() => setShowNewAddress(!showNewAddress)}
            className="text-sm text-[var(--color-primary)] hover:underline flex items-center gap-1"
          >
            <Plus size={14} /> 새 배송지 추가
          </button>
        </div>

        {showNewAddress && (
          <div className="border border-[var(--color-primary)] rounded-lg p-4 mb-4 space-y-3">
            <input
              type="text"
              placeholder="수령인 이름"
              value={newAddress.recipientName}
              onChange={(e) => setNewAddress({ ...newAddress, recipientName: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg text-sm"
            />
            <input
              type="text"
              placeholder="전화번호"
              value={newAddress.phone}
              onChange={(e) => setNewAddress({ ...newAddress, phone: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg text-sm"
            />
            <div className="flex gap-2">
              <input
                type="text"
                placeholder="우편번호"
                value={newAddress.zipCode}
                onChange={(e) => setNewAddress({ ...newAddress, zipCode: e.target.value })}
                className="w-32 px-3 py-2 border rounded-lg text-sm"
              />
              <input
                type="text"
                placeholder="기본 주소"
                value={newAddress.address1}
                onChange={(e) => setNewAddress({ ...newAddress, address1: e.target.value })}
                className="flex-1 px-3 py-2 border rounded-lg text-sm"
              />
            </div>
            <input
              type="text"
              placeholder="상세 주소 (선택)"
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
                기본 배송지로 설정
              </label>
              <button
                onClick={handleSaveAddress}
                className="px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90"
              >
                저장
              </button>
            </div>
          </div>
        )}

        {addresses.length === 0 && !showNewAddress ? (
          <p className="text-gray-500 text-sm">등록된 배송지가 없습니다. 새 배송지를 추가해 주세요.</p>
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
                      <span className="ml-2 text-xs text-[var(--color-primary)] font-normal">기본</span>
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
        <h2 className="font-bold text-[var(--color-secondary)] mb-4">주문 상품 ({cart.totalQuantity}개)</h2>
        <div className="space-y-3">
          {cart.items.map((item) => (
            <div key={item.id} className="flex gap-3 py-3 border-b border-gray-50 last:border-0">
              <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-gray-100 shrink-0">
                {item.primaryImageUrl ? (
                  <Image src={item.primaryImageUrl} alt={item.productName} fill className="object-cover" sizes="64px" unoptimized />
                ) : (
                  <div className="w-full h-full flex items-center justify-center text-2xl opacity-30">📦</div>
                )}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-[var(--color-secondary)] line-clamp-1">{item.productName}</p>
                {item.variantName && <p className="text-xs text-gray-500">{item.variantName}</p>}
                <p className="text-xs text-gray-400 mt-0.5">{item.quantity}개</p>
              </div>
              <p className="text-sm font-bold text-[var(--color-secondary)] shrink-0">{formatPrice(item.subTotal)}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Note */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] mb-3">배송 메모</h2>
        <textarea
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder="배송 시 요청사항을 입력해 주세요 (선택)"
          className="w-full px-3 py-2 border rounded-lg text-sm resize-none h-20"
        />
      </section>

      {/* Coupon */}
      <section className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <h2 className="font-bold text-[var(--color-secondary)] mb-3 flex items-center gap-2">
          <Tag size={18} /> 쿠폰
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
              취소
            </button>
          </div>
        ) : (
          <>
            <div className="flex gap-2 mb-2">
              <input
                type="text"
                value={couponCode}
                onChange={(e) => { setCouponCode(e.target.value.toUpperCase()); setCouponError(""); }}
                placeholder="쿠폰 코드 입력"
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
                      setCouponError(result.errorMessage || "사용할 수 없는 쿠폰입니다.");
                    }
                  } catch {
                    setCouponError("쿠폰 확인에 실패했습니다.");
                  }
                }}
                className="px-4 py-2 bg-[var(--color-secondary)] text-white rounded-lg text-sm hover:opacity-90"
              >
                적용
              </button>
            </div>
            {couponError && <p className="text-xs text-red-500 mb-2">{couponError}</p>}
            {myCoupons.length > 0 && (
              <div className="space-y-1">
                <p className="text-xs text-gray-400 mb-1">보유 쿠폰</p>
                {myCoupons.map((c) => (
                  <button
                    key={c.code}
                    onClick={() => setCouponCode(c.code)}
                    className="w-full text-left px-3 py-2 text-sm rounded-lg border border-gray-100 hover:border-[var(--color-primary)] transition-colors"
                  >
                    <span className="font-medium">{c.couponName}</span>
                    <span className="text-gray-400 text-xs ml-2">
                      ({c.discountType === "Percentage" ? `${c.discountValue}%` : formatPrice(c.discountValue)} 할인)
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
          <Coins size={18} /> 포인트
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
            placeholder="사용할 포인트"
            className="flex-1 px-3 py-2 border rounded-lg text-sm"
          />
          <button
            onClick={() => setPointsToUse(pointBalance)}
            className="px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50"
          >
            전액 사용
          </button>
        </div>
        <p className="text-xs text-gray-400 mt-2">
          보유 포인트: <span className="font-medium text-[var(--color-primary)]">{pointBalance.toLocaleString()}</span>P
        </p>
      </section>

      {/* Summary + Submit */}
      <section className="bg-white rounded-xl shadow-sm p-6">
        <div className="space-y-3 text-sm mb-6">
          <div className="flex justify-between">
            <span className="text-gray-500">상품 금액</span>
            <span>{formatPrice(cart.totalAmount)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">배송비</span>
            <span className="text-[var(--color-primary)]">무료</span>
          </div>
          {(appliedCoupon?.discount ?? 0) > 0 && (
            <div className="flex justify-between text-green-600">
              <span>쿠폰 할인</span>
              <span>-{formatPrice(appliedCoupon!.discount)}</span>
            </div>
          )}
          {pointsToUse > 0 && (
            <div className="flex justify-between text-blue-600">
              <span>포인트 사용</span>
              <span>-{formatPrice(pointsToUse)}</span>
            </div>
          )}
          <div className="border-t pt-3 flex justify-between">
            <span className="font-bold text-[var(--color-secondary)]">총 결제금액</span>
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
          {submitting ? "주문 처리 중..." : `${formatPrice(Math.max(0, cart.totalAmount - (appliedCoupon?.discount ?? 0) - pointsToUse))} 결제하기`}
        </button>
      </section>
    </div>
  );
}
