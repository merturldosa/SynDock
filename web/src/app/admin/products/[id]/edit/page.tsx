"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { getProductById, getCategories } from "@/lib/productApi";
import { updateProduct } from "@/lib/adminApi";
import { AIContentGenerator } from "@/components/admin/AIContentGenerator";
import type { ProductDetail, CategoryInfo } from "@/types/product";

export default function AdminProductEditPage() {
  const params = useParams();
  const router = useRouter();
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

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
    isNew: false,
  });

  useEffect(() => {
    const id = Number(params.id);
    if (!id) return;

    Promise.all([getProductById(id), getCategories()])
      .then(([p, cats]) => {
        setProduct(p);
        setCategories(cats);
        setForm({
          name: p.name,
          slug: p.slug || "",
          description: p.description || "",
          specification: p.specification || "",
          categoryId: p.categoryId,
          price: p.price,
          salePrice: p.salePrice ?? undefined,
          priceType: p.priceType || "Fixed",
          isActive: true,
          isFeatured: false,
          isNew: false,
        });
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [params.id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!product) return;
    setSubmitting(true);
    try {
      await updateProduct(product.id, form);
      alert("상품이 수정되었습니다.");
      router.push("/admin/products");
    } catch {
      alert("수정에 실패했습니다.");
    }
    setSubmitting(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (!product) {
    return (
      <div className="text-center py-20 text-gray-400">
        상품을 찾을 수 없습니다.
      </div>
    );
  }

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        상품 수정
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            기본 정보
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm text-gray-500 mb-1">상품명</label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
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
                카테고리
              </label>
              <select
                value={form.categoryId}
                onChange={(e) =>
                  setForm((f) => ({ ...f, categoryId: Number(e.target.value) }))
                }
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
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
              <div className="mt-2">
                <AIContentGenerator
                  productId={product?.id ?? null}
                  onApply={(desc) => setForm((f) => ({ ...f, description: desc }))}
                />
              </div>
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
            {submitting ? "수정 중..." : "상품 수정"}
          </button>
        </div>
      </form>
    </div>
  );
}
