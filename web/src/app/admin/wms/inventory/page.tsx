"use client";
import { useEffect, useState } from "react";
import { ArrowUpDown, Plus, Filter, Package, ArrowRight } from "lucide-react";
import api from "@/lib/api";

interface Movement {
  id: number;
  productId: number;
  product?: { name: string };
  movementType: string;
  quantity: number;
  previousStock: number;
  newStock: number;
  fromLocationId?: number;
  toLocationId?: number;
  fromLocation?: { code: string };
  toLocation?: { code: string };
  reason?: string;
  createdAt: string;
  createdBy?: string;
}

const typeColors: Record<string, string> = {
  Inbound: "bg-green-100 text-green-700",
  Outbound: "bg-red-100 text-red-700",
  Transfer: "bg-blue-100 text-blue-700",
  Adjustment: "bg-yellow-100 text-yellow-700",
  Return: "bg-purple-100 text-purple-700",
};

export default function WmsInventoryPage() {
  const [movements, setMovements] = useState<Movement[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [showFilter, setShowFilter] = useState(false);
  const [filter, setFilter] = useState({ productId: "", movementType: "", dateFrom: "", dateTo: "" });
  const [form, setForm] = useState({
    productId: "",
    movementType: "Inbound",
    quantity: 1,
    fromLocationId: "",
    toLocationId: "",
    reason: "",
  });

  const fetchMovements = () => {
    setLoading(true);
    setError("");
    const params = new URLSearchParams();
    if (filter.productId) params.set("productId", filter.productId);
    if (filter.movementType) params.set("movementType", filter.movementType);
    if (filter.dateFrom) params.set("dateFrom", filter.dateFrom);
    if (filter.dateTo) params.set("dateTo", filter.dateTo);
    const qs = params.toString();
    api.get(`/wms/movements${qs ? `?${qs}` : ""}`)
      .then(r => setMovements(r.data))
      .catch(() => setError("Failed to load movements"))
      .finally(() => setLoading(false));
  };

  useEffect(() => { fetchMovements(); }, []);

  const createMovement = async () => {
    try {
      await api.post("/wms/movements", {
        productId: parseInt(form.productId),
        movementType: form.movementType,
        quantity: form.quantity,
        fromLocationId: form.fromLocationId ? parseInt(form.fromLocationId) : null,
        toLocationId: form.toLocationId ? parseInt(form.toLocationId) : null,
        reason: form.reason || null,
      });
      setShowCreate(false);
      setForm({ productId: "", movementType: "Inbound", quantity: 1, fromLocationId: "", toLocationId: "", reason: "" });
      fetchMovements();
    } catch {
      setError("Failed to create movement");
    }
  };

  const applyFilter = () => {
    setShowFilter(false);
    fetchMovements();
  };

  const clearFilter = () => {
    setFilter({ productId: "", movementType: "", dateFrom: "", dateTo: "" });
    setShowFilter(false);
    setTimeout(fetchMovements, 0);
  };

  const hasActiveFilter = filter.productId || filter.movementType || filter.dateFrom || filter.dateTo;

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><ArrowUpDown size={24} /> Inventory Movements</h1>
          <p className="text-gray-500 text-sm mt-1">{movements.length} movements</p>
        </div>
        <div className="flex gap-2">
          <button onClick={() => setShowFilter(!showFilter)} className={`flex items-center gap-2 px-4 py-2 border rounded-lg text-sm hover:bg-gray-50 ${hasActiveFilter ? "border-blue-500 text-blue-600" : "text-gray-600"}`}>
            <Filter size={16} /> Filter {hasActiveFilter && <span className="w-2 h-2 bg-blue-600 rounded-full" />}
          </button>
          <button onClick={() => setShowCreate(true)} className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"><Plus size={16} /> Record Movement</button>
        </div>
      </div>

      {showFilter && (
        <div className="bg-white rounded-xl border p-4 mb-4">
          <div className="grid grid-cols-4 gap-3">
            <input type="number" value={filter.productId} onChange={e => setFilter({...filter, productId: e.target.value})} placeholder="Product ID" className="px-3 py-2 border rounded-lg text-sm" />
            <select value={filter.movementType} onChange={e => setFilter({...filter, movementType: e.target.value})} className="px-3 py-2 border rounded-lg text-sm">
              <option value="">All Types</option>
              {["Inbound","Outbound","Transfer","Adjustment","Return"].map(t => <option key={t} value={t}>{t}</option>)}
            </select>
            <input type="date" value={filter.dateFrom} onChange={e => setFilter({...filter, dateFrom: e.target.value})} className="px-3 py-2 border rounded-lg text-sm" />
            <input type="date" value={filter.dateTo} onChange={e => setFilter({...filter, dateTo: e.target.value})} className="px-3 py-2 border rounded-lg text-sm" />
          </div>
          <div className="flex gap-2 mt-3 justify-end">
            <button onClick={clearFilter} className="px-3 py-1.5 text-gray-500 text-sm hover:bg-gray-100 rounded-lg">Clear</button>
            <button onClick={applyFilter} className="px-3 py-1.5 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700">Apply</button>
          </div>
        </div>
      )}

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      <div className="bg-white rounded-xl border overflow-hidden">
        {loading ? <div className="p-12 text-center text-gray-400">Loading...</div> : movements.length === 0 ? (
          <div className="p-12 text-center text-gray-400">
            <Package className="w-12 h-12 mx-auto mb-3 text-gray-300" />
            <p>No movements found</p>
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 text-gray-500 text-xs uppercase">
                <th className="text-left px-4 py-3">Date</th>
                <th className="text-left px-4 py-3">Product</th>
                <th className="text-left px-4 py-3">Type</th>
                <th className="text-right px-4 py-3">Qty</th>
                <th className="text-center px-4 py-3">Stock Change</th>
                <th className="text-center px-4 py-3">Location</th>
                <th className="text-left px-4 py-3">Reason</th>
              </tr>
            </thead>
            <tbody>
              {movements.map(m => (
                <tr key={m.id} className="border-t hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-500 text-xs whitespace-nowrap">{new Date(m.createdAt).toLocaleString()}</td>
                  <td className="px-4 py-3 font-medium text-gray-900">{m.product?.name || `#${m.productId}`}</td>
                  <td className="px-4 py-3"><span className={`px-2 py-0.5 rounded text-xs font-medium ${typeColors[m.movementType] || "bg-gray-100 text-gray-600"}`}>{m.movementType}</span></td>
                  <td className="px-4 py-3 text-right font-mono">{m.movementType === "Outbound" ? `-${m.quantity}` : `+${m.quantity}`}</td>
                  <td className="px-4 py-3 text-center text-xs text-gray-500">{m.previousStock} <ArrowRight size={10} className="inline mx-1" /> {m.newStock}</td>
                  <td className="px-4 py-3 text-center text-xs font-mono text-gray-500">
                    {m.fromLocation?.code || "-"} <ArrowRight size={10} className="inline mx-1" /> {m.toLocation?.code || "-"}
                  </td>
                  <td className="px-4 py-3 text-gray-500 text-xs truncate max-w-[160px]">{m.reason || "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-4">Record Movement</h3>
            <div className="space-y-3">
              <div>
                <label className="block text-xs text-gray-500 mb-1">Product ID</label>
                <input type="number" value={form.productId} onChange={e => setForm({...form, productId: e.target.value})} placeholder="Product ID" className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Movement Type</label>
                <select value={form.movementType} onChange={e => setForm({...form, movementType: e.target.value})} className="w-full px-3 py-2 border rounded-lg text-sm">
                  {["Inbound","Outbound","Transfer","Adjustment","Return"].map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Quantity</label>
                <input type="number" min={1} value={form.quantity} onChange={e => setForm({...form, quantity: parseInt(e.target.value) || 0})} className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-500 mb-1">From Location ID</label>
                  <input type="number" value={form.fromLocationId} onChange={e => setForm({...form, fromLocationId: e.target.value})} placeholder="Optional" className="w-full px-3 py-2 border rounded-lg text-sm" />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">To Location ID</label>
                  <input type="number" value={form.toLocationId} onChange={e => setForm({...form, toLocationId: e.target.value})} placeholder="Optional" className="w-full px-3 py-2 border rounded-lg text-sm" />
                </div>
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Reason</label>
                <textarea value={form.reason} onChange={e => setForm({...form, reason: e.target.value})} placeholder="Reason for movement" className="w-full px-3 py-2 border rounded-lg text-sm" rows={2} />
              </div>
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createMovement} disabled={!form.productId || !form.quantity} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed">Record</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
