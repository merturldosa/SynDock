"use client";

import Link from "next/link";
import Image from "next/image";
import { ShoppingCart } from "lucide-react";
import { useTranslations } from "next-intl";
import type { ProductSummary } from "@/types/product";

interface ProductCardProps {
  product: ProductSummary;
  onAddToCart?: (product: ProductSummary) => void;
}

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

export function ProductCard({ product, onAddToCart }: ProductCardProps) {
  const t = useTranslations("products");
  const discount = product.salePrice
    ? Math.round((1 - product.salePrice / product.price) * 100)
    : 0;

  const isInquiry = product.priceType === "Inquiry";
  const hasImage = !!product.primaryImageUrl;

  return (
    <Link href={`/products/${product.id}`}>
      <div className="group bg-white rounded-2xl shadow-sm hover:shadow-lg transition-all overflow-hidden">
        {/* Image */}
        <div className="relative aspect-square bg-gradient-to-br from-gray-100 to-gray-50 flex items-center justify-center overflow-hidden">
          {hasImage ? (
            <Image
              src={product.primaryImageUrl!}
              alt={product.name}
              fill
              className="object-cover group-hover:scale-105 transition-transform duration-300"
              sizes="(max-width: 768px) 50vw, (max-width: 1200px) 33vw, 25vw"
            />
          ) : (
            <span className="text-6xl opacity-30 group-hover:scale-110 transition-transform duration-300">
              📦
            </span>
          )}

          {product.isFeatured && (
            <span className="absolute top-3 left-3 px-3 py-1 text-xs font-bold rounded-full text-white bg-[var(--color-primary)]">
              {t("best")}
            </span>
          )}
          {product.isNew && (
            <span className="absolute top-3 left-3 px-3 py-1 text-xs font-bold rounded-full text-white bg-emerald-500">
              {t("new")}
            </span>
          )}
          {discount > 0 && (
            <span className="absolute top-3 left-3 px-3 py-1 text-xs font-bold rounded-full text-white bg-red-500">
              {t("discount", { percent: discount })}
            </span>
          )}

          {/* Out of stock overlay */}
          {product.totalStock !== undefined && product.totalStock <= 0 && (
            <div className="absolute inset-0 bg-black/40 flex items-center justify-center">
              <span className="bg-white/90 text-gray-700 font-bold text-sm px-4 py-2 rounded-full">{t("outOfStock")}</span>
            </div>
          )}

          {/* Quick add button */}
          {!isInquiry && (product.totalStock === undefined || product.totalStock > 0) && (
            <button
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onAddToCart?.(product);
              }}
              className="absolute bottom-3 right-3 p-3 bg-[var(--color-secondary)] text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity hover:bg-[var(--color-primary)]"
            >
              <ShoppingCart size={18} />
            </button>
          )}
        </div>

        {/* Info */}
        <div className="p-4">
          <p className="text-xs text-[var(--color-primary)] font-medium mb-1">{product.categoryName}</p>
          <h3 className="font-semibold text-[var(--color-secondary)] mb-1 line-clamp-2 text-sm group-hover:text-[var(--color-primary)] transition-colors">
            {product.name}
          </h3>
          {product.specification && (
            <p className="text-xs text-gray-500 mb-2 line-clamp-1">{product.specification}</p>
          )}

          <div className="flex items-baseline gap-2 mt-auto">
            {isInquiry ? (
              <span className="text-sm font-bold text-[var(--color-primary)]">{t("inquiryPrice")}</span>
            ) : (
              <>
                <span className="text-lg font-bold text-[var(--color-secondary)]">
                  {formatPrice(product.salePrice || product.price)}
                </span>
                {product.salePrice && (
                  <span className="text-sm text-gray-400 line-through">
                    {formatPrice(product.price)}
                  </span>
                )}
              </>
            )}
          </div>
          {product.totalStock !== undefined && product.totalStock <= 0 && (
            <p className="text-xs text-red-500 font-medium mt-1">{t("outOfStock")}</p>
          )}
          {product.totalStock !== undefined && product.totalStock > 0 && product.totalStock <= 5 && (
            <p className="text-xs text-orange-500 mt-1">{t("lowStock", { count: product.totalStock })}</p>
          )}
        </div>
      </div>
    </Link>
  );
}
