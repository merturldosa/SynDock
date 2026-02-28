"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { Hash, Heart, MessageCircle } from "lucide-react";
import { getPostsByHashtag } from "@/lib/postApi";
import type { PagedPosts } from "@/types/post";

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins}분 전`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}시간 전`;
  return new Date(dateStr).toLocaleDateString("ko-KR");
}

export default function HashtagPage() {
  const params = useParams();
  const tag = params.tag as string;
  const [data, setData] = useState<PagedPosts | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!tag) return;
    setLoading(true);
    getPostsByHashtag(decodeURIComponent(tag), page)
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [tag, page]);

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center gap-2 mb-6">
        <Hash size={24} className="text-[var(--color-primary)]" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          #{decodeURIComponent(tag)}
        </h1>
        {data && (
          <span className="text-sm text-gray-400 ml-2">
            {data.totalCount}개 게시글
          </span>
        )}
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>이 해시태그에 게시글이 없습니다.</p>
          <Link
            href="/feed"
            className="text-[var(--color-primary)] hover:underline mt-2 inline-block"
          >
            피드로 돌아가기
          </Link>
        </div>
      ) : (
        <div className="space-y-4">
          {data.items.map((post) => (
            <Link
              key={post.id}
              href={`/feed/${post.id}`}
              className="block bg-white rounded-xl shadow-sm p-5 hover:shadow-md transition-shadow"
            >
              <div className="flex items-center gap-2 mb-2">
                <span className="text-sm font-medium text-[var(--color-secondary)]">
                  {post.userName}
                </span>
                <span className="text-xs text-gray-400">
                  {timeAgo(post.createdAt)}
                </span>
              </div>
              {post.title && (
                <h3 className="font-medium text-[var(--color-secondary)] mb-1">
                  {post.title}
                </h3>
              )}
              <p className="text-sm text-gray-600 mb-3">
                {post.contentPreview}
              </p>
              <div className="flex items-center gap-4 text-xs text-gray-400">
                <span className="flex items-center gap-1">
                  <Heart size={14} /> {post.reactionCount}
                </span>
                <span className="flex items-center gap-1">
                  <MessageCircle size={14} /> {post.commentCount}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
