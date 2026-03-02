"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import { MessageCircle, Send } from "lucide-react";
import { useTranslations } from "next-intl";
import { ReactionPicker } from "@/components/ui/ReactionPicker";
import { getPost, toggleReaction, addComment } from "@/lib/postApi";
import { useAuthStore } from "@/stores/authStore";
import type { PostDto } from "@/types/post";

export default function PostDetailClient() {
  const t = useTranslations();
  const params = useParams();
  const { isAuthenticated, user } = useAuthStore();

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
  const [post, setPost] = useState<PostDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [comment, setComment] = useState("");
  const [replyTo, setReplyTo] = useState<number | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    const id = Number(params.id);
    if (!id) return;
    setLoading(true);
    getPost(id)
      .then(setPost)
      .catch(() => setPost(null))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [params.id]);

  const handleReaction = async (reactionType?: string) => {
    if (!post) return;
    try {
      const { isReacted } = await toggleReaction(post.id, reactionType || "like");
      setPost((p) =>
        p
          ? {
              ...p,
              myReaction: isReacted ? "like" : undefined,
              reactionCount: p.reactionCount + (isReacted ? 1 : -1),
            }
          : null
      );
    } catch {}
  };

  const handleComment = async () => {
    if (!post || !comment.trim()) return;
    setSubmitting(true);
    try {
      await addComment(post.id, comment, replyTo ?? undefined);
      setComment("");
      setReplyTo(null);
      load();
    } catch {
      alert(t("feed.commentFailed"));
    }
    setSubmitting(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!post) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500">{t("feed.postNotFound")}</p>
        <Link href="/feed" className="text-[var(--color-primary)] hover:underline mt-2 inline-block">
          {t("feed.backToFeed")}
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <Link
        href="/feed"
        className="text-sm text-gray-500 hover:text-[var(--color-primary)] mb-4 inline-block"
      >
        &larr; {t("feed.backToFeed")}
      </Link>

      <div className="bg-white rounded-xl shadow-sm p-6">
        {/* Author */}
        <div className="flex items-center gap-3 mb-4">
          <Link href={`/profile/${post.userId}`}>
            <div className="w-10 h-10 rounded-full bg-[var(--color-secondary)] flex items-center justify-center text-white font-bold">
              {post.userName.charAt(0)}
            </div>
          </Link>
          <div>
            <Link
              href={`/profile/${post.userId}`}
              className="font-medium text-[var(--color-secondary)] hover:text-[var(--color-primary)]"
            >
              {post.userName}
            </Link>
            <p className="text-xs text-gray-400">{timeAgo(post.createdAt)}</p>
          </div>
          <span className="text-xs px-2 py-0.5 bg-gray-100 text-gray-500 rounded-full ml-auto">
            {post.postType}
          </span>
        </div>

        {/* Content */}
        {post.title && (
          <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-3">
            {post.title}
          </h1>
        )}
        <p className="text-gray-700 whitespace-pre-wrap mb-4">{post.content}</p>

        {/* Images */}
        {post.images.length > 0 && (
          <div className="grid grid-cols-2 gap-2 mb-4">
            {post.images.map((img) => (
              <div
                key={img.id}
                className="relative aspect-square rounded-lg overflow-hidden bg-gray-100"
              >
                <Image
                  src={img.url}
                  alt={img.altText || ""}
                  fill
                  className="object-cover"
                  sizes="300px"
                  unoptimized
                />
              </div>
            ))}
          </div>
        )}

        {/* Hashtags */}
        {post.hashtags.length > 0 && (
          <div className="flex flex-wrap gap-1 mb-4">
            {post.hashtags.map((tag) => (
              <Link
                key={tag}
                href={`/hashtag/${tag}`}
                className="text-sm text-[var(--color-primary)] hover:underline"
              >
                #{tag}
              </Link>
            ))}
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center gap-4 py-3 border-y border-gray-100">
          <ReactionPicker
            currentReaction={post.myReaction}
            reactionCount={post.reactionCount}
            onReact={handleReaction}
            disabled={!isAuthenticated}
          />
          <span className="flex items-center gap-1 text-sm text-gray-400">
            <MessageCircle size={18} />
            {post.commentCount}
          </span>
        </div>

        {/* Comments */}
        <div className="mt-4 space-y-4">
          {post.comments?.map((c) => (
            <div key={c.id}>
              <div className="flex items-start gap-2">
                <div className="w-7 h-7 rounded-full bg-gray-200 flex items-center justify-center text-xs font-bold text-gray-500">
                  {c.userName.charAt(0)}
                </div>
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">{c.userName}</span>
                    <span className="text-xs text-gray-400">
                      {timeAgo(c.createdAt)}
                    </span>
                  </div>
                  <p className="text-sm text-gray-600">{c.content}</p>
                  {isAuthenticated && (
                    <button
                      onClick={() => setReplyTo(c.id)}
                      className="text-xs text-gray-400 hover:text-[var(--color-primary)] mt-1"
                    >
                      {t("feed.reply")}
                    </button>
                  )}

                  {/* Replies */}
                  {c.replies?.map((r) => (
                    <div key={r.id} className="flex items-start gap-2 mt-2 ml-4">
                      <div className="w-6 h-6 rounded-full bg-gray-100 flex items-center justify-center text-[10px] font-bold text-gray-400">
                        {r.userName.charAt(0)}
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium">{r.userName}</span>
                          <span className="text-xs text-gray-400">
                            {timeAgo(r.createdAt)}
                          </span>
                        </div>
                        <p className="text-sm text-gray-600">{r.content}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Comment input */}
        {isAuthenticated && (
          <div className="mt-4 pt-4 border-t border-gray-100">
            {replyTo && (
              <p className="text-xs text-[var(--color-primary)] mb-2">
                {t("feed.writingReply")}{" "}
                <button
                  onClick={() => setReplyTo(null)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  {t("feed.cancel")}
                </button>
              </p>
            )}
            <div className="flex gap-2">
              <input
                type="text"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && !e.shiftKey) {
                    e.preventDefault();
                    handleComment();
                  }
                }}
                placeholder={t("feed.writeComment")}
                className="flex-1 px-3 py-2.5 border rounded-lg text-sm"
              />
              <button
                onClick={handleComment}
                disabled={submitting || !comment.trim()}
                className="p-2.5 bg-[var(--color-primary)] text-white rounded-lg disabled:opacity-60"
              >
                <Send size={16} />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
