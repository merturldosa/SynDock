"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Image from "next/image";
import Link from "next/link";
import { ChevronLeft, X } from "lucide-react";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import {
  getCollectionDetail,
  removeFromCollection,
  type CollectionDetail,
} from "@/lib/collectionApi";
import { formatPrice } from "@/lib/format";

export default function CollectionDetailPage() {
  const t = useTranslations();
  const params = useParams();
  const router = useRouter();
  const [collection, setCollection] = useState<CollectionDetail | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchData = () => {
    const id = Number(params.id);
    if (!id) return;
    setLoading(true);
    getCollectionDetail(id)
      .then(setCollection)
      .catch(() => setCollection(null))
      .finally(() => setLoading(false));
  };

  useEffect(fetchData, [params.id]);

  const handleRemove = async (productId: number) => {
    if (!collection) return;
    try {
      await removeFromCollection(collection.id, productId);
      fetchData();
    } catch {
      toast.error(t("mypage.collections.deleteFailed"));
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!collection) {
    return (
      <div className="text-center py-20 text-gray-400">
        {t("mypage.collections.notFound")}
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto px-4 py-8">
      <button
        onClick={() => router.back()}
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-[var(--color-secondary)] mb-4"
      >
        <ChevronLeft size={16} /> {t("common.back")}
      </button>

      <div className="mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          {collection.name}
        </h1>
        {collection.description && (
          <p className="text-gray-500 mt-1">{collection.description}</p>
        )}
        <p className="text-sm text-gray-400 mt-2">
          {t("mypage.collections.itemCount", { count: collection.items.length })} · {collection.isPublic ? t("mypage.collections.public") : t("mypage.collections.private")}
        </p>
      </div>

      {collection.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          {t("mypage.collections.emptyItems")}
        </div>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {collection.items.map((item) => (
            <div
              key={item.productId}
              className="bg-white rounded-xl border overflow-hidden group relative"
            >
              <button
                onClick={() => handleRemove(item.productId)}
                aria-label="Remove from collection"
                className="absolute top-2 right-2 z-10 p-1.5 bg-white/90 rounded-full text-gray-400 hover:text-red-500 opacity-0 group-hover:opacity-100 transition-opacity"
              >
                <X size={14} />
              </button>
              <Link href={`/products/${item.productId}`}>
                <div className="relative aspect-square bg-gray-50">
                  {item.imageUrl ? (
                    <Image
                      src={item.imageUrl}
                      alt={item.productName}
                      fill
                      className="object-contain"
                      sizes="(max-width: 768px) 50vw, 25vw"
                    />
                  ) : (
                    <div className="flex items-center justify-center h-full text-4xl opacity-20">
                      📦
                    </div>
                  )}
                </div>
                <div className="p-3">
                  <p className="text-sm font-medium text-gray-800 line-clamp-2 mb-1">
                    {item.productName}
                  </p>
                  <p className="text-sm font-bold text-[var(--color-secondary)]">
                    {item.salePrice
                      ? formatPrice(item.salePrice)
                      : formatPrice(item.price)}
                  </p>
                  {item.note && (
                    <p className="text-xs text-gray-400 mt-1 line-clamp-1">
                      {item.note}
                    </p>
                  )}
                </div>
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
