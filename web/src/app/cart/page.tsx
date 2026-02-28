"use client";

import { useEffect } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { ShoppingCart, Trash2, Minus, Plus, ArrowRight } from "lucide-react";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

export default function CartPage() {
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
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">장바구니</h1>
        <p className="text-gray-500 mb-8">로그인 후 이용할 수 있습니다.</p>
        <Link
          href="/login"
          className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          로그인하기
        </Link>
      </div>
    );
  }

  if (isLoading && !cart) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <div className="w-12 h-12 border-4 border-[var(--color-primary)] border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <p className="text-gray-500">장바구니를 불러오는 중...</p>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <ShoppingCart size={64} className="mx-auto text-gray-300 mb-6" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">장바구니가 비어있습니다</h1>
        <p className="text-gray-500 mb-8">마음에 드는 상품을 담아보세요.</p>
        <Link
          href="/products"
          className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          상품 둘러보기
        </Link>
      </div>
    );
  }

  const handleCheckout = () => {
    router.push("/order");
  };

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-8">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          장바구니 <span className="text-[var(--color-primary)]">({cart.totalQuantity})</span>
        </h1>
        <button
          onClick={() => clearCart()}
          className="text-sm text-gray-400 hover:text-red-500 transition-colors"
        >
          전체 삭제
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
                      unoptimized
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
            <h2 className="font-bold text-[var(--color-secondary)] mb-4">주문 요약</h2>

            <div className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">상품 금액</span>
                <span className="font-medium">{formatPrice(cart.totalAmount)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">배송비</span>
                <span className="font-medium text-[var(--color-primary)]">무료</span>
              </div>
              <div className="border-t pt-3 flex justify-between">
                <span className="font-bold text-[var(--color-secondary)]">총 결제금액</span>
                <span className="font-bold text-lg text-[var(--color-primary)]">
                  {formatPrice(cart.totalAmount)}
                </span>
              </div>
            </div>

            <button
              onClick={handleCheckout}
              className="w-full mt-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity flex items-center justify-center gap-2"
            >
              주문하기
              <ArrowRight size={18} />
            </button>

            <Link
              href="/products"
              className="block text-center mt-3 text-sm text-gray-500 hover:text-[var(--color-primary)] transition-colors"
            >
              쇼핑 계속하기
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
