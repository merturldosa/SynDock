"use client";

import { useEffect, useState } from "react";
import { Star, Trash2 } from "lucide-react";
import { getProductReviews, createReview, deleteReview } from "@/lib/reviewApi";
import { useAuthStore } from "@/stores/authStore";
import type { ReviewSummary } from "@/types/review";

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
  const { isAuthenticated, user } = useAuthStore();
  const [data, setData] = useState<ReviewSummary | null>(null);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [newRating, setNewRating] = useState(5);
  const [newContent, setNewContent] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    getProductReviews(productId, page).then(setData).catch(() => {});
  };

  useEffect(() => { load(); }, [productId, page]);

  const handleSubmit = async () => {
    if (newRating < 1) return;
    setSubmitting(true);
    try {
      await createReview(productId, newRating, newContent || undefined);
      setShowForm(false);
      setNewContent("");
      setNewRating(5);
      load();
    } catch {
      alert("리뷰 작성에 실패했습니다.");
    }
    setSubmitting(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm("리뷰를 삭제하시겠습니까?")) return;
    try {
      await deleteReview(id);
      load();
    } catch {
      alert("삭제에 실패했습니다.");
    }
  };

  if (!data) return <div className="py-8 text-center text-gray-400">로딩 중...</div>;

  return (
    <div>
      {/* Summary */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <StarRating rating={Math.round(data.averageRating)} />
          <span className="text-lg font-bold text-[var(--color-secondary)]">{data.averageRating.toFixed(1)}</span>
          <span className="text-sm text-gray-400">({data.totalCount}개)</span>
        </div>
        {isAuthenticated && (
          <button
            onClick={() => setShowForm(!showForm)}
            className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90"
          >
            리뷰 작성
          </button>
        )}
      </div>

      {/* Write form */}
      {showForm && (
        <div className="bg-gray-50 rounded-xl p-4 mb-6 space-y-3">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-[var(--color-secondary)]">별점:</span>
            <StarRating rating={newRating} onRate={setNewRating} interactive />
          </div>
          <textarea
            value={newContent}
            onChange={(e) => setNewContent(e.target.value)}
            placeholder="리뷰를 작성해 주세요..."
            className="w-full px-3 py-2 border rounded-lg text-sm resize-none h-24"
          />
          <div className="flex justify-end gap-2">
            <button onClick={() => setShowForm(false)} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">취소</button>
            <button
              onClick={handleSubmit}
              disabled={submitting}
              className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90 disabled:opacity-60"
            >
              {submitting ? "등록 중..." : "등록"}
            </button>
          </div>
        </div>
      )}

      {/* Reviews list */}
      {data.reviews.length === 0 ? (
        <p className="text-center text-gray-400 py-8">아직 리뷰가 없습니다.</p>
      ) : (
        <div className="space-y-4">
          {data.reviews.map((review) => (
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
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
