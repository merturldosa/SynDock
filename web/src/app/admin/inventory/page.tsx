"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { Warehouse, AlertTriangle, Save, Server } from "lucide-react";
import { useTranslations } from "next-intl";
import { getLowStock, updateStock, type LowStockItem } from "@/lib/adminApi";
import { formatNumber } from "@/lib/format";
import MesInventoryComparison from "./_components/MesInventoryComparison";

type Tab = "lowStock" | "mesComparison";

export default function InventoryPage() {
  const t = useTranslations();
  const [activeTab, setActiveTab] = useState<Tab>("lowStock");
  const [items, setItems] = useState<LowStockItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [threshold, setThreshold] = useState(10);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editStock, setEditStock] = useState(0);
  const [saving, setSaving] = useState(false);

  const load = () => {
    setLoading(true);
    getLowStock(threshold)
      .then(setItems)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    if (activeTab === "lowStock") {
      load();
    }
  }, [threshold, activeTab]);

  const handleEdit = (item: LowStockItem) => {
    setEditingId(item.variantId);
    setEditStock(item.stock);
  };

  const handleSave = async (variantId: number) => {
    setSaving(true);
    try {
      await updateStock(variantId, editStock);
      setItems((prev) =>
        prev.map((i) =>
          i.variantId === variantId ? { ...i, stock: editStock } : i
        )
      );
      setEditingId(null);
    } catch {
      alert(t("admin.inventory.saveFailed"));
    }
    setSaving(false);
  };

  const tabs: { key: Tab; label: string; icon: React.ReactNode }[] = [
    {
      key: "lowStock",
      label: t("admin.inventory.tabs.lowStock"),
      icon: <AlertTriangle size={16} />,
    },
    {
      key: "mesComparison",
      label: t("admin.inventory.tabs.mesComparison"),
      icon: <Server size={16} />,
    },
  ];

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] flex items-center gap-2">
          <Warehouse size={24} /> {t("admin.inventory.title")}
        </h1>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition -mb-px ${
              activeTab === tab.key
                ? "border-[var(--color-primary)] text-[var(--color-primary)]"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
            }`}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {activeTab === "lowStock" ? (
        <div>
          <div className="flex items-center justify-end mb-4">
            <div className="flex items-center gap-2">
              <label className="text-sm text-gray-500">
                {t("admin.inventory.threshold")}:
              </label>
              <select
                value={threshold}
                onChange={(e) => setThreshold(Number(e.target.value))}
                className="border rounded-lg px-3 py-2 text-sm"
              >
                <option value={5}>
                  {t("admin.inventory.thresholdBelow5")}
                </option>
                <option value={10}>
                  {t("admin.inventory.thresholdBelow10")}
                </option>
                <option value={20}>
                  {t("admin.inventory.thresholdBelow20")}
                </option>
                <option value={50}>
                  {t("admin.inventory.thresholdBelow50")}
                </option>
                <option value={100}>
                  {t("admin.inventory.thresholdAll")}
                </option>
              </select>
            </div>
          </div>

          {items.length > 0 && (
            <div className="flex items-center gap-2 mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
              <AlertTriangle size={18} className="text-red-500" />
              <p className="text-sm text-red-700">
                {t("admin.inventory.lowStockAlert", {
                  threshold,
                  count: items.length,
                })}
              </p>
            </div>
          )}

          {loading ? (
            <div className="flex items-center justify-center py-20">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
            </div>
          ) : items.length === 0 ? (
            <div className="text-center py-20 text-gray-400">
              <Warehouse size={48} className="mx-auto mb-3 opacity-30" />
              <p>{t("admin.inventory.noLowStockItems")}</p>
            </div>
          ) : (
            <div className="bg-white rounded-xl shadow-sm overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b">
                  <tr>
                    <th className="text-left p-3 font-medium text-gray-500">
                      {t("admin.inventory.product")}
                    </th>
                    <th className="text-left p-3 font-medium text-gray-500">
                      {t("admin.inventory.variant")}
                    </th>
                    <th className="text-left p-3 font-medium text-gray-500">
                      {t("admin.inventory.sku")}
                    </th>
                    <th className="text-center p-3 font-medium text-gray-500">
                      {t("admin.inventory.currentStock")}
                    </th>
                    <th className="text-center p-3 font-medium text-gray-500">
                      {t("admin.inventory.edit")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr
                      key={item.variantId}
                      className={`border-b last:border-0 hover:bg-gray-50 ${
                        item.stock === 0
                          ? "bg-red-50"
                          : item.stock <= 5
                          ? "bg-yellow-50"
                          : ""
                      }`}
                    >
                      <td className="p-3">
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0">
                            {item.imageUrl ? (
                              <Image
                                src={item.imageUrl}
                                alt={item.productName}
                                width={40}
                                height={40}
                                className="object-cover w-full h-full"
                              />
                            ) : (
                              <div className="w-full h-full flex items-center justify-center text-sm opacity-20">
                                📦
                              </div>
                            )}
                          </div>
                          <span className="font-medium line-clamp-1">
                            {item.productName}
                          </span>
                        </div>
                      </td>
                      <td className="p-3 text-gray-600">
                        {item.variantName}
                      </td>
                      <td className="p-3 text-gray-400 font-mono text-xs">
                        {item.sku || "-"}
                      </td>
                      <td className="p-3 text-center">
                        {editingId === item.variantId ? (
                          <input
                            type="number"
                            value={editStock}
                            onChange={(e) =>
                              setEditStock(Number(e.target.value))
                            }
                            min={0}
                            className="w-20 px-2 py-1 border rounded text-center text-sm"
                            autoFocus
                          />
                        ) : (
                          <span
                            className={`font-bold ${
                              item.stock === 0
                                ? "text-red-600"
                                : item.stock <= 5
                                ? "text-orange-600"
                                : "text-gray-700"
                            }`}
                          >
                            {formatNumber(item.stock)}
                          </span>
                        )}
                      </td>
                      <td className="p-3 text-center">
                        {editingId === item.variantId ? (
                          <div className="flex items-center justify-center gap-1">
                            <button
                              onClick={() => handleSave(item.variantId)}
                              disabled={saving}
                              className="px-3 py-1 bg-[var(--color-primary)] text-white rounded text-xs hover:opacity-90 disabled:opacity-60"
                            >
                              <Save size={14} />
                            </button>
                            <button
                              onClick={() => setEditingId(null)}
                              className="px-3 py-1 border rounded text-xs hover:bg-gray-50"
                            >
                              {t("common.cancel")}
                            </button>
                          </div>
                        ) : (
                          <button
                            onClick={() => handleEdit(item)}
                            className="px-3 py-1 border rounded text-xs hover:bg-gray-50"
                          >
                            {t("admin.inventory.edit")}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      ) : (
        <MesInventoryComparison />
      )}
    </div>
  );
}
