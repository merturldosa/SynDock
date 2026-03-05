"use client";

import { useEffect } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { ShoppingCart, Trash2, Minus, Plus, ArrowRight, Truck } from "lucide-react";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";
import { useTranslations } from "next-intl";
import { formatPrice } from "@/lib/format";
import { CheckoutSteps } from "@/components/checkout/CheckoutSteps";

export default function CartPage() {
  const t = useTranslations();
  const router = useRouter();
  const { cart, isLoading, fetchCart, updateQuantity, removeItem, clearCart } = useCartStore();
  const { isAuthenticated } = useAuthStore();

  useEffect(() => {
    if (isAuthenticated) {
      fetchCart();
    }
  }, [isAuthenticated, fetchCart]);

  if (!isAuthenticated) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <ShoppingCart size={64} className="mx-auto text-gray-300 mb-6" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">{t("cart.title")}</h1>
        <p className="text-gray-500 mb-8">{t("cart.loginRequired")}</p>
        <Link
          href="/login"
          className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          {t("cart.loginAction")}
        </Link>
      </div>
    );
  }

  if (isLoading && !cart) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <div className="w-12 h-12 border-4 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <p className="text-gray-500">{t("common.loadingCart")}</p>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <ShoppingCart size={64} className="mx-auto text-gray-300 mb-6" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">{t("cart.empty")}</h1>
        <p className="text-gray-500 mb-8">{t("cart.emptyDesc")}</p>
        <Link
          href="/products"
          className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          {t("cart.browseProducts")}
        </Link>
      </div>
    );
  }

  const handleCheckout = () => {
    router.push("/order");
  };

  const discountTotal = cart.items.reduce((sum, item) => {
    if (item.salePrice) {
      return sum + (item.price - item.salePrice) * item.quantity;
    }
    return sum;
  }, 0);

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <CheckoutSteps currentStep="cart" />

      <div className="flex items-center justify-between mb-8">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          {t("cart.title")} <span className="text-[var(--color-primary)]">({cart.totalQuantity})</span>
        </h1>
        <button
          onClick={() => clearCart()}
          className="text-sm text-gray-400 hover:text-red-500 transition-colors"
        >
          {t("common.deleteAll")}
        </button>
      </div>

      <div className="lg:grid lg:grid-cols-3 lg:gap-8">
        {/* Cart Items */}
        <div className="lg:col-span-2 space-y-4 mb-8 lg:mb-0">
          {cart.items.map((item) => (
            <div
              key={item.id}
              className="bg-white rounded-xl shadow-sm p-4 flex gap-4"
            >
              {/* Image */}
              <Link href={`/products/${item.productId}`} className="shrink-0">
                <div className="relative w-24 h-24 rounded-lg overflow-hidden bg-gray-100">
                  {item.primaryImageUrl ? (
                    <Image
                      src={item.primaryImageUrl}
                      alt={item.productName}
                      fill
                      className="object-cover"
                      sizes="96px"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-3xl opacity-30">📦</div>
                  )}
                </div>
              </Link>

              {/* Details */}
              <div className="flex-1 min-w-0">
                <Link
                  href={`/products/${item.productId}`}
                  className="font-semibold text-[var(--color-secondary)] hover:text-[var(--color-primary)] transition-colors line-clamp-2 text-sm"
                >
                  {item.productName}
                </Link>
                {item.variantName && (
                  <p className="text-xs text-gray-500 mt-0.5">{item.variantName}</p>
                )}

                <div className="flex items-center gap-2 mt-2">
                  <span className="font-bold text-[var(--color-secondary)]">
                    {formatPrice(item.subTotal)}
                  </span>
                  {item.salePrice && (
                    <span className="text-xs text-gray-400 line-through">
                      {formatPrice(item.price * item.quantity)}
                    </span>
                  )}
                </div>

                {/* Quantity & Remove */}
                <div className="flex items-center justify-between mt-3">
                  <div className="flex items-center border rounded-lg">
                    <button
                      onClick={() => updateQuantity(item.id, Math.max(1, item.quantity - 1))}
                      className="p-1.5 hover:bg-gray-100 transition-colors rounded-l-lg"
                      disabled={item.quantity <= 1}
                    >
                      <Minus size={14} className={item.quantity <= 1 ? "text-gray-300" : "text-gray-600"} />
                    </button>
                    <span className="px-3 text-sm font-medium min-w-[2rem] text-center">{item.quantity}</span>
                    <button
                      onClick={() => updateQuantity(item.id, item.quantity + 1)}
                      className="p-1.5 hover:bg-gray-100 transition-colors rounded-r-lg"
                    >
                      <Plus size={14} className="text-gray-600" />
                    </button>
                  </div>
                  <button
                    onClick={() => removeItem(item.id)}
                    className="p-2 text-gray-400 hover:text-red-500 transition-colors"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Order Summary */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-xl shadow-sm p-6 sticky top-24">
            <h2 className="font-bold text-[var(--color-secondary)] mb-4">{t("cart.orderSummary")}</h2>

            <div className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">{t("cart.subtotal")}</span>
                <span className="font-medium">{formatPrice(cart.totalAmount + discountTotal)}</span>
              </div>
              {discountTotal > 0 && (
                <div className="flex justify-between text-red-500">
                  <span>{t("products.discount", { percent: "" }).replace("%", "")}</span>
                  <span>-{formatPrice(discountTotal)}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span className="text-gray-500">{t("cart.shippingFee")}</span>
                <span className="font-medium text-[var(--color-primary)]">{t("common.free")}</span>
              </div>
              <div className="border-t pt-3 flex justify-between">
                <span className="font-bold text-[var(--color-secondary)]">{t("cart.totalAmount")}</span>
                <span className="font-bold text-lg text-[var(--color-primary)]">
                  {formatPrice(cart.totalAmount)}
                </span>
              </div>
            </div>

            {/* Estimated delivery */}
            <div className="mt-4 p-3 bg-gray-50 rounded-lg flex items-start gap-2">
              <Truck size={16} className="text-gray-400 mt-0.5 shrink-0" />
              <div className="text-xs text-gray-500">
                <p className="font-medium text-gray-700">{t("checkout.estimatedDelivery")}</p>
                <p>{t("checkout.estimatedDays", { min: 3, max: 7 })}</p>
              </div>
            </div>

            <button
              onClick={handleCheckout}
              className="w-full mt-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity flex items-center justify-center gap-2"
            >
              {t("cart.checkout")}
              <ArrowRight size={18} />
            </button>

            <Link
              href="/products"
              className="block text-center mt-3 text-sm text-gray-500 hover:text-[var(--color-primary)] transition-colors"
            >
              {t("cart.continueShopping")}
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
