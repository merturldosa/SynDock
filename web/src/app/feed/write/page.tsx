"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Upload, X, Hash } from "lucide-react";
import { createPost, uploadImage } from "@/lib/postApi";
import { useAuthStore } from "@/stores/authStore";

export default function FeedWritePage() {
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [postType, setPostType] = useState("general");
  const [imageUrls, setImageUrls] = useState<string[]>([]);
  const [hashtagInput, setHashtagInput] = useState("");
  const [hashtags, setHashtags] = useState<string[]>([]);
  const [uploading, setUploading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  if (!isAuthenticated) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500">로그인 후 글을 작성할 수 있습니다.</p>
      </div>
    );
  }

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files) return;
    setUploading(true);
    for (const file of Array.from(files)) {
      try {
        const { url } = await uploadImage(file, "posts");
        setImageUrls((prev) => [...prev, url]);
      } catch {}
    }
    setUploading(false);
    e.target.value = "";
  };

  const addHashtag = () => {
    const tag = hashtagInput.trim().toLowerCase().replace(/^#/, "");
    if (tag && !hashtags.includes(tag)) {
      setHashtags((prev) => [...prev, tag]);
    }
    setHashtagInput("");
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) {
      alert("내용을 입력해 주세요.");
      return;
    }
    setSubmitting(true);
    try {
      await createPost({
        title: title || undefined,
        content,
        postType,
        imageUrls: imageUrls.length > 0 ? imageUrls : undefined,
        hashtags: hashtags.length > 0 ? hashtags : undefined,
      });
      router.push("/feed");
    } catch {
      alert("게시글 작성에 실패했습니다.");
    }
    setSubmitting(false);
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        글쓰기
      </h1>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="bg-white rounded-xl shadow-sm p-5 space-y-4">
          <select
            value={postType}
            onChange={(e) => setPostType(e.target.value)}
            className="px-3 py-2 border rounded-lg text-sm"
          >
            <option value="general">일반</option>
            <option value="review">리뷰</option>
            <option value="daily">일상</option>
          </select>

          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="제목 (선택)"
            className="w-full px-3 py-2.5 border rounded-lg text-sm"
          />

          <textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="무슨 이야기를 나누고 싶으신가요?"
            rows={6}
            className="w-full px-3 py-2.5 border rounded-lg text-sm resize-none"
            required
          />

          {/* Images */}
          <div className="flex flex-wrap gap-2">
            {imageUrls.map((url, i) => (
              <div
                key={i}
                className="relative w-20 h-20 rounded-lg overflow-hidden border"
              >
                <img src={url} alt="" className="w-full h-full object-cover" />
                <button
                  type="button"
                  onClick={() =>
                    setImageUrls((prev) => prev.filter((_, idx) => idx !== i))
                  }
                  className="absolute top-0.5 right-0.5 p-0.5 bg-red-500 text-white rounded-full"
                >
                  <X size={10} />
                </button>
              </div>
            ))}
            <label className="w-20 h-20 border-2 border-dashed rounded-lg flex flex-col items-center justify-center cursor-pointer text-gray-400 hover:border-[var(--color-primary)] hover:text-[var(--color-primary)]">
              <Upload size={18} />
              <span className="text-[10px] mt-0.5">
                {uploading ? "..." : "사진"}
              </span>
              <input
                type="file"
                accept="image/*"
                multiple
                onChange={handleImageUpload}
                className="hidden"
                disabled={uploading}
              />
            </label>
          </div>

          {/* Hashtags */}
          <div>
            <div className="flex gap-2 mb-2">
              <div className="relative flex-1">
                <Hash
                  size={14}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
                />
                <input
                  type="text"
                  value={hashtagInput}
                  onChange={(e) => setHashtagInput(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      addHashtag();
                    }
                  }}
                  placeholder="해시태그 입력 후 Enter"
                  className="w-full pl-8 pr-3 py-2 border rounded-lg text-sm"
                />
              </div>
            </div>
            {hashtags.length > 0 && (
              <div className="flex flex-wrap gap-1">
                {hashtags.map((tag) => (
                  <span
                    key={tag}
                    className="flex items-center gap-1 text-xs bg-[var(--color-primary)]/10 text-[var(--color-primary)] px-2 py-1 rounded-full"
                  >
                    #{tag}
                    <button
                      type="button"
                      onClick={() =>
                        setHashtags((prev) => prev.filter((t) => t !== tag))
                      }
                    >
                      <X size={10} />
                    </button>
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="flex gap-3">
          <button
            type="button"
            onClick={() => router.back()}
            className="flex-1 py-3 border rounded-lg text-sm font-medium text-gray-500"
          >
            취소
          </button>
          <button
            type="submit"
            disabled={submitting || !content.trim()}
            className="flex-1 py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
          >
            {submitting ? "게시 중..." : "게시하기"}
          </button>
        </div>
      </form>
    </div>
  );
}
