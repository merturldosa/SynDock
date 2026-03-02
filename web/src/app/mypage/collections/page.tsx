"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { FolderOpen, Plus, Trash2 } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  getMyCollections,
  createCollection,
  deleteCollection,
  type CollectionSummary,
} from "@/lib/collectionApi";

export default function MyCollectionsPage() {
  const t = useTranslations();
  const [collections, setCollections] = useState<CollectionSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [newName, setNewName] = useState("");
  const [creating, setCreating] = useState(false);

  const fetchData = () => {
    setLoading(true);
    getMyCollections()
      .then(setCollections)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(fetchData, []);

  const handleCreate = async () => {
    if (!newName.trim()) return;
    setCreating(true);
    try {
      await createCollection(newName.trim());
      setNewName("");
      fetchData();
    } catch {
      alert(t("collection.createFailed"));
    }
    setCreating(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm(t("mypage.collections.deleteConfirm"))) return;
    try {
      await deleteCollection(id);
      fetchData();
    } catch {
      alert(t("mypage.collections.deleteFailed"));
    }
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("mypage.collections.title")}
      </h1>

      {/* Create new */}
      <div className="flex gap-2 mb-6">
        <input
          type="text"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          placeholder={t("collection.newCollectionName")}
          className="flex-1 px-4 py-2.5 border rounded-lg text-sm"
          onKeyDown={(e) => e.key === "Enter" && handleCreate()}
        />
        <button
          onClick={handleCreate}
          disabled={creating || !newName.trim()}
          className="flex items-center gap-2 px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50"
        >
          <Plus size={16} />
          {creating ? t("common.saving") : t("collection.create")}
        </button>
      </div>

      {loading ? (
        <div className="flex justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : collections.length === 0 ? (
        <div className="text-center py-20">
          <FolderOpen size={48} className="mx-auto text-gray-300 mb-4" />
          <p className="text-gray-400">{t("mypage.collections.empty")}</p>
          <p className="text-sm text-gray-300 mt-1">
            {t("mypage.collections.createFirst")}
          </p>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {collections.map((col) => (
            <div
              key={col.id}
              className="bg-white rounded-xl shadow-sm border p-5 hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between">
                <Link
                  href={`/mypage/collections/${col.id}`}
                  className="flex-1"
                >
                  <h3 className="font-semibold text-[var(--color-secondary)] hover:text-[var(--color-primary)] transition-colors">
                    {col.name}
                  </h3>
                  {col.description && (
                    <p className="text-sm text-gray-400 mt-1 line-clamp-2">
                      {col.description}
                    </p>
                  )}
                  <div className="flex items-center gap-3 mt-3 text-xs text-gray-400">
                    <span>{t("mypage.collections.itemCount", { count: col.itemCount })}</span>
                    <span>{col.isPublic ? t("mypage.collections.public") : t("mypage.collections.private")}</span>
                  </div>
                </Link>
                <button
                  onClick={() => handleDelete(col.id)}
                  className="p-2 text-gray-300 hover:text-red-400 transition-colors"
                >
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
