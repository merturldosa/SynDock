"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Star, Trash2 } from "lucide-react";
import { getMyReviews, deleteReview, type MyReview } from "@/lib/reviewApi";

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("ko-KR", {
    year: "numeric", month: "short", day: "numeric",
  });
}

export default function MyReviewsPage() {
  const [reviews, setReviews] = useState<MyReview[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 10;

  const load = (p: number) => {
    setLoading(true);
    getMyReviews(p, pageSize)
      .then((res) => {
        setReviews(res.items);
        setTotalCount(res.totalCount);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(page); }, [page]);

  const handleDelete = async (id: number) => {
    if (!confirm("이 리뷰를 삭제하시겠습니까?")) return;
    try {
      await deleteReview(id);
      load(page);
    } catch {
      alert("삭제에 실패했습니다.");
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">내 리뷰</h1>

      {reviews.length === 0 ? (
        <div className="text-center py-20">
          <Star size={64} className="mx-auto text-gray-300 mb-6" />
          <p className="text-gray-500 mb-4">작성한 리뷰가 없습니다.</p>
          <Link
            href="/products"
            className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
          >
            상품 둘러보기
          </Link>
        </div>
      ) : (
        <>
          <div className="space-y-3">
            {reviews.map((review) => (
              <div key={review.id} className="bg-white rounded-xl shadow-sm p-5 flex gap-4">
                <Link href={`/products/${review.productId}`} className="shrink-0">
                  <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-gray-100">
                    {review.productImageUrl ? (
                      <Image src={review.productImageUrl} alt="" fill className="object-cover" sizes="64px" unoptimized />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-gray-300">
                        <Star size={24} />
                      </div>
                    )}
                  </div>
                </Link>
                <div className="flex-1 min-w-0">
                  <Link href={`/products/${review.productId}`} className="text-sm font-medium text-[var(--color-secondary)] hover:text-[var(--color-primary)] line-clamp-1">
                    {review.productName}
                  </Link>
                  <div className="flex items-center gap-1 mt-1">
                    {Array.from({ length: 5 }, (_, i) => (
                      <Star
                        key={i}
                        size={14}
                        className={i < review.rating ? "text-amber-400 fill-amber-400" : "text-gray-200"}
                      />
                    ))}
                    <span className="text-xs text-gray-400 ml-2">{formatDate(review.createdAt)}</span>
                  </div>
                  {review.content && (
                    <p className="text-sm text-gray-600 mt-2 line-clamp-2">{review.content}</p>
                  )}
                </div>
                <button
                  onClick={() => handleDelete(review.id)}
                  className="p-2 text-gray-400 hover:text-red-500 transition-colors shrink-0 self-start"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="flex justify-center gap-2 mt-8">
              {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                <button
                  key={p}
                  onClick={() => setPage(p)}
                  className={`w-9 h-9 rounded-lg text-sm font-medium transition-colors ${
                    p === page
                      ? "bg-[var(--color-primary)] text-white"
                      : "text-gray-500 hover:bg-gray-100"
                  }`}
                >
                  {p}
                </button>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}
