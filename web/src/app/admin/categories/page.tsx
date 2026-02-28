"use client";

import { useEffect, useState } from "react";
import { FolderTree, Plus, Edit2, Trash2, Save, X } from "lucide-react";
import { getCategories } from "@/lib/productApi";
import {
  createCategory,
  updateCategory,
  deleteCategory,
} from "@/lib/adminApi";
import type { CategoryInfo } from "@/types/product";

export default function AdminCategoriesPage() {
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editName, setEditName] = useState("");
  const [editSlug, setEditSlug] = useState("");
  const [showAdd, setShowAdd] = useState(false);
  const [newName, setNewName] = useState("");
  const [newSlug, setNewSlug] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    setLoading(true);
    getCategories()
      .then(setCategories)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, []);

  const handleAdd = async () => {
    if (!newName.trim()) return;
    setSubmitting(true);
    try {
      const slug =
        newSlug.trim() ||
        newName
          .toLowerCase()
          .replace(/[^a-z0-9가-힣]/g, "-")
          .replace(/-+/g, "-");
      await createCategory({ name: newName, slug });
      setNewName("");
      setNewSlug("");
      setShowAdd(false);
      load();
    } catch {
      alert("카테고리 추가에 실패했습니다.");
    }
    setSubmitting(false);
  };

  const handleEdit = (cat: CategoryInfo) => {
    setEditingId(cat.id);
    setEditName(cat.name);
    setEditSlug(cat.slug || "");
  };

  const handleSave = async () => {
    if (editingId === null || !editName.trim()) return;
    setSubmitting(true);
    try {
      await updateCategory(editingId, { name: editName, slug: editSlug });
      setEditingId(null);
      load();
    } catch {
      alert("카테고리 수정에 실패했습니다.");
    }
    setSubmitting(false);
  };

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`"${name}" 카테고리를 삭제하시겠습니까?`)) return;
    try {
      await deleteCategory(id);
      load();
    } catch {
      alert("삭제에 실패했습니다. 하위 카테고리나 상품이 있을 수 있습니다.");
    }
  };

  return (
    <div className="max-w-2xl">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          카테고리 관리
        </h1>
        <button
          onClick={() => setShowAdd(!showAdd)}
          className="flex items-center gap-2 px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90"
        >
          <Plus size={16} /> 카테고리 추가
        </button>
      </div>

      {/* Add Form */}
      {showAdd && (
        <div className="bg-white rounded-xl shadow-sm p-4 mb-4">
          <div className="flex gap-2">
            <input
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="카테고리 이름"
              className="flex-1 px-3 py-2.5 border rounded-lg text-sm"
            />
            <input
              type="text"
              value={newSlug}
              onChange={(e) => setNewSlug(e.target.value)}
              placeholder="slug (자동 생성)"
              className="w-40 px-3 py-2.5 border rounded-lg text-sm"
            />
            <button
              onClick={handleAdd}
              disabled={submitting || !newName.trim()}
              className="px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm disabled:opacity-60"
            >
              {submitting ? "추가 중..." : "추가"}
            </button>
            <button
              onClick={() => setShowAdd(false)}
              className="px-3 py-2.5 text-gray-400 hover:text-gray-600"
            >
              <X size={18} />
            </button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : categories.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <FolderTree size={48} className="mx-auto mb-4 opacity-50" />
          <p>카테고리가 없습니다.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          {categories.map((cat, idx) => (
            <div
              key={cat.id}
              className={`flex items-center justify-between p-4 ${
                idx < categories.length - 1 ? "border-b" : ""
              } hover:bg-gray-50`}
            >
              {editingId === cat.id ? (
                <div className="flex items-center gap-2 flex-1">
                  <input
                    type="text"
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    className="flex-1 px-3 py-1.5 border rounded-lg text-sm"
                  />
                  <input
                    type="text"
                    value={editSlug}
                    onChange={(e) => setEditSlug(e.target.value)}
                    placeholder="slug"
                    className="w-32 px-3 py-1.5 border rounded-lg text-sm"
                  />
                  <button
                    onClick={handleSave}
                    disabled={submitting}
                    className="p-1.5 text-[var(--color-primary)] hover:bg-[var(--color-primary)]/10 rounded"
                  >
                    <Save size={16} />
                  </button>
                  <button
                    onClick={() => setEditingId(null)}
                    className="p-1.5 text-gray-400 hover:text-gray-600"
                  >
                    <X size={16} />
                  </button>
                </div>
              ) : (
                <>
                  <div className="flex items-center gap-3">
                    <FolderTree
                      size={18}
                      className="text-[var(--color-primary)]"
                    />
                    <div>
                      <p className="font-medium text-[var(--color-secondary)]">
                        {cat.name}
                      </p>
                      <p className="text-xs text-gray-400">
                        {cat.slug} · {cat.productCount}개 상품
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => handleEdit(cat)}
                      className="p-1.5 text-gray-400 hover:text-[var(--color-primary)]"
                    >
                      <Edit2 size={16} />
                    </button>
                    <button
                      onClick={() => handleDelete(cat.id, cat.name)}
                      className="p-1.5 text-gray-400 hover:text-red-500"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                </>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
