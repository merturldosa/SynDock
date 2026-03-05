"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useState, useMemo } from "react";
import Image from "next/image";
import Link from "next/link";
import {
  ChevronLeft, ChevronRight, ShoppingCart, Phone, Heart, Sparkles,
  FolderPlus, Minus, Plus, Package, Check,
} from "lucide-react";
import { getProductById } from "@/lib/productApi";
import { toggleWishlist, checkWishlist } from "@/lib/reviewApi";
import { getProductRecommendations, type RecommendedProduct } from "@/lib/recommendationApi";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import toast from "react-hot-toast";
import { useTenantStore } from "@/stores/tenantStore";
import { ReviewTab } from "@/components/product/ReviewTab";
import { QnATab } from "@/components/product/QnATab";
import { ShareButton } from "@/components/ShareButton";
import { AddToCollectionModal } from "@/components/AddToCollectionModal";
import { useTranslations } from "next-intl";
import { formatPrice } from "@/lib/format";
import DOMPurify from "isomorphic-dompurify";
import type { ProductDetail, ProductVariant } from "@/types/product";

function calcDiscountPercent(price: number, salePrice: number): number {
  if (price <= 0 || salePrice >= price) return 0;
  return Math.round(((price - salePrice) / price) * 100);
}

export default function ProductDetailClient() {
  const t = useTranslations();
  const params = useParams();
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();
  const { addToCart } = useCartStore();
  const { config } = useTenantStore();
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedImage, setSelectedImage] = useState(0);
  const [activeTab, setActiveTab] = useState<"detail" | "review" | "qna">("detail");
  const [isWished, setIsWished] = useState(false);
  const [recommendations, setRecommendations] = useState<RecommendedProduct[]>([]);
  const [showCollectionModal, setShowCollectionModal] = useState(false);
  const [selectedVariant, setSelectedVariant] = useState<ProductVariant | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [addingToCart, setAddingToCart] = useState(false);
  const [buyingNow, setBuyingNow] = useState(false);

  useEffect(() => {
    const id = Number(params.id);
    if (!id) return;

    setLoading(true);
    setSelectedVariant(null);
    setQuantity(1);
    getProductById(id)
      .then((p) => {
        setProduct(p);
        if (p.variants && p.variants.length > 0) {
          setSelectedVariant(p.variants[0]);
        }
        if (isAuthenticated) {
          checkWishlist([p.id]).then((res) => setIsWished(res.wishedProductIds.includes(p.id))).catch(() => { toast.error(t("common.fetchError")); });
        }
        getProductRecommendations(p.id, 6).then(setRecommendations).catch(() => { toast.error(t("common.fetchError")); });
      })
      .catch(() => setProduct(null))
      .finally(() => setLoading(false));
  }, [params.id, isAuthenticated]);

  // Computed price based on selected variant
  const displayPrice = useMemo(() => {
    if (!product) return { price: 0, salePrice: null as number | null };
    if (selectedVariant?.price) {
      return { price: selectedVariant.price, salePrice: null };
    }
    return { price: product.price, salePrice: product.salePrice };
  }, [product, selectedVariant]);

  const finalPrice = displayPrice.salePrice ?? displayPrice.price;
  const discountPercent = displayPrice.salePrice
    ? calcDiscountPercent(displayPrice.price, displayPrice.salePrice)
    : 0;
  const totalPrice = finalPrice * quantity;

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!product) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <p className="text-lg text-gray-500 mb-4">{t("products.notFound")}</p>
        <Link href="/products" className="text-[var(--color-primary)] hover:underline">
          {t("common.viewAllProducts")}
        </Link>
      </div>
    );
  }

  const isInquiry = product.priceType === "Inquiry";
  const primaryImage = product.images[selectedImage] || product.images[0];
  const contactPhone = config?.contactPhone || "";
  const hasVariants = product.variants && product.variants.length > 0;
  const stockCount = selectedVariant ? selectedVariant.stock : null;
  const isOutOfStock = selectedVariant ? selectedVariant.stock <= 0 : false;

  const handleToggleWishlist = async () => {
    if (!isAuthenticated) {
      sessionStorage.setItem("returnTo", `/products/${product.id}`);
      router.push("/login");
      return;
    }
    try {
      const { isWished: newState } = await toggleWishlist(product.id);
      setIsWished(newState);
    } catch { toast.error(t("common.fetchError")); }
  };

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      sessionStorage.setItem("returnTo", `/products/${product.id}`);
      router.push("/login");
      return;
    }
    setAddingToCart(true);
    const success = await addToCart(product.id, selectedVariant?.id ?? null, quantity);
    setAddingToCart(false);
    if (success) {
      toast.success(t("products.addedToCart", { name: product.name }));
    }
  };

  const handleBuyNow = async () => {
    if (!isAuthenticated) {
      sessionStorage.setItem("returnTo", `/products/${product.id}`);
      router.push("/login");
      return;
    }
    setBuyingNow(true);
    const success = await addToCart(product.id, selectedVariant?.id ?? null, quantity);
    setBuyingNow(false);
    if (success) {
      router.push("/order");
    }
  };

  const handlePrevImage = () => {
    setSelectedImage((prev) => (prev > 0 ? prev - 1 : product.images.length - 1));
  };

  const handleNextImage = () => {
    setSelectedImage((prev) => (prev < product.images.length - 1 ? prev + 1 : 0));
  };

  return (
    <div className="max-w-7xl mx-auto px-4 py-8 pb-28 md:pb-8">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link href="/" className="hover:text-[var(--color-primary)]">{t("common.home")}</Link>
        <span>/</span>
        <Link
          href={`/products?category=${product.categorySlug}`}
          className="hover:text-[var(--color-primary)]"
        >
          {product.categoryName}
        </Link>
        <span>/</span>
        <span className="text-[var(--color-secondary)] font-medium line-clamp-1">{product.name}</span>
      </div>

      {/* Back button */}
      <button
        onClick={() => router.back()}
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-[var(--color-secondary)] mb-6"
      >
        <ChevronLeft size={16} /> {t("common.back")}
      </button>

      <div className="grid md:grid-cols-2 gap-8 lg:gap-12">
        {/* Image section */}
        <div>
          <div className="relative aspect-square bg-gray-50 rounded-2xl overflow-hidden mb-4 group">
            {primaryImage ? (
              <Image
                src={primaryImage.url}
                alt={primaryImage.altText || product.name}
                fill
                className="object-contain"
                sizes="(max-width: 768px) 100vw, 50vw"
              />
            ) : (
              <div className="flex items-center justify-center h-full text-8xl opacity-20">
                📦
              </div>
            )}

            {/* Image navigation arrows */}
            {product.images.length > 1 && (
              <>
                <button
                  onClick={handlePrevImage}
                  aria-label="Previous image"
                  className="absolute left-2 top-1/2 -translate-y-1/2 w-10 h-10 bg-white/80 rounded-full flex items-center justify-center shadow-md opacity-0 group-hover:opacity-100 transition-opacity hover:bg-white"
                >
                  <ChevronLeft size={20} />
                </button>
                <button
                  onClick={handleNextImage}
                  aria-label="Next image"
                  className="absolute right-2 top-1/2 -translate-y-1/2 w-10 h-10 bg-white/80 rounded-full flex items-center justify-center shadow-md opacity-0 group-hover:opacity-100 transition-opacity hover:bg-white"
                >
                  <ChevronRight size={20} />
                </button>
                {/* Image counter */}
                <div className="absolute bottom-3 right-3 bg-black/50 text-white text-xs px-2 py-1 rounded-full">
                  {selectedImage + 1} / {product.images.length}
                </div>
              </>
            )}

            {/* Discount badge */}
            {discountPercent > 0 && (
              <div className="absolute top-3 left-3 bg-red-500 text-white text-sm font-bold px-3 py-1 rounded-full">
                {discountPercent}% OFF
              </div>
            )}
          </div>

          {/* Thumbnail gallery */}
          {product.images.length > 1 && (
            <div className="flex gap-2 overflow-x-auto pb-1">
              {product.images.map((img, i) => (
                <button
                  key={img.id}
                  onClick={() => setSelectedImage(i)}
                  className={`relative w-20 h-20 flex-shrink-0 rounded-lg overflow-hidden border-2 transition-all ${
                    i === selectedImage ? "border-[var(--color-primary)] ring-1 ring-[var(--color-primary)]" : "border-transparent hover:border-gray-300"
                  }`}
                >
                  <Image
                    src={img.url}
                    alt={img.altText || ""}
                    fill
                    className="object-cover"
                    sizes="80px"
                  />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Product info */}
        <div>
          <p className="text-sm text-[var(--color-primary)] font-medium mb-2">{product.categoryName}</p>
          <h1 className="text-2xl lg:text-3xl font-bold text-[var(--color-secondary)] mb-4">{product.name}</h1>

          {product.specification && (
            <p className="text-gray-500 mb-4">{t("products.specification")}: {product.specification}</p>
          )}

          {/* Price */}
          <div className="border-y border-gray-100 py-6 mb-6">
            {isInquiry ? (
              <div>
                <p className="text-2xl font-bold text-[var(--color-primary)] mb-2">{t("products.inquiryPrice")}</p>
                <p className="text-sm text-gray-500">
                  {t("products.inquiryDesc")}
                </p>
              </div>
            ) : (
              <div>
                <div className="flex items-baseline gap-3">
                  {discountPercent > 0 && (
                    <span className="text-xl font-bold text-red-500">{discountPercent}%</span>
                  )}
                  <span className="text-3xl font-bold text-[var(--color-secondary)]">
                    {formatPrice(finalPrice)}
                  </span>
                  {displayPrice.salePrice && (
                    <span className="text-lg text-gray-400 line-through">
                      {formatPrice(displayPrice.price)}
                    </span>
                  )}
                </div>
                {/* Stock indicator */}
                {stockCount !== null && (
                  <div className="mt-2 flex items-center gap-1.5">
                    {stockCount > 10 ? (
                      <span className="flex items-center gap-1 text-sm text-green-600">
                        <Check size={14} /> {t("products.inStock")}
                      </span>
                    ) : stockCount > 0 ? (
                      <span className="flex items-center gap-1 text-sm text-orange-500">
                        <Package size={14} /> {t("products.lowStock", { count: stockCount })}
                      </span>
                    ) : (
                      <span className="text-sm text-red-500 font-medium">{t("products.outOfStock")}</span>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Variant selector */}
          {hasVariants && !isInquiry && (
            <div className="mb-6">
              <h3 className="text-sm font-semibold text-[var(--color-secondary)] mb-3">{t("products.selectOption")}</h3>
              <div className="flex flex-wrap gap-2">
                {product.variants!.map((variant) => (
                  <button
                    key={variant.id}
                    onClick={() => { setSelectedVariant(variant); setQuantity(1); }}
                    disabled={variant.stock <= 0}
                    className={`px-4 py-2 rounded-lg border-2 text-sm font-medium transition-all ${
                      selectedVariant?.id === variant.id
                        ? "border-[var(--color-primary)] bg-[var(--color-primary)]/5 text-[var(--color-primary)]"
                        : variant.stock <= 0
                          ? "border-gray-200 bg-gray-50 text-gray-300 cursor-not-allowed line-through"
                          : "border-gray-200 text-gray-700 hover:border-gray-400"
                    }`}
                  >
                    {variant.name}
                    {variant.price && variant.price !== product.price && (
                      <span className="ml-1 text-xs opacity-70">({formatPrice(variant.price)})</span>
                    )}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Quantity selector */}
          {!isInquiry && (
            <div className="mb-6">
              <h3 className="text-sm font-semibold text-[var(--color-secondary)] mb-3">{t("products.quantity")}</h3>
              <div className="flex items-center gap-4">
                <div className="flex items-center border-2 border-gray-200 rounded-lg">
                  <button
                    onClick={() => setQuantity(Math.max(1, quantity - 1))}
                    disabled={quantity <= 1}
                    aria-label="Decrease quantity"
                    className="p-2.5 hover:bg-gray-50 transition-colors rounded-l-lg disabled:opacity-30"
                  >
                    <Minus size={16} />
                  </button>
                  <input
                    type="number"
                    min={1}
                    max={stockCount && stockCount > 0 ? stockCount : 99}
                    value={quantity}
                    onChange={(e) => {
                      const v = Math.max(1, Math.min(Number(e.target.value) || 1, stockCount && stockCount > 0 ? stockCount : 99));
                      setQuantity(v);
                    }}
                    className="w-14 text-center text-sm font-medium border-x border-gray-200 py-2.5 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                  />
                  <button
                    onClick={() => setQuantity(Math.min(quantity + 1, stockCount && stockCount > 0 ? stockCount : 99))}
                    disabled={isOutOfStock}
                    aria-label="Increase quantity"
                    className="p-2.5 hover:bg-gray-50 transition-colors rounded-r-lg disabled:opacity-30"
                  >
                    <Plus size={16} />
                  </button>
                </div>
                {/* Total price */}
                {quantity > 1 && (
                  <span className="text-sm text-gray-500">
                    {t("products.totalPrice")}: <span className="font-bold text-[var(--color-secondary)]">{formatPrice(totalPrice)}</span>
                  </span>
                )}
              </div>
            </div>
          )}

          {/* Description */}
          {product.description && (
            <div className="mb-8">
              <h3 className="font-semibold text-[var(--color-secondary)] mb-2">{t("products.description")}</h3>
              <p className="text-gray-500 whitespace-pre-wrap">{product.description}</p>
            </div>
          )}

          {/* Actions - Desktop */}
          <div className="hidden md:flex gap-3 mb-3">
            <button
              onClick={handleToggleWishlist}
              aria-label="Add to wishlist"
              className={`p-4 rounded-xl border-2 transition-colors ${isWished ? "border-red-400 text-red-500" : "border-gray-200 text-gray-400 hover:border-red-300 hover:text-red-400"}`}
            >
              <Heart size={22} className={isWished ? "fill-red-500" : ""} />
            </button>
            <ShareButton title={product.name} text={product.description || undefined} />
            {isAuthenticated && (
              <button
                onClick={() => setShowCollectionModal(true)}
                aria-label="Add to collection"
                className="p-4 rounded-xl border-2 border-gray-200 text-gray-400 hover:border-purple-300 hover:text-purple-500 transition-colors"
                title={t("products.addToCollection")}
              >
                <FolderPlus size={22} />
              </button>
            )}
            {isInquiry ? (
              contactPhone ? (
                <a
                  href={`tel:${contactPhone}`}
                  className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-[var(--color-primary)] text-white rounded-xl font-bold text-lg hover:opacity-90 transition-opacity"
                >
                  <Phone size={22} /> {t("products.phoneConsult")}
                </a>
              ) : (
                <span className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-gray-200 text-gray-500 rounded-xl font-bold text-lg">
                  {t("products.contactInquiry")}
                </span>
              )
            ) : (
              <>
                <button
                  onClick={handleAddToCart}
                  disabled={addingToCart || isOutOfStock}
                  className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-[var(--color-secondary)] text-white rounded-xl font-bold text-lg hover:opacity-90 transition-opacity disabled:opacity-60"
                >
                  <ShoppingCart size={22} />
                  {isOutOfStock ? t("products.outOfStock") : addingToCart ? t("products.addingToCart") : t("products.addToCart")}
                </button>
                <button
                  onClick={handleBuyNow}
                  disabled={buyingNow || isOutOfStock}
                  className="flex-1 px-6 py-4 bg-[var(--color-primary)] text-white rounded-xl font-bold text-lg hover:opacity-90 transition-opacity disabled:opacity-60"
                >
                  {buyingNow ? t("products.addingToCart") : t("products.buyNow")}
                </button>
              </>
            )}
          </div>

          {/* Mobile action icons (wishlist, share, collection) */}
          <div className="flex md:hidden gap-3 mb-3">
            <button
              onClick={handleToggleWishlist}
              aria-label="Add to wishlist"
              className={`p-3 rounded-xl border-2 transition-colors ${isWished ? "border-red-400 text-red-500" : "border-gray-200 text-gray-400 hover:border-red-300 hover:text-red-400"}`}
            >
              <Heart size={20} className={isWished ? "fill-red-500" : ""} />
            </button>
            <ShareButton title={product.name} text={product.description || undefined} />
            {isAuthenticated && (
              <button
                onClick={() => setShowCollectionModal(true)}
                aria-label="Add to collection"
                className="p-3 rounded-xl border-2 border-gray-200 text-gray-400 hover:border-purple-300 hover:text-purple-500 transition-colors"
              >
                <FolderPlus size={20} />
              </button>
            )}
          </div>

          {/* Info */}
          <div className="mt-8 p-4 bg-gray-50 rounded-xl text-sm text-gray-500 space-y-2">
            <p>{t("common.viewCount")}: {product.viewCount.toLocaleString()}</p>
            <p>{t("common.shipping")}: {t("common.shippingInfo")}</p>
            {contactPhone && <p>{t("common.inquiry")}: {contactPhone} ({t("common.inquiryHours")})</p>}
          </div>
        </div>
      </div>

      {/* Tabs Section */}
      <div className="mt-12 border-t border-gray-200">
        <div className="flex border-b border-gray-200">
          {([
            { key: "detail" as const, label: t("products.detailInfo") },
            { key: "review" as const, label: t("products.review") },
            { key: "qna" as const, label: t("products.qna") },
          ]).map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`flex-1 py-4 text-center font-medium text-sm transition-colors ${
                activeTab === tab.key
                  ? "text-[var(--color-primary)] border-b-2 border-[var(--color-primary)]"
                  : "text-gray-400 hover:text-gray-600"
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="py-8">
          {activeTab === "detail" && (
            <div className="prose max-w-none">
              {product.detailSections && product.detailSections.length > 0 ? (
                <div className="space-y-10">
                  {product.detailSections.map((section) => (
                    <div key={section.id} className="border-b border-gray-100 pb-8 last:border-0">
                      <h3 className="text-lg font-bold text-[var(--color-secondary)] mb-3">{section.title}</h3>
                      {section.imageUrl && (
                        <div className="mb-4">
                          <Image
                            src={section.imageUrl}
                            alt={section.imageAltText || section.title}
                            width={800}
                            height={400}
                            className="rounded-xl w-full object-cover"
                          />
                        </div>
                      )}
                      {section.content && (
                        <div className="text-gray-600 whitespace-pre-wrap" dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(section.content || "") }} />
                      )}
                    </div>
                  ))}
                </div>
              ) : product.description ? (
                <p className="text-gray-600 whitespace-pre-wrap">{product.description}</p>
              ) : (
                <p className="text-gray-400 text-center py-8">{t("products.noDetail")}</p>
              )}
            </div>
          )}
          {activeTab === "review" && <ReviewTab productId={product.id} />}
          {activeTab === "qna" && <QnATab productId={product.id} />}
        </div>
      </div>

      {/* AI Recommendations */}
      {recommendations.length > 0 && (
        <div className="mt-16">
          <div className="flex items-center gap-2 mb-6">
            <Sparkles className="w-5 h-5 text-[var(--color-primary)]" />
            <h2 className="text-xl font-bold text-[var(--color-secondary)]">{t("products.recommended")}</h2>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
            {recommendations.map((rec) => (
              <Link
                key={rec.productId}
                href={`/products/${rec.productId}`}
                className="group bg-white rounded-xl border border-gray-100 overflow-hidden hover:shadow-md transition-shadow"
              >
                <div className="relative aspect-square bg-gray-50">
                  {rec.imageUrl ? (
                    <Image
                      src={rec.imageUrl}
                      alt={rec.productName}
                      fill
                      className="object-contain group-hover:scale-105 transition-transform"
                      sizes="(max-width: 768px) 50vw, 16vw"
                    />
                  ) : (
                    <div className="flex items-center justify-center h-full text-4xl opacity-20">
                      📦
                    </div>
                  )}
                </div>
                <div className="p-3">
                  <p className="text-sm text-gray-800 font-medium line-clamp-2 mb-1">
                    {rec.productName}
                  </p>
                  <p className="text-sm font-bold text-[var(--color-secondary)]">
                    {rec.salePrice
                      ? formatPrice(rec.salePrice)
                      : formatPrice(rec.price)}
                  </p>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* Sticky Mobile Bottom Bar */}
      {!isInquiry && (
        <div className="fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 p-3 flex gap-2 md:hidden z-50 safe-bottom">
          <button
            onClick={handleAddToCart}
            disabled={addingToCart || isOutOfStock}
            className="flex-1 flex items-center justify-center gap-2 py-3.5 bg-[var(--color-secondary)] text-white rounded-xl font-bold hover:opacity-90 transition-opacity disabled:opacity-60"
          >
            <ShoppingCart size={18} />
            {isOutOfStock ? t("products.outOfStock") : addingToCart ? "..." : t("products.addToCart")}
          </button>
          <button
            onClick={handleBuyNow}
            disabled={buyingNow || isOutOfStock}
            className="flex-1 py-3.5 bg-[var(--color-primary)] text-white rounded-xl font-bold hover:opacity-90 transition-opacity disabled:opacity-60"
          >
            {buyingNow ? "..." : `${t("products.buyNow")} ${formatPrice(totalPrice)}`}
          </button>
        </div>
      )}

      {/* Collection Modal */}
      {product && (
        <AddToCollectionModal
          productId={product.id}
          isOpen={showCollectionModal}
          onClose={() => setShowCollectionModal(false)}
        />
      )}
    </div>
  );
}
