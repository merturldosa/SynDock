"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { ChevronLeft, ShoppingCart, Phone, Heart, Sparkles, FolderPlus } from "lucide-react";
import { getProductById } from "@/lib/productApi";
import { toggleWishlist, checkWishlist } from "@/lib/reviewApi";
import { getProductRecommendations, type RecommendedProduct } from "@/lib/recommendationApi";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import { useTenantStore } from "@/stores/tenantStore";
import { ReviewTab } from "@/components/product/ReviewTab";
import { QnATab } from "@/components/product/QnATab";
import { ShareButton } from "@/components/ShareButton";
import { AddToCollectionModal } from "@/components/AddToCollectionModal";
import { useTranslations } from "next-intl";
import type { ProductDetail } from "@/types/product";

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "\uC6D0";
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

  useEffect(() => {
    const id = Number(params.id);
    if (!id) return;

    setLoading(true);
    getProductById(id)
      .then((p) => {
        setProduct(p);
        if (isAuthenticated) {
          checkWishlist([p.id]).then((res) => setIsWished(res.wishedProductIds.includes(p.id))).catch(() => {});
        }
        getProductRecommendations(p.id, 6).then(setRecommendations).catch(() => {});
      })
      .catch(() => setProduct(null))
      .finally(() => setLoading(false));
  }, [params.id, isAuthenticated]);

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

  const [addingToCart, setAddingToCart] = useState(false);

  const handleToggleWishlist = async () => {
    if (!isAuthenticated) {
      sessionStorage.setItem("returnTo", `/products/${product.id}`);
      router.push("/login");
      return;
    }
    try {
      const { isWished: newState } = await toggleWishlist(product.id);
      setIsWished(newState);
    } catch { /* ignore */ }
  };

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      sessionStorage.setItem("returnTo", `/products/${product.id}`);
      router.push("/login");
      return;
    }
    setAddingToCart(true);
    const success = await addToCart(product.id);
    setAddingToCart(false);
    if (success) {
      alert(t("products.addedToCart", { name: product.name }));
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
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
          <div className="relative aspect-square bg-gray-50 rounded-2xl overflow-hidden mb-4">
            {primaryImage ? (
              <Image
                src={primaryImage.url}
                alt={primaryImage.altText || product.name}
                fill
                className="object-contain"
                sizes="(max-width: 768px) 100vw, 50vw"
                unoptimized
              />
            ) : (
              <div className="flex items-center justify-center h-full text-8xl opacity-20">
                📦
              </div>
            )}
          </div>

          {/* Thumbnail gallery */}
          {product.images.length > 1 && (
            <div className="flex gap-2 overflow-x-auto">
              {product.images.map((img, i) => (
                <button
                  key={img.id}
                  onClick={() => setSelectedImage(i)}
                  className={`relative w-20 h-20 flex-shrink-0 rounded-lg overflow-hidden border-2 ${
                    i === selectedImage ? "border-[var(--color-primary)]" : "border-transparent"
                  }`}
                >
                  <Image
                    src={img.url}
                    alt={img.altText || ""}
                    fill
                    className="object-cover"
                    sizes="80px"
                    unoptimized
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
              <div className="flex items-baseline gap-3">
                <span className="text-3xl font-bold text-[var(--color-secondary)]">
                  {formatPrice(product.salePrice || product.price)}
                </span>
                {product.salePrice && (
                  <span className="text-lg text-gray-400 line-through">
                    {formatPrice(product.price)}
                  </span>
                )}
              </div>
            )}
          </div>

          {/* Description */}
          {product.description && (
            <div className="mb-8">
              <h3 className="font-semibold text-[var(--color-secondary)] mb-2">{t("products.description")}</h3>
              <p className="text-gray-500 whitespace-pre-wrap">{product.description}</p>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3 mb-3">
            <button
              onClick={handleToggleWishlist}
              className={`p-4 rounded-xl border-2 transition-colors ${isWished ? "border-red-400 text-red-500" : "border-gray-200 text-gray-400 hover:border-red-300 hover:text-red-400"}`}
            >
              <Heart size={22} className={isWished ? "fill-red-500" : ""} />
            </button>
            <ShareButton title={product.name} text={product.description || undefined} />
            {isAuthenticated && (
              <button
                onClick={() => setShowCollectionModal(true)}
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
                  disabled={addingToCart}
                  className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-[var(--color-secondary)] text-white rounded-xl font-bold text-lg hover:opacity-90 transition-opacity disabled:opacity-60"
                >
                  <ShoppingCart size={22} /> {addingToCart ? t("products.addingToCart") : t("products.addToCart")}
                </button>
                <button className="flex-1 px-6 py-4 bg-[var(--color-primary)] text-white rounded-xl font-bold text-lg hover:opacity-90 transition-opacity">
                  {t("products.buyNow")}
                </button>
              </>
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
                            unoptimized
                          />
                        </div>
                      )}
                      {section.content && (
                        <div className="text-gray-600 whitespace-pre-wrap" dangerouslySetInnerHTML={{ __html: section.content }} />
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
                      unoptimized
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
