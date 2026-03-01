"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { MessageCircleQuestion, Trash2, Lock } from "lucide-react";
import { getMyQnAs, deleteQnA, type MyQnA } from "@/lib/reviewApi";

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("ko-KR", {
    year: "numeric", month: "short", day: "numeric",
  });
}

export default function MyQnAPage() {
  const [qnas, setQnAs] = useState<MyQnA[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 10;

  const load = (p: number) => {
    setLoading(true);
    getMyQnAs(p, pageSize)
      .then((res) => {
        setQnAs(res.items);
        setTotalCount(res.totalCount);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(page); }, [page]);

  const handleDelete = async (id: number) => {
    if (!confirm("이 QnA를 삭제하시겠습니까?")) return;
    try {
      await deleteQnA(id);
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
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">내 QnA</h1>

      {qnas.length === 0 ? (
        <div className="text-center py-20">
          <MessageCircleQuestion size={64} className="mx-auto text-gray-300 mb-6" />
          <p className="text-gray-500 mb-4">작성한 QnA가 없습니다.</p>
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
            {qnas.map((qna) => (
              <div key={qna.id} className="bg-white rounded-xl shadow-sm p-5 flex gap-4">
                <Link href={`/products/${qna.productId}`} className="shrink-0">
                  <div className="relative w-16 h-16 rounded-lg overflow-hidden bg-gray-100">
                    {qna.productImageUrl ? (
                      <Image src={qna.productImageUrl} alt="" fill className="object-cover" sizes="64px" unoptimized />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-gray-300">
                        <MessageCircleQuestion size={24} />
                      </div>
                    )}
                  </div>
                </Link>
                <div className="flex-1 min-w-0">
                  <Link href={`/products/${qna.productId}`} className="text-xs text-gray-400 hover:text-[var(--color-primary)]">
                    {qna.productName}
                  </Link>
                  <div className="flex items-center gap-2 mt-1">
                    {qna.isSecret && <Lock size={12} className="text-gray-400" />}
                    <p className="text-sm font-medium text-[var(--color-secondary)] line-clamp-1">
                      {qna.title}
                    </p>
                  </div>
                  <div className="flex items-center gap-2 mt-1.5">
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full ${
                        qna.isAnswered
                          ? "bg-emerald-100 text-emerald-700"
                          : "bg-gray-100 text-gray-500"
                      }`}
                    >
                      {qna.isAnswered ? "답변완료" : "미답변"}
                    </span>
                    <span className="text-xs text-gray-400">{formatDate(qna.createdAt)}</span>
                  </div>
                </div>
                <button
                  onClick={() => handleDelete(qna.id)}
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
