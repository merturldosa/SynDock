"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Heart, Trash2 } from "lucide-react";
import toast from "react-hot-toast";
import { useTranslations } from "next-intl";
import { getWishlist, toggleWishlist } from "@/lib/reviewApi";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import { formatPrice } from "@/lib/format";
import type { WishlistItem } from "@/types/review";

export default function WishlistPage() {
  const t = useTranslations();
  const { isAuthenticated } = useAuthStore();
  const { addToCart } = useCartStore();
  const [items, setItems] = useState<WishlistItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!isAuthenticated) return;
    setLoading(true);
    getWishlist().then(setItems).catch(() => toast.error(t("common.fetchError"))).finally(() => setLoading(false));
  }, [isAuthenticated]);

  const handleRemove = async (productId: number) => {
    await toggleWishlist(productId);
    setItems((prev) => prev.filter((i) => i.productId !== productId));
  };

  const handleAddToCart = async (productId: number) => {
    await addToCart(productId);
  };

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

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link href="/mypage" className="hover:text-[var(--color-primary)]">{t("mypage.title")}</Link>
        <span>/</span>
        <span className="text-[var(--color-secondary)] font-medium">{t("mypage.wishlist.title")}</span>
      </div>

      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("mypage.wishlist.title")} <span className="text-[var(--color-primary)]">({items.length})</span>
      </h1>

      {items.length === 0 ? (
        <div className="text-center py-20">
          <Heart size={64} className="mx-auto text-gray-300 mb-6" />
          <p className="text-gray-500 mb-4">{t("mypage.wishlist.empty")}</p>
          <Link
            href="/products"
            className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
          >
            {t("mypage.wishlist.browseProducts")}
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {items.map((item) => (
            <div key={item.id} className="bg-white rounded-xl shadow-sm overflow-hidden group">
              <Link href={`/products/${item.productId}`}>
                <div className="relative aspect-square bg-gray-100">
                  {item.primaryImageUrl ? (
                    <Image src={item.primaryImageUrl} alt={item.productName} fill className="object-cover" sizes="(max-width: 768px) 50vw, 25vw" />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-4xl opacity-20">📦</div>
                  )}
                  <button
                    onClick={(e) => { e.preventDefault(); e.stopPropagation(); handleRemove(item.productId); }}
                    className="absolute top-2 right-2 p-2 bg-white/90 rounded-full text-red-500 hover:bg-red-50 transition-colors"
                    aria-label="Remove from wishlist"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              </Link>
              <div className="p-3">
                <Link href={`/products/${item.productId}`}>
                  <h3 className="text-sm font-medium text-[var(--color-secondary)] line-clamp-2 hover:text-[var(--color-primary)] transition-colors">
                    {item.productName}
                  </h3>
                </Link>
                <div className="flex items-baseline gap-1 mt-1">
                  {item.priceType === "Inquiry" ? (
                    <span className="text-sm font-bold text-[var(--color-primary)]">{t("products.inquiryPrice")}</span>
                  ) : (
                    <>
                      <span className="text-sm font-bold text-[var(--color-secondary)]">{formatPrice(item.salePrice ?? item.price)}</span>
                      {item.salePrice && <span className="text-xs text-gray-400 line-through">{formatPrice(item.price)}</span>}
                    </>
                  )}
                </div>
                {item.priceType !== "Inquiry" && (
                  <button
                    onClick={() => handleAddToCart(item.productId)}
                    className="w-full mt-2 py-2 text-xs bg-[var(--color-secondary)] text-white rounded-lg hover:opacity-90 transition-opacity"
                  >
                    {t("products.addToCart")}
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
