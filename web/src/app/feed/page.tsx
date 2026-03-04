"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { MessageCircle, Hash, PenSquare } from "lucide-react";
import { useTranslations } from "next-intl";
import { getFeed, getTrendingHashtags, toggleReaction } from "@/lib/postApi";
import { useAuthStore } from "@/stores/authStore";
import type { PostSummary, PagedPosts, HashtagInfo } from "@/types/post";

export default function FeedPage() {
  const t = useTranslations();
  const { isAuthenticated } = useAuthStore();

  const timeAgo = (dateStr: string): string => {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return t("feed.justNow");
    if (mins < 60) return t("feed.minutesAgo", { count: mins });
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return t("feed.hoursAgo", { count: hrs });
    const days = Math.floor(hrs / 24);
    if (days < 30) return t("feed.daysAgo", { count: days });
    return new Date(dateStr).toLocaleDateString();
  };
  const [data, setData] = useState<PagedPosts | null>(null);
  const [trending, setTrending] = useState<HashtagInfo[]>([]);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    Promise.all([getFeed(page), getTrendingHashtags(10)])
      .then(([feed, tags]) => {
        setData(feed);
        setTrending(tags);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [page]);

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          {t("feed.title")}
        </h1>
        {isAuthenticated && (
          <Link
            href="/feed/write"
            className="flex items-center gap-2 px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90"
          >
            <PenSquare size={16} /> {t("feed.write")}
          </Link>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Feed */}
        <div className="lg:col-span-2">
          {loading ? (
            <div className="flex items-center justify-center py-20">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
            </div>
          ) : !data || data.items.length === 0 ? (
            <div className="text-center py-20 text-gray-400">
              <MessageCircle size={48} className="mx-auto mb-4 opacity-50" />
              <p>{t("feed.empty")}</p>
            </div>
          ) : (
            <div className="space-y-4">
              {data.items.map((post) => (
                <Link
                  key={post.id}
                  href={`/feed/${post.id}`}
                  className="block bg-white rounded-xl shadow-sm p-5 hover:shadow-md transition-shadow"
                >
                  <div className="flex items-center gap-2 mb-3">
                    <div className="w-8 h-8 rounded-full bg-[var(--color-secondary)] flex items-center justify-center text-white text-xs font-bold">
                      {post.userName.charAt(0)}
                    </div>
                    <span className="text-sm font-medium text-[var(--color-secondary)]">
                      {post.userName}
                    </span>
                    <span className="text-xs text-gray-400">
                      {timeAgo(post.createdAt)}
                    </span>
                    <span className="text-xs px-2 py-0.5 bg-gray-100 text-gray-500 rounded-full">
                      {post.postType}
                    </span>
                  </div>

                  {post.title && (
                    <h3 className="font-semibold text-[var(--color-secondary)] mb-1">
                      {post.title}
                    </h3>
                  )}
                  <p className="text-sm text-gray-600 mb-3">
                    {post.contentPreview}
                  </p>

                  {post.thumbnailUrl && (
                    <div className="relative w-full h-48 rounded-lg overflow-hidden bg-gray-100 mb-3">
                      <Image
                        src={post.thumbnailUrl}
                        alt=""
                        fill
                        className="object-cover"
                        sizes="600px"
                      />
                    </div>
                  )}

                  {post.hashtags.length > 0 && (
                    <div className="flex flex-wrap gap-1 mb-3">
                      {post.hashtags.map((tag) => (
                        <span
                          key={tag}
                          className="text-xs text-[var(--color-primary)] bg-[var(--color-primary)]/5 px-2 py-0.5 rounded-full"
                        >
                          #{tag}
                        </span>
                      ))}
                    </div>
                  )}

                  <div className="flex items-center gap-4 text-xs text-gray-400">
                    <span className="flex items-center gap-1">
                      🙏 {post.reactionCount}
                    </span>
                    <span className="flex items-center gap-1">
                      <MessageCircle size={14} /> {post.commentCount}
                    </span>
                  </div>
                </Link>
              ))}

              {data.totalCount > data.pageSize && (
                <div className="flex items-center justify-center gap-2 mt-4">
                  <button
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
                  >
                    {t("feed.prev")}
                  </button>
                  <span className="text-sm text-gray-500">
                    {page} / {Math.ceil(data.totalCount / data.pageSize)}
                  </span>
                  <button
                    onClick={() => setPage((p) => p + 1)}
                    disabled={page * data.pageSize >= data.totalCount}
                    className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
                  >
                    {t("feed.next")}
                  </button>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Sidebar - Trending */}
        <div className="hidden lg:block">
          <div className="bg-white rounded-xl shadow-sm p-5 sticky top-24">
            <h3 className="font-semibold text-[var(--color-secondary)] mb-4 flex items-center gap-2">
              <Hash size={18} className="text-[var(--color-primary)]" />
              {t("feed.trending")}
            </h3>
            {trending.length === 0 ? (
              <p className="text-sm text-gray-400">{t("feed.noHashtags")}</p>
            ) : (
              <div className="space-y-2">
                {trending.map((tag) => (
                  <Link
                    key={tag.id}
                    href={`/hashtag/${tag.tag}`}
                    className="flex items-center justify-between py-1.5 text-sm hover:text-[var(--color-primary)] transition-colors"
                  >
                    <span className="text-[var(--color-secondary)]">
                      #{tag.tag}
                    </span>
                    <span className="text-xs text-gray-400">
                      {tag.postCount}
                    </span>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
