"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { UserCheck, UserPlus } from "lucide-react";
import { getSocialProfile, toggleFollow, getFeed } from "@/lib/postApi";
import { useAuthStore } from "@/stores/authStore";
import type { SocialProfile, PagedPosts } from "@/types/post";

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins}분 전`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}시간 전`;
  return new Date(dateStr).toLocaleDateString("ko-KR");
}

export default function ProfilePage() {
  const params = useParams();
  const { isAuthenticated, user } = useAuthStore();
  const [profile, setProfile] = useState<SocialProfile | null>(null);
  const [posts, setPosts] = useState<PagedPosts | null>(null);
  const [loading, setLoading] = useState(true);

  const userId = Number(params.id);

  useEffect(() => {
    if (!userId) return;
    setLoading(true);
    Promise.all([getSocialProfile(userId), getFeed(1, 20, undefined, userId)])
      .then(([p, f]) => {
        setProfile(p);
        setPosts(f);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [userId]);

  const handleFollow = async () => {
    if (!profile) return;
    try {
      const { isFollowing } = await toggleFollow(profile.userId);
      setProfile((p) =>
        p
          ? {
              ...p,
              isFollowing,
              followerCount: p.followerCount + (isFollowing ? 1 : -1),
            }
          : null
      );
    } catch {}
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center text-gray-500">
        사용자를 찾을 수 없습니다.
      </div>
    );
  }

  const isMe = user?.id === profile.userId;

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      {/* Profile Header */}
      <div className="bg-white rounded-xl shadow-sm p-6 mb-6">
        <div className="flex items-center gap-4">
          <div className="w-16 h-16 rounded-full bg-[var(--color-secondary)] flex items-center justify-center text-white text-2xl font-bold">
            {profile.userName.charAt(0)}
          </div>
          <div className="flex-1">
            <h1 className="text-xl font-bold text-[var(--color-secondary)]">
              {profile.name || profile.userName}
            </h1>
            <p className="text-sm text-gray-500">@{profile.userName}</p>
          </div>
          {isAuthenticated && !isMe && (
            <button
              onClick={handleFollow}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                profile.isFollowing
                  ? "bg-gray-100 text-gray-600 hover:bg-red-50 hover:text-red-500"
                  : "bg-[var(--color-primary)] text-white hover:opacity-90"
              }`}
            >
              {profile.isFollowing ? (
                <>
                  <UserCheck size={16} /> 팔로잉
                </>
              ) : (
                <>
                  <UserPlus size={16} /> 팔로우
                </>
              )}
            </button>
          )}
        </div>

        <div className="flex items-center gap-6 mt-4 pt-4 border-t border-gray-100">
          <div className="text-center">
            <p className="text-lg font-bold text-[var(--color-secondary)]">
              {profile.postCount}
            </p>
            <p className="text-xs text-gray-400">게시글</p>
          </div>
          <div className="text-center">
            <p className="text-lg font-bold text-[var(--color-secondary)]">
              {profile.followerCount}
            </p>
            <p className="text-xs text-gray-400">팔로워</p>
          </div>
          <div className="text-center">
            <p className="text-lg font-bold text-[var(--color-secondary)]">
              {profile.followingCount}
            </p>
            <p className="text-xs text-gray-400">팔로잉</p>
          </div>
        </div>
      </div>

      {/* Posts */}
      <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-4">
        게시글
      </h2>
      {!posts || posts.items.length === 0 ? (
        <p className="text-center py-10 text-gray-400">
          아직 게시글이 없습니다.
        </p>
      ) : (
        <div className="space-y-3">
          {posts.items.map((post) => (
            <Link
              key={post.id}
              href={`/feed/${post.id}`}
              className="block bg-white rounded-xl shadow-sm p-4 hover:shadow-md transition-shadow"
            >
              {post.title && (
                <h3 className="font-medium text-[var(--color-secondary)] mb-1">
                  {post.title}
                </h3>
              )}
              <p className="text-sm text-gray-600 line-clamp-2">
                {post.contentPreview}
              </p>
              <p className="text-xs text-gray-400 mt-2">
                {timeAgo(post.createdAt)}
              </p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
