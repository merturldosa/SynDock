"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Plus, Trash2, ImagePlus } from "lucide-react";
import { getProductById, getCategories } from "@/lib/productApi";
import { updateProduct, getProductVariants, updateProductVariants, type ProductVariantDto } from "@/lib/adminApi";
import { generateProductImage } from "@/lib/forecastApi";
import { AIContentGenerator } from "@/components/admin/AIContentGenerator";
import toast from "react-hot-toast";
import type { ProductDetail, CategoryInfo } from "@/types/product";

export default function AdminProductEditPage() {
  const params = useParams();
  const router = useRouter();
  const t = useTranslations();
  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [variants, setVariants] = useState<ProductVariantDto[]>([]);
  const [generatingImage, setGeneratingImage] = useState(false);
  const [generatedImageUrl, setGeneratedImageUrl] = useState<string | null>(null);
  const [imagePrompt, setImagePrompt] = useState("");

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

    Promise.all([getProductById(id), getCategories(), getProductVariants(id)])
      .then(([p, cats, v]) => {
        setProduct(p);
        setCategories(cats);
        setVariants(v);
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
      .catch(() => { toast.error(t("common.fetchError")); })
      .finally(() => setLoading(false));
  }, [params.id]);

  const addVariant = () => {
    setVariants((prev) => [
      ...prev,
      { name: "", sku: "", price: null, stock: 0, sortOrder: prev.length, isActive: true },
    ]);
  };

  const removeVariant = (index: number) => {
    setVariants((prev) => prev.filter((_, i) => i !== index));
  };

  const updateVariant = (index: number, field: keyof ProductVariantDto, value: unknown) => {
    setVariants((prev) =>
      prev.map((v, i) => (i === index ? { ...v, [field]: value } : v))
    );
  };

  const handleGenerateImage = async () => {
    if (!product) return;
    setGeneratingImage(true);
    try {
      const result = await generateProductImage(
        product.id,
        imagePrompt || undefined
      );
      setGeneratedImageUrl(result.url);
    } catch {
      toast.error(t("admin.products.aiImageGenerateFailed"));
    }
    setGeneratingImage(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!product) return;
    setSubmitting(true);
    try {
      await Promise.all([
        updateProduct(product.id, form),
        updateProductVariants(product.id, variants.map((v, i) => ({ ...v, sortOrder: i }))),
      ]);
      toast.success(t("admin.products.updated"));
      router.push("/admin/products");
    } catch {
      toast.error(t("admin.products.updateFailed"));
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
        {t("admin.products.notFound")}
      </div>
    );
  }

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("admin.products.edit")}
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            {t("admin.products.basicInfo")}
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm text-gray-500 mb-1">{t("admin.products.name")}</label>
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
                {t("admin.products.category")}
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
                {t("admin.products.description")}
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
              <label className="block text-sm text-gray-500 mb-1">{t("admin.products.specification")}</label>
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
            {t("admin.products.priceInfo")}
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm text-gray-500 mb-1">
                {t("admin.products.priceType")}
              </label>
              <select
                value={form.priceType}
                onChange={(e) =>
                  setForm((f) => ({ ...f, priceType: e.target.value }))
                }
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
                <option value="Fixed">{t("admin.products.priceTypeFixed")}</option>
                <option value="Inquiry">{t("admin.products.priceTypeInquiry")}</option>
              </select>
            </div>
            {form.priceType === "Fixed" && (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm text-gray-500 mb-1">
                    {t("admin.products.priceWon")}
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
                    {t("admin.products.salePriceWon")}
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
                    placeholder={t("admin.products.salePricePlaceholder")}
                  />
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            {t("admin.products.options")}
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
              {t("admin.products.active")}
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isFeatured}
                onChange={(e) =>
                  setForm((f) => ({ ...f, isFeatured: e.target.checked }))
                }
              />
              {t("admin.products.featured")}
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isNew}
                onChange={(e) =>
                  setForm((f) => ({ ...f, isNew: e.target.checked }))
                }
              />
              {t("admin.products.isNew")}
            </label>
          </div>
        </div>

        {/* Variant Management */}
        <div className="bg-white rounded-xl shadow-sm p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-semibold text-[var(--color-secondary)]">{t("admin.products.variantManagement")}</h2>
            <button
              type="button"
              onClick={addVariant}
              className="flex items-center gap-1 px-3 py-1.5 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90"
            >
              <Plus size={14} />
              {t("admin.products.addVariant")}
            </button>
          </div>
          {variants.length === 0 ? (
            <p className="text-sm text-gray-400 text-center py-6">{t("admin.products.noVariants")}</p>
          ) : (
            <div className="space-y-3">
              {variants.map((v, i) => (
                <div key={v.id ?? `new-${i}`} className="border rounded-lg p-4">
                  <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                    <div className="col-span-2 sm:col-span-1">
                      <label className="block text-xs text-gray-400 mb-1">{t("admin.products.variantName")}</label>
                      <input
                        type="text"
                        value={v.name}
                        onChange={(e) => updateVariant(i, "name", e.target.value)}
                        className="w-full px-2.5 py-2 border rounded-lg text-sm"
                        placeholder={t("admin.products.variantNamePlaceholder")}
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-gray-400 mb-1">{t("admin.products.variantSku")}</label>
                      <input
                        type="text"
                        value={v.sku ?? ""}
                        onChange={(e) => updateVariant(i, "sku", e.target.value || null)}
                        className="w-full px-2.5 py-2 border rounded-lg text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-gray-400 mb-1">{t("admin.products.variantPriceWon")}</label>
                      <input
                        type="number"
                        value={v.price ?? ""}
                        onChange={(e) => updateVariant(i, "price", e.target.value ? Number(e.target.value) : null)}
                        className="w-full px-2.5 py-2 border rounded-lg text-sm"
                        placeholder={t("admin.products.variantPricePlaceholder")}
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-gray-400 mb-1">{t("admin.products.variantStock")}</label>
                      <input
                        type="number"
                        value={v.stock}
                        onChange={(e) => updateVariant(i, "stock", Number(e.target.value))}
                        className="w-full px-2.5 py-2 border rounded-lg text-sm"
                      />
                    </div>
                  </div>
                  <div className="flex items-center justify-between mt-3">
                    <label className="flex items-center gap-2 text-sm text-gray-600">
                      <input
                        type="checkbox"
                        checked={v.isActive}
                        onChange={(e) => updateVariant(i, "isActive", e.target.checked)}
                      />
                      {t("admin.products.variantActive")}
                    </label>
                    <button
                      type="button"
                      onClick={() => removeVariant(i)}
                      className="flex items-center gap-1 text-sm text-red-500 hover:text-red-700"
                    >
                      <Trash2 size={14} />
                      {t("common.delete")}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* AI Image Generation */}
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            {t("admin.products.aiImageGeneration")}
          </h2>
          <div className="space-y-3">
            <input
              type="text"
              value={imagePrompt}
              onChange={(e) => setImagePrompt(e.target.value)}
              placeholder={t("admin.products.aiImagePromptPlaceholder")}
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
            />
            <button
              type="button"
              onClick={handleGenerateImage}
              disabled={generatingImage}
              className="flex items-center gap-2 px-4 py-2.5 bg-[var(--color-secondary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
            >
              <ImagePlus size={16} />
              {generatingImage ? t("admin.products.aiImageGenerating") : t("admin.products.aiImageGenerate")}
            </button>
            {generatedImageUrl && (
              <div className="mt-3">
                <img
                  src={generatedImageUrl}
                  alt="AI generated"
                  className="w-64 h-64 object-cover rounded-lg border"
                />
                <p className="text-xs text-gray-400 mt-2">
                  {t("admin.products.aiImageSaveHint")}
                </p>
              </div>
            )}
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-3">{t("admin.products.detailContent")}</h2>
          <Link
            href={`/admin/products/${product.id}/sections`}
            className="inline-flex items-center gap-2 px-4 py-2.5 bg-[var(--color-secondary)] text-white rounded-lg text-sm font-medium hover:opacity-90"
          >
            {t("admin.sections.title")}
          </Link>
          <p className="text-xs text-gray-400 mt-2">{t("admin.products.detailContentDesc")}</p>
        </div>

        <div className="flex gap-3">
          <button
            type="button"
            onClick={() => router.back()}
            className="flex-1 py-3 border rounded-lg text-sm font-medium text-gray-500 hover:bg-gray-50"
          >
            {t("common.cancel")}
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
          >
            {submitting ? t("admin.products.updating") : t("admin.products.edit")}
          </button>
        </div>
      </form>
    </div>
  );
}
