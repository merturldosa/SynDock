"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { X, Plus, FolderPlus } from "lucide-react";
import {
  getMyCollections,
  createCollection,
  addToCollection,
  type CollectionSummary,
} from "@/lib/collectionApi";

interface AddToCollectionModalProps {
  productId: number;
  isOpen: boolean;
  onClose: () => void;
}

export function AddToCollectionModal({
  productId,
  isOpen,
  onClose,
}: AddToCollectionModalProps) {
  const t = useTranslations();
  const [collections, setCollections] = useState<CollectionSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [adding, setAdding] = useState<number | null>(null);

  useEffect(() => {
    if (isOpen) {
      setLoading(true);
      getMyCollections()
        .then(setCollections)
        .catch(() => {})
        .finally(() => setLoading(false));
    }
  }, [isOpen]);

  const handleCreate = async () => {
    if (!newName.trim()) return;
    setCreating(true);
    try {
      const { collectionId } = await createCollection(newName.trim());
      await addToCollection(collectionId, productId);
      alert(t("collection.addedToNew"));
      onClose();
    } catch {
      alert(t("collection.createFailed"));
    }
    setCreating(false);
  };

  const handleAdd = async (collectionId: number) => {
    setAdding(collectionId);
    try {
      await addToCollection(collectionId, productId);
      alert(t("collection.added"));
      onClose();
    } catch {
      alert(t("collection.addFailed"));
    }
    setAdding(null);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-2xl w-full max-w-md mx-4 shadow-2xl">
        <div className="flex items-center justify-between p-4 border-b">
          <h3 className="font-bold text-[var(--color-secondary)]">
            {t("collection.addToCollection")}
          </h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <X size={20} />
          </button>
        </div>

        <div className="p-4 max-h-[50vh] overflow-y-auto">
          {loading ? (
            <div className="flex justify-center py-8">
              <div className="h-6 w-6 animate-spin rounded-full border-2 border-[var(--color-primary)] border-t-transparent" />
            </div>
          ) : (
            <div className="space-y-2">
              {collections.map((col) => (
                <button
                  key={col.id}
                  onClick={() => handleAdd(col.id)}
                  disabled={adding === col.id}
                  className="w-full flex items-center justify-between p-3 rounded-lg border hover:bg-gray-50 transition-colors disabled:opacity-50"
                >
                  <div className="text-left">
                    <p className="text-sm font-medium text-gray-800">{col.name}</p>
                    <p className="text-xs text-gray-400">{t("mypage.collections.itemCount", { count: col.itemCount })}</p>
                  </div>
                  <Plus size={16} className="text-gray-400" />
                </button>
              ))}
              {collections.length === 0 && (
                <p className="text-center text-sm text-gray-400 py-4">
                  {t("collection.noCollections")}
                </p>
              )}
            </div>
          )}
        </div>

        <div className="p-4 border-t">
          <div className="flex gap-2">
            <input
              type="text"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder={t("collection.newCollectionName")}
              className="flex-1 px-3 py-2 border rounded-lg text-sm"
              onKeyDown={(e) => e.key === "Enter" && handleCreate()}
            />
            <button
              onClick={handleCreate}
              disabled={creating || !newName.trim()}
              className="flex items-center gap-1 px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50"
            >
              <FolderPlus size={14} />
              {creating ? t("collection.creating") : t("collection.create")}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
