"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import Image from "next/image";
import { Star, Trash2, Camera, X } from "lucide-react";
import { getProductReviews, createReview, deleteReview } from "@/lib/reviewApi";
import { useAuthStore } from "@/stores/authStore";
import type { ReviewSummary } from "@/types/review";
import api from "@/lib/api";

interface ReviewTabProps {
  productId: number;
}

function StarRating({ rating, onRate, interactive = false }: { rating: number; onRate?: (r: number) => void; interactive?: boolean }) {
  return (
    <div className="flex gap-0.5">
      {[1, 2, 3, 4, 5].map((i) => (
        <button
          key={i}
          type="button"
          disabled={!interactive}
          onClick={() => onRate?.(i)}
          className={interactive ? "cursor-pointer hover:scale-110 transition-transform" : "cursor-default"}
        >
          <Star size={18} className={i <= rating ? "fill-yellow-400 text-yellow-400" : "text-gray-300"} />
        </button>
      ))}
    </div>
  );
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("ko-KR", { year: "numeric", month: "short", day: "numeric" });
}

export function ReviewTab({ productId }: ReviewTabProps) {
  const t = useTranslations();
  const { isAuthenticated, user } = useAuthStore();
  const [data, setData] = useState<ReviewSummary | null>(null);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [newRating, setNewRating] = useState(5);
  const [newContent, setNewContent] = useState("");
  const [newImageUrl, setNewImageUrl] = useState("");
  const [uploading, setUploading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [photoOnly, setPhotoOnly] = useState(false);
  const [expandedImage, setExpandedImage] = useState<string | null>(null);

  const load = () => {
    getProductReviews(productId, page).then(setData).catch(() => {});
  };

  useEffect(() => { load(); }, [productId, page]);

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append("file", file);
      const { data: result } = await api.post("/upload/image?folder=reviews", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      setNewImageUrl(result.url);
    } catch {
      alert(t("review.uploadFailed"));
    }
    setUploading(false);
  };

  const handleSubmit = async () => {
    if (newRating < 1) return;
    setSubmitting(true);
    try {
      await createReview(productId, newRating, newContent || undefined, newImageUrl || undefined);
      setShowForm(false);
      setNewContent("");
      setNewRating(5);
      setNewImageUrl("");
      load();
    } catch {
      alert(t("review.writeFailed"));
    }
    setSubmitting(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm(t("review.deleteConfirm"))) return;
    try {
      await deleteReview(id);
      load();
    } catch {
      alert(t("review.deleteFailed"));
    }
  };

  if (!data) return <div className="py-8 text-center text-gray-400">{t("common.loading")}</div>;

  const filteredReviews = photoOnly
    ? data.reviews.filter((r) => r.imageUrl)
    : data.reviews;

  const photoReviews = data.reviews.filter((r) => r.imageUrl);

  return (
    <div>
      {/* Summary + Rating Distribution */}
      <div className="flex flex-col md:flex-row gap-6 mb-6">
        <div className="flex items-center gap-3">
          <StarRating rating={Math.round(data.averageRating)} />
          <span className="text-lg font-bold text-[var(--color-secondary)]">{data.averageRating.toFixed(1)}</span>
          <span className="text-sm text-gray-400">{t("review.count", { count: data.totalCount })}</span>
        </div>

        {/* Rating Distribution */}
        {data.ratingDistribution.length > 0 && (
          <div className="flex-1 space-y-1">
            {[5, 4, 3, 2, 1].map((rating) => {
              const dist = data.ratingDistribution.find((d) => d.rating === rating);
              const count = dist?.count || 0;
              const pct = data.totalCount > 0 ? (count / data.totalCount) * 100 : 0;
              return (
                <div key={rating} className="flex items-center gap-2 text-xs">
                  <span className="w-8 text-right text-gray-500">{rating}{t("review.ratingUnit")}</span>
                  <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                    <div className="h-full bg-yellow-400 rounded-full" style={{ width: `${pct}%` }} />
                  </div>
                  <span className="w-8 text-gray-400">{count}</span>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Photo Gallery Strip */}
      {photoReviews.length > 0 && (
        <div className="mb-6">
          <p className="text-sm font-medium text-[var(--color-secondary)] mb-2">
            {t("review.photoReview")} ({data.photoReviewCount})
          </p>
          <div className="flex gap-2 overflow-x-auto pb-2">
            {photoReviews.map((r) => (
              <button
                key={r.id}
                onClick={() => setExpandedImage(r.imageUrl)}
                className="w-16 h-16 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0 hover:opacity-80 transition-opacity"
              >
                <Image src={r.imageUrl!} alt="" width={64} height={64} className="object-cover w-full h-full" unoptimized />
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          {data.photoReviewCount > 0 && (
            <button
              onClick={() => setPhotoOnly(!photoOnly)}
              className={`text-sm px-3 py-1.5 rounded-full transition-colors ${
                photoOnly ? "bg-[var(--color-primary)] text-white" : "bg-gray-100 text-gray-600 hover:bg-gray-200"
              }`}
            >
              <Camera size={14} className="inline mr-1" />
              {t("review.photoOnly")}
            </button>
          )}
        </div>
        {isAuthenticated && (
          <button
            onClick={() => setShowForm(!showForm)}
            className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90"
          >
            {t("review.writeReview")}
          </button>
        )}
      </div>

      {/* Write form */}
      {showForm && (
        <div className="bg-gray-50 rounded-xl p-4 mb-6 space-y-3">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-[var(--color-secondary)]">{t("review.rating")}:</span>
            <StarRating rating={newRating} onRate={setNewRating} interactive />
          </div>
          <textarea
            value={newContent}
            onChange={(e) => setNewContent(e.target.value)}
            placeholder={t("review.placeholder")}
            className="w-full px-3 py-2 border rounded-lg text-sm resize-none h-24"
          />
          {/* Photo upload */}
          <div>
            <label className="flex items-center gap-2 text-sm text-gray-500 cursor-pointer hover:text-[var(--color-primary)]">
              <Camera size={16} />
              <span>{t("review.attachPhoto")}</span>
              <input type="file" accept="image/*" onChange={handleImageUpload} className="hidden" disabled={uploading} />
            </label>
            {uploading && <span className="text-xs text-gray-400 ml-2">{t("review.uploading")}</span>}
            {newImageUrl && (
              <div className="mt-2 flex items-start gap-2">
                <div className="w-20 h-20 rounded-lg overflow-hidden bg-gray-100">
                  <Image src={newImageUrl} alt="" width={80} height={80} className="object-cover w-full h-full" unoptimized />
                </div>
                <button onClick={() => setNewImageUrl("")} className="text-gray-400 hover:text-red-500">
                  <X size={14} />
                </button>
              </div>
            )}
          </div>
          <div className="flex justify-end gap-2">
            <button onClick={() => { setShowForm(false); setNewImageUrl(""); }} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">{t("common.cancel")}</button>
            <button
              onClick={handleSubmit}
              disabled={submitting}
              className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90 disabled:opacity-60"
            >
              {submitting ? t("common.submitting") : t("common.submit")}
            </button>
          </div>
        </div>
      )}

      {/* Reviews list */}
      {filteredReviews.length === 0 ? (
        <p className="text-center text-gray-400 py-8">
          {photoOnly ? t("review.noPhotoReviews") : t("review.noReviews")}
        </p>
      ) : (
        <div className="space-y-4">
          {filteredReviews.map((review) => (
            <div key={review.id} className="border-b border-gray-100 pb-4">
              <div className="flex items-center justify-between mb-1">
                <div className="flex items-center gap-2">
                  <StarRating rating={review.rating} />
                  <span className="text-sm font-medium text-[var(--color-secondary)]">{review.userName}</span>
                  <span className="text-xs text-gray-400">{formatDate(review.createdAt)}</span>
                </div>
                {user?.id === review.userId && (
                  <button onClick={() => handleDelete(review.id)} className="text-gray-400 hover:text-red-500">
                    <Trash2 size={14} />
                  </button>
                )}
              </div>
              {review.content && <p className="text-sm text-gray-600 mt-1">{review.content}</p>}
              {review.imageUrl && (
                <button
                  onClick={() => setExpandedImage(review.imageUrl)}
                  className="mt-2 w-24 h-24 rounded-lg overflow-hidden bg-gray-100 hover:opacity-80 transition-opacity"
                >
                  <Image src={review.imageUrl} alt="" width={96} height={96} className="object-cover w-full h-full" unoptimized />
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Image Lightbox */}
      {expandedImage && (
        <div
          className="fixed inset-0 bg-black/70 z-50 flex items-center justify-center p-4"
          onClick={() => setExpandedImage(null)}
        >
          <div className="relative max-w-2xl max-h-[80vh]">
            <Image src={expandedImage} alt="" width={800} height={800} className="object-contain max-h-[80vh] rounded-xl" unoptimized />
            <button
              onClick={() => setExpandedImage(null)}
              className="absolute top-2 right-2 w-8 h-8 bg-white/80 rounded-full flex items-center justify-center text-gray-700 hover:bg-white"
            >
              <X size={18} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
