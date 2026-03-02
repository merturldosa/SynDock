"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import Image from "next/image";
import { ChevronUp, ChevronDown, Trash2, Plus, Save, X } from "lucide-react";
import {
  getProductDetailSections,
  createProductDetailSection,
  updateProductDetailSection,
  deleteProductDetailSection,
  reorderProductDetailSections,
  type ProductDetailSection,
} from "@/lib/productDetailSectionApi";
import api from "@/lib/api";

const SECTION_TYPE_KEYS = [
  { value: "Hero", key: "admin.sections.typeHero" },
  { value: "Feature", key: "admin.sections.typeFeature" },
  { value: "Closing", key: "admin.sections.typeClosing" },
  { value: "Custom", key: "admin.sections.typeCustom" },
];

interface Props {
  productId: number;
}

export function ProductDetailSectionEditor({ productId }: Props) {
  const t = useTranslations();
  const [sections, setSections] = useState<ProductDetailSection[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState({
    title: "",
    content: "",
    imageUrl: "",
    imageAltText: "",
    sectionType: "Custom",
  });
  const [uploading, setUploading] = useState(false);

  const load = async () => {
    try {
      const data = await getProductDetailSections(productId);
      setSections(data);
    } catch {}
    setLoading(false);
  };

  useEffect(() => { load(); }, [productId]);

  const resetForm = () => {
    setForm({ title: "", content: "", imageUrl: "", imageAltText: "", sectionType: "Custom" });
    setShowForm(false);
    setEditingId(null);
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append("file", file);
      const { data } = await api.post("/upload/image?folder=sections", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      setForm((f) => ({ ...f, imageUrl: data.url }));
    } catch {
      alert(t("admin.sections.uploadFailed"));
    }
    setUploading(false);
  };

  const handleSubmit = async () => {
    if (!form.title.trim()) return alert(t("admin.sections.titleRequired"));
    try {
      if (editingId) {
        await updateProductDetailSection(productId, editingId, {
          title: form.title,
          content: form.content || undefined,
          imageUrl: form.imageUrl || undefined,
          imageAltText: form.imageAltText || undefined,
          sectionType: form.sectionType,
        });
      } else {
        await createProductDetailSection(productId, {
          title: form.title,
          content: form.content || undefined,
          imageUrl: form.imageUrl || undefined,
          imageAltText: form.imageAltText || undefined,
          sectionType: form.sectionType,
          sortOrder: sections.length,
        });
      }
      resetForm();
      await load();
    } catch {
      alert(t("admin.sections.saveFailed"));
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm(t("admin.sections.deleteConfirm"))) return;
    try {
      await deleteProductDetailSection(productId, id);
      await load();
    } catch {
      alert(t("admin.sections.deleteFailed"));
    }
  };

  const handleMove = async (index: number, direction: -1 | 1) => {
    const newIndex = index + direction;
    if (newIndex < 0 || newIndex >= sections.length) return;
    const ids = sections.map((s) => s.id);
    [ids[index], ids[newIndex]] = [ids[newIndex], ids[index]];
    try {
      await reorderProductDetailSections(productId, ids);
      await load();
    } catch {
      alert(t("admin.sections.reorderFailed"));
    }
  };

  const startEdit = (section: ProductDetailSection) => {
    setForm({
      title: section.title,
      content: section.content || "",
      imageUrl: section.imageUrl || "",
      imageAltText: section.imageAltText || "",
      sectionType: section.sectionType,
    });
    setEditingId(section.id);
    setShowForm(true);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-10">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div>
      {/* Section List */}
      {sections.length === 0 && !showForm && (
        <p className="text-center text-gray-400 py-8">{t("admin.sections.noSections")}</p>
      )}

      <div className="space-y-3 mb-6">
        {sections.map((section, i) => (
          <div key={section.id} className="bg-white rounded-xl shadow-sm p-4 border border-gray-100">
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-3 flex-1 min-w-0">
                {section.imageUrl && (
                  <div className="w-16 h-16 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0">
                    <Image src={section.imageUrl} alt={section.imageAltText || ""} width={64} height={64} className="object-cover w-full h-full" unoptimized />
                  </div>
                )}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                      section.sectionType === "Hero" ? "bg-purple-100 text-purple-700" :
                      section.sectionType === "Feature" ? "bg-blue-100 text-blue-700" :
                      section.sectionType === "Closing" ? "bg-orange-100 text-orange-700" :
                      "bg-gray-100 text-gray-700"
                    }`}>
                      {(() => { const found = SECTION_TYPE_KEYS.find((s) => s.value === section.sectionType); return found ? t(found.key) : section.sectionType; })()}
                    </span>
                    {!section.isActive && <span className="text-xs text-red-500">{t("admin.sections.inactive")}</span>}
                  </div>
                  <p className="font-medium text-sm text-[var(--color-secondary)] line-clamp-1">{section.title}</p>
                  {section.content && <p className="text-xs text-gray-400 line-clamp-1 mt-0.5">{section.content}</p>}
                </div>
              </div>

              <div className="flex items-center gap-1 flex-shrink-0">
                <button onClick={() => handleMove(i, -1)} disabled={i === 0} className="p-1.5 text-gray-400 hover:text-gray-600 disabled:opacity-30">
                  <ChevronUp size={16} />
                </button>
                <button onClick={() => handleMove(i, 1)} disabled={i === sections.length - 1} className="p-1.5 text-gray-400 hover:text-gray-600 disabled:opacity-30">
                  <ChevronDown size={16} />
                </button>
                <button onClick={() => startEdit(section)} className="p-1.5 text-gray-400 hover:text-blue-500">
                  <Save size={16} />
                </button>
                <button onClick={() => handleDelete(section.id)} className="p-1.5 text-gray-400 hover:text-red-500">
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Add/Edit Form */}
      {showForm ? (
        <div className="bg-gray-50 rounded-xl p-5 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-semibold text-[var(--color-secondary)]">
              {editingId ? t("admin.sections.editSection") : t("admin.sections.newSection")}
            </h3>
            <button onClick={resetForm} className="text-gray-400 hover:text-gray-600">
              <X size={18} />
            </button>
          </div>

          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.sections.sectionTitle")} *</label>
            <input
              type="text"
              value={form.title}
              onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
              placeholder={t("admin.sections.sectionTitlePlaceholder")}
            />
          </div>

          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.sections.content")}</label>
            <textarea
              value={form.content}
              onChange={(e) => setForm((f) => ({ ...f, content: e.target.value }))}
              rows={4}
              className="w-full px-3 py-2.5 border rounded-lg text-sm resize-none"
              placeholder={t("admin.sections.contentPlaceholder")}
            />
          </div>

          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.sections.image")}</label>
            <div className="flex items-center gap-3">
              <input
                type="file"
                accept="image/*"
                onChange={handleImageUpload}
                className="text-sm"
                disabled={uploading}
              />
              {uploading && <span className="text-xs text-gray-400">{t("review.uploading")}</span>}
            </div>
            {form.imageUrl && (
              <div className="mt-2 flex items-center gap-3">
                <div className="w-20 h-20 rounded-lg overflow-hidden bg-gray-100">
                  <Image src={form.imageUrl} alt="" width={80} height={80} className="object-cover w-full h-full" unoptimized />
                </div>
                <input
                  type="text"
                  value={form.imageAltText}
                  onChange={(e) => setForm((f) => ({ ...f, imageAltText: e.target.value }))}
                  className="flex-1 px-3 py-2 border rounded-lg text-sm"
                  placeholder={t("admin.sections.imageAlt")}
                />
              </div>
            )}
          </div>

          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.sections.sectionType")}</label>
            <select
              value={form.sectionType}
              onChange={(e) => setForm((f) => ({ ...f, sectionType: e.target.value }))}
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
            >
              {SECTION_TYPE_KEYS.map((s) => (
                <option key={s.value} value={s.value}>{t(s.key)}</option>
              ))}
            </select>
          </div>

          <div className="flex justify-end gap-2">
            <button onClick={resetForm} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">{t("common.cancel")}</button>
            <button onClick={handleSubmit} className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90">
              {editingId ? t("common.edit") : t("common.add")}
            </button>
          </div>
        </div>
      ) : (
        <button
          onClick={() => setShowForm(true)}
          className="w-full py-3 border-2 border-dashed border-gray-300 rounded-xl text-sm text-gray-500 hover:border-[var(--color-primary)] hover:text-[var(--color-primary)] transition-colors flex items-center justify-center gap-2"
        >
          <Plus size={16} /> {t("admin.sections.addSection")}
        </button>
      )}
    </div>
  );
}
