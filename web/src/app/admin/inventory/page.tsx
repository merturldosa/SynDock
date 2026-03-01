"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { Warehouse, AlertTriangle, Save } from "lucide-react";
import { getLowStock, updateStock, type LowStockItem } from "@/lib/adminApi";

function formatNumber(n: number): string {
  return n.toLocaleString("ko-KR");
}

export default function InventoryPage() {
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
    load();
  }, [threshold]);

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
      alert("재고 수정에 실패했습니다.");
    }
    setSaving(false);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] flex items-center gap-2">
          <Warehouse size={24} /> 재고 관리
        </h1>
        <div className="flex items-center gap-2">
          <label className="text-sm text-gray-500">기준 수량:</label>
          <select
            value={threshold}
            onChange={(e) => setThreshold(Number(e.target.value))}
            className="border rounded-lg px-3 py-2 text-sm"
          >
            <option value={5}>5개 이하</option>
            <option value={10}>10개 이하</option>
            <option value={20}>20개 이하</option>
            <option value={50}>50개 이하</option>
            <option value={100}>전체 (100 이하)</option>
          </select>
        </div>
      </div>

      {items.length > 0 && (
        <div className="flex items-center gap-2 mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
          <AlertTriangle size={18} className="text-red-500" />
          <p className="text-sm text-red-700">
            재고 {threshold}개 이하 상품: <strong>{items.length}건</strong>
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
          <p>저재고 상품이 없습니다.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3 font-medium text-gray-500">상품</th>
                <th className="text-left p-3 font-medium text-gray-500">옵션</th>
                <th className="text-left p-3 font-medium text-gray-500">SKU</th>
                <th className="text-center p-3 font-medium text-gray-500">현재 재고</th>
                <th className="text-center p-3 font-medium text-gray-500">수정</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr
                  key={item.variantId}
                  className={`border-b last:border-0 hover:bg-gray-50 ${
                    item.stock === 0 ? "bg-red-50" : item.stock <= 5 ? "bg-yellow-50" : ""
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
                            unoptimized
                          />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-sm opacity-20">
                            📦
                          </div>
                        )}
                      </div>
                      <span className="font-medium line-clamp-1">{item.productName}</span>
                    </div>
                  </td>
                  <td className="p-3 text-gray-600">{item.variantName}</td>
                  <td className="p-3 text-gray-400 font-mono text-xs">{item.sku || "-"}</td>
                  <td className="p-3 text-center">
                    {editingId === item.variantId ? (
                      <input
                        type="number"
                        value={editStock}
                        onChange={(e) => setEditStock(Number(e.target.value))}
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
                          취소
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => handleEdit(item)}
                        className="px-3 py-1 border rounded text-xs hover:bg-gray-50"
                      >
                        수정
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
  );
}
