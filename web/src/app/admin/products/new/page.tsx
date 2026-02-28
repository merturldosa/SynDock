"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Upload, X } from "lucide-react";
import { getCategories } from "@/lib/productApi";
import { createProduct } from "@/lib/adminApi";
import { uploadImage } from "@/lib/postApi";
import type { CategoryInfo } from "@/types/product";

export default function AdminProductNewPage() {
  const router = useRouter();
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [imageUrls, setImageUrls] = useState<string[]>([]);
  const [uploading, setUploading] = useState(false);

  const [form, setForm] = useState({
    name: "",
    slug: "",
    description: "",
    specification: "",
    categoryId: 0,
    price: 0,
    salePrice: undefined as number | undefined,
    priceType: "Fixed",
    isActive: true,
    isFeatured: false,
    isNew: true,
  });

  useEffect(() => {
    getCategories().then(setCategories).catch(() => {});
  }, []);

  const handleSlugGen = () => {
    setForm((f) => ({
      ...f,
      slug: f.name
        .toLowerCase()
        .replace(/[^a-z0-9가-힣]/g, "-")
        .replace(/-+/g, "-")
        .replace(/^-|-$/g, ""),
    }));
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files) return;
    setUploading(true);
    try {
      for (const file of Array.from(files)) {
        const { url } = await uploadImage(file, "products");
        setImageUrls((prev) => [...prev, url]);
      }
    } catch {
      alert("이미지 업로드에 실패했습니다.");
    }
    setUploading(false);
    e.target.value = "";
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name || !form.categoryId) {
      alert("상품명과 카테고리를 입력해 주세요.");
      return;
    }
    setSubmitting(true);
    try {
      const { productId } = await createProduct({
        ...form,
        imageUrls,
      });
      alert("상품이 등록되었습니다.");
      router.push("/admin/products");
    } catch {
      alert("상품 등록에 실패했습니다.");
    }
    setSubmitting(false);
  };

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        상품 등록
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            기본 정보
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm text-gray-500 mb-1">
                상품명 *
              </label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                onBlur={handleSlugGen}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                required
              />
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">Slug</label>
              <input
                type="text"
                value={form.slug}
                onChange={(e) => setForm((f) => ({ ...f, slug: e.target.value }))}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              />
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">
                카테고리 *
              </label>
              <select
                value={form.categoryId}
                onChange={(e) =>
                  setForm((f) => ({ ...f, categoryId: Number(e.target.value) }))
                }
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                required
              >
                <option value={0}>선택하세요</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">
                상품 설명
              </label>
              <textarea
                value={form.description}
                onChange={(e) =>
                  setForm((f) => ({ ...f, description: e.target.value }))
                }
                rows={4}
                className="w-full px-3 py-2.5 border rounded-lg text-sm resize-none"
              />
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">규격</label>
              <input
                type="text"
                value={form.specification}
                onChange={(e) =>
                  setForm((f) => ({ ...f, specification: e.target.value }))
                }
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              />
            </div>
          </div>
        </div>

        {/* Price */}
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            가격 정보
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm text-gray-500 mb-1">
                가격 유형
              </label>
              <select
                value={form.priceType}
                onChange={(e) =>
                  setForm((f) => ({ ...f, priceType: e.target.value }))
                }
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
                <option value="Fixed">정가</option>
                <option value="Inquiry">상담요망</option>
              </select>
            </div>
            {form.priceType === "Fixed" && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm text-gray-500 mb-1">
                    판매가 (원)
                  </label>
                  <input
                    type="number"
                    value={form.price}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, price: Number(e.target.value) }))
                    }
                    className="w-full px-3 py-2.5 border rounded-lg text-sm"
                  />
                </div>
                <div>
                  <label className="block text-sm text-gray-500 mb-1">
                    할인가 (원)
                  </label>
                  <input
                    type="number"
                    value={form.salePrice ?? ""}
                    onChange={(e) =>
                      setForm((f) => ({
                        ...f,
                        salePrice: e.target.value
                          ? Number(e.target.value)
                          : undefined,
                      }))
                    }
                    className="w-full px-3 py-2.5 border rounded-lg text-sm"
                    placeholder="없으면 비워두세요"
                  />
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Images */}
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            이미지
          </h2>
          <div className="flex flex-wrap gap-3 mb-3">
            {imageUrls.map((url, i) => (
              <div
                key={i}
                className="relative w-24 h-24 rounded-lg overflow-hidden border"
              >
                <img
                  src={url}
                  alt=""
                  className="w-full h-full object-cover"
                />
                <button
                  type="button"
                  onClick={() =>
                    setImageUrls((prev) => prev.filter((_, idx) => idx !== i))
                  }
                  className="absolute top-1 right-1 p-0.5 bg-red-500 text-white rounded-full"
                >
                  <X size={12} />
                </button>
                {i === 0 && (
                  <span className="absolute bottom-0 left-0 right-0 bg-[var(--color-primary)] text-white text-[10px] text-center py-0.5">
                    대표
                  </span>
                )}
              </div>
            ))}
            <label className="w-24 h-24 border-2 border-dashed rounded-lg flex flex-col items-center justify-center cursor-pointer text-gray-400 hover:border-[var(--color-primary)] hover:text-[var(--color-primary)] transition-colors">
              <Upload size={20} />
              <span className="text-[10px] mt-1">
                {uploading ? "업로드 중..." : "이미지 추가"}
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
        </div>

        {/* Options */}
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            옵션
          </h2>
          <div className="flex flex-wrap gap-4">
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) =>
                  setForm((f) => ({ ...f, isActive: e.target.checked }))
                }
              />
              판매 중
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isFeatured}
                onChange={(e) =>
                  setForm((f) => ({ ...f, isFeatured: e.target.checked }))
                }
              />
              추천 상품
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isNew}
                onChange={(e) =>
                  setForm((f) => ({ ...f, isNew: e.target.checked }))
                }
              />
              신상품
            </label>
          </div>
        </div>

        {/* Submit */}
        <div className="flex gap-3">
          <button
            type="button"
            onClick={() => router.back()}
            className="flex-1 py-3 border rounded-lg text-sm font-medium text-gray-500 hover:bg-gray-50"
          >
            취소
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
          >
            {submitting ? "등록 중..." : "상품 등록"}
          </button>
        </div>
      </form>
    </div>
  );
}
