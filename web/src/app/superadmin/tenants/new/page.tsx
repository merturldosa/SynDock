"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createPlatformTenant } from "@/lib/platformApi";

const PRESET_THEMES = [
  {
    name: "Sacred Gold",
    theme: {
      primary: "#C9A84C",
      primaryLight: "#D4BA6A",
      secondary: "#1B2A4A",
      secondaryLight: "#2A3D66",
      background: "#FAF8F5",
    },
  },
  {
    name: "Modern Blue",
    theme: {
      primary: "#3B82F6",
      primaryLight: "#60A5FA",
      secondary: "#1E293B",
      secondaryLight: "#334155",
      background: "#F8FAFC",
    },
  },
  {
    name: "Nature Green",
    theme: {
      primary: "#22C55E",
      primaryLight: "#4ADE80",
      secondary: "#14532D",
      secondaryLight: "#166534",
      background: "#F0FDF4",
    },
  },
  {
    name: "Luxury Purple",
    theme: {
      primary: "#A855F7",
      primaryLight: "#C084FC",
      secondary: "#3B0764",
      secondaryLight: "#581C87",
      background: "#FAF5FF",
    },
  },
  {
    name: "Warm Orange",
    theme: {
      primary: "#F97316",
      primaryLight: "#FB923C",
      secondary: "#431407",
      secondaryLight: "#7C2D12",
      background: "#FFF7ED",
    },
  },
  {
    name: "Rose Pink",
    theme: {
      primary: "#F43F5E",
      primaryLight: "#FB7185",
      secondary: "#4C0519",
      secondaryLight: "#881337",
      background: "#FFF1F2",
    },
  },
];

export default function CreateTenantPage() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [selectedTheme, setSelectedTheme] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const handleNameChange = (value: string) => {
    setName(value);
    // Auto-generate slug from name
    setSlug(
      value
        .toLowerCase()
        .replace(/[^a-z0-9가-힣]/g, "-")
        .replace(/-+/g, "-")
        .replace(/^-|-$/g, "")
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !slug.trim()) return;

    setSubmitting(true);
    setError("");

    try {
      const config = {
        theme: PRESET_THEMES[selectedTheme].theme,
      };

      await createPlatformTenant({
        name: name.trim(),
        slug: slug.trim(),
        configJson: JSON.stringify(config),
      });

      router.push("/superadmin/tenants");
    } catch (err: unknown) {
      const msg =
        err instanceof Error ? err.message : "쇼핑몰 생성에 실패했습니다.";
      setError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">
        새 쇼핑몰 분양
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        {error && (
          <div className="p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
            {error}
          </div>
        )}

        <div className="bg-white rounded-xl shadow-sm p-5 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              쇼핑몰 이름
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => handleNameChange(e.target.value)}
              placeholder="예: 모현 스토어"
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Slug (URL 식별자)
            </label>
            <input
              type="text"
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              placeholder="예: mohyun"
              className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
              required
              pattern="[a-z0-9-]+"
            />
            <p className="text-xs text-gray-400 mt-1">
              영문 소문자, 숫자, 하이픈만 사용 가능
            </p>
          </div>
        </div>

        {/* Theme Selection */}
        <div className="bg-white rounded-xl shadow-sm p-5">
          <label className="block text-sm font-medium text-gray-700 mb-3">
            디자인 테마 선택
          </label>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            {PRESET_THEMES.map((preset, index) => (
              <button
                key={preset.name}
                type="button"
                onClick={() => setSelectedTheme(index)}
                className={`p-3 rounded-lg border-2 text-left transition-colors ${
                  selectedTheme === index
                    ? "border-emerald-500 bg-emerald-50"
                    : "border-gray-200 hover:border-gray-300"
                }`}
              >
                <div className="flex gap-1 mb-2">
                  <div
                    className="w-6 h-6 rounded-full"
                    style={{ backgroundColor: preset.theme.primary }}
                  />
                  <div
                    className="w-6 h-6 rounded-full"
                    style={{ backgroundColor: preset.theme.secondary }}
                  />
                  <div
                    className="w-6 h-6 rounded-full border"
                    style={{ backgroundColor: preset.theme.background }}
                  />
                </div>
                <p className="text-xs font-medium text-gray-700">
                  {preset.name}
                </p>
              </button>
            ))}
          </div>
        </div>

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={submitting}
            className="px-6 py-2.5 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700 disabled:opacity-50"
          >
            {submitting ? "생성 중..." : "쇼핑몰 생성"}
          </button>
          <button
            type="button"
            onClick={() => router.back()}
            className="px-6 py-2.5 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
          >
            취소
          </button>
        </div>
      </form>
    </div>
  );
}
