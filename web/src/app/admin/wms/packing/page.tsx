"use client";
import { useEffect, useState } from "react";
import { PackageCheck, Plus, Truck, CheckCircle, Package } from "lucide-react";
import api from "@/lib/api";

interface PackingSlip {
  id: number;
  packingNumber: string;
  orderId: number;
  orderNumber?: string;
  pickingOrderId?: number;
  status: string;
  trackingNumber?: string;
  carrier?: string;
  weight?: number;
  boxSize?: string;
  items: PackingItem[];
  createdAt: string;
  completedAt?: string;
  shippedAt?: string;
}

interface PackingItem {
  id: number;
  productId: number;
  product?: { name: string; sku?: string };
  quantity: number;
}

const statusColors: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  Packing: "bg-blue-100 text-blue-700",
  Completed: "bg-green-100 text-green-700",
  Shipped: "bg-purple-100 text-purple-700",
};

const carriers = ["CJ대한통운", "한진택배", "로젠택배", "우체국택배", "롯데택배", "DHL", "FedEx", "UPS", "EMS"];
const boxSizes = ["Small (30x20x15)", "Medium (40x30x20)", "Large (50x40x30)", "XLarge (60x50x40)", "Custom"];

export default function WmsPackingPage() {
  const [slips, setSlips] = useState<PackingSlip[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [showComplete, setShowComplete] = useState<PackingSlip | null>(null);
  const [form, setForm] = useState({ orderId: "", pickingOrderId: "" });
  const [completeForm, setCompleteForm] = useState({ trackingNumber: "", carrier: "", weight: "", boxSize: "" });
  const [statusFilter, setStatusFilter] = useState("All");

  const fetchSlips = () => {
    setLoading(true);
    setError("");
    const params = statusFilter !== "All" ? `?status=${statusFilter}` : "";
    api.get(`/wms/packing${params}`)
      .then(r => setSlips(r.data))
      .catch(() => setError("Failed to load packing slips"))
      .finally(() => setLoading(false));
  };

  useEffect(() => { fetchSlips(); }, [statusFilter]);

  const createSlip = async () => {
    try {
      await api.post("/wms/packing", {
        orderId: parseInt(form.orderId),
        pickingOrderId: form.pickingOrderId ? parseInt(form.pickingOrderId) : null,
      });
      setShowCreate(false);
      setForm({ orderId: "", pickingOrderId: "" });
      fetchSlips();
    } catch {
      setError("Failed to create packing slip");
    }
  };

  const completePacking = async () => {
    if (!showComplete) return;
    try {
      await api.put(`/wms/packing/${showComplete.id}/complete`, {
        trackingNumber: completeForm.trackingNumber || null,
        carrier: completeForm.carrier || null,
        weight: completeForm.weight ? parseFloat(completeForm.weight) : null,
        boxSize: completeForm.boxSize || null,
      });
      setShowComplete(null);
      setCompleteForm({ trackingNumber: "", carrier: "", weight: "", boxSize: "" });
      fetchSlips();
    } catch {
      setError("Failed to complete packing");
    }
  };

  const shipPacking = async (id: number) => {
    try {
      await api.put(`/wms/packing/${id}/ship`);
      fetchSlips();
    } catch {
      setError("Failed to mark as shipped");
    }
  };

  const openComplete = (slip: PackingSlip) => {
    setShowComplete(slip);
    setCompleteForm({
      trackingNumber: slip.trackingNumber || "",
      carrier: slip.carrier || "",
      weight: slip.weight?.toString() || "",
      boxSize: slip.boxSize || "",
    });
  };

  const tabs = ["All", "Pending", "Packing", "Completed", "Shipped"];

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><PackageCheck size={24} /> Packing Slips</h1>
          <p className="text-gray-500 text-sm mt-1">{slips.length} slips</p>
        </div>
        <button onClick={() => setShowCreate(true)} className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"><Plus size={16} /> Create Packing</button>
      </div>

      <div className="flex gap-1 mb-4 bg-gray-100 rounded-lg p-1 w-fit">
        {tabs.map(tab => (
          <button key={tab} onClick={() => setStatusFilter(tab)} className={`px-4 py-1.5 rounded-md text-sm font-medium transition ${statusFilter === tab ? "bg-white text-gray-900 shadow-sm" : "text-gray-500 hover:text-gray-700"}`}>
            {tab}
          </button>
        ))}
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      <div className="bg-white rounded-xl border overflow-hidden">
        {loading ? <div className="p-12 text-center text-gray-400">Loading...</div> : slips.length === 0 ? (
          <div className="p-12 text-center text-gray-400">
            <Package className="w-12 h-12 mx-auto mb-3 text-gray-300" />
            <p>No packing slips</p>
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 text-gray-500 text-xs uppercase">
                <th className="text-left px-4 py-3">Packing #</th>
                <th className="text-left px-4 py-3">Order</th>
                <th className="text-left px-4 py-3">Status</th>
                <th className="text-left px-4 py-3">Carrier</th>
                <th className="text-left px-4 py-3">Tracking</th>
                <th className="text-center px-4 py-3">Weight</th>
                <th className="text-left px-4 py-3">Box</th>
                <th className="text-center px-4 py-3">Items</th>
                <th className="text-center px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {slips.map(slip => (
                <tr key={slip.id} className="border-t hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-xs">{slip.packingNumber}</td>
                  <td className="px-4 py-3 text-gray-700">{slip.orderNumber || `#${slip.orderId}`}</td>
                  <td className="px-4 py-3"><span className={`px-2 py-0.5 rounded text-xs font-medium ${statusColors[slip.status] || "bg-gray-100"}`}>{slip.status}</span></td>
                  <td className="px-4 py-3 text-gray-500 text-xs">{slip.carrier || "-"}</td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-500">{slip.trackingNumber || "-"}</td>
                  <td className="px-4 py-3 text-center text-xs text-gray-500">{slip.weight ? `${slip.weight}kg` : "-"}</td>
                  <td className="px-4 py-3 text-xs text-gray-500">{slip.boxSize || "-"}</td>
                  <td className="px-4 py-3 text-center text-xs">{slip.items?.length || 0}</td>
                  <td className="px-4 py-3 text-center">
                    <div className="flex items-center justify-center gap-1">
                      {(slip.status === "Pending" || slip.status === "Packing") && (
                        <button onClick={() => openComplete(slip)} className="p-1 text-green-600 hover:text-green-800 hover:bg-green-50 rounded" title="Complete Packing"><CheckCircle size={14} /></button>
                      )}
                      {slip.status === "Completed" && (
                        <button onClick={() => shipPacking(slip.id)} className="p-1 text-purple-600 hover:text-purple-800 hover:bg-purple-50 rounded" title="Mark Shipped"><Truck size={14} /></button>
                      )}
                      {slip.status === "Shipped" && (
                        <span className="text-xs text-gray-400">{slip.shippedAt ? new Date(slip.shippedAt).toLocaleDateString() : "Shipped"}</span>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-4">Create Packing Slip</h3>
            <div className="space-y-3">
              <div>
                <label className="block text-xs text-gray-500 mb-1">Order ID</label>
                <input type="number" value={form.orderId} onChange={e => setForm({...form, orderId: e.target.value})} placeholder="Order ID" className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Picking Order ID (optional)</label>
                <input type="number" value={form.pickingOrderId} onChange={e => setForm({...form, pickingOrderId: e.target.value})} placeholder="Link to picking order" className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createSlip} disabled={!form.orderId} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed">Create</button>
            </div>
          </div>
        </div>
      )}

      {showComplete && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-1">Complete Packing</h3>
            <p className="text-sm text-gray-500 mb-4">{showComplete.packingNumber} - Order {showComplete.orderNumber || `#${showComplete.orderId}`}</p>

            {showComplete.items && showComplete.items.length > 0 && (
              <div className="bg-gray-50 rounded-lg p-3 mb-4">
                <p className="text-xs font-medium text-gray-600 mb-2">Items ({showComplete.items.length})</p>
                {showComplete.items.map(item => (
                  <div key={item.id} className="flex justify-between text-xs py-1">
                    <span className="text-gray-700">{item.product?.name || `Product #${item.productId}`}</span>
                    <span className="font-mono text-gray-500">x{item.quantity}</span>
                  </div>
                ))}
              </div>
            )}

            <div className="space-y-3">
              <div>
                <label className="block text-xs text-gray-500 mb-1">Carrier</label>
                <select value={completeForm.carrier} onChange={e => setCompleteForm({...completeForm, carrier: e.target.value})} className="w-full px-3 py-2 border rounded-lg text-sm">
                  <option value="">Select carrier</option>
                  {carriers.map(c => <option key={c} value={c}>{c}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Tracking Number</label>
                <input value={completeForm.trackingNumber} onChange={e => setCompleteForm({...completeForm, trackingNumber: e.target.value})} placeholder="Tracking number" className="w-full px-3 py-2 border rounded-lg text-sm font-mono" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Weight (kg)</label>
                  <input type="number" step="0.1" value={completeForm.weight} onChange={e => setCompleteForm({...completeForm, weight: e.target.value})} placeholder="0.0" className="w-full px-3 py-2 border rounded-lg text-sm" />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Box Size</label>
                  <select value={completeForm.boxSize} onChange={e => setCompleteForm({...completeForm, boxSize: e.target.value})} className="w-full px-3 py-2 border rounded-lg text-sm">
                    <option value="">Select size</option>
                    {boxSizes.map(s => <option key={s} value={s}>{s}</option>)}
                  </select>
                </div>
              </div>
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowComplete(null)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={completePacking} className="px-4 py-2 bg-green-600 text-white rounded-lg text-sm hover:bg-green-700 flex items-center gap-2"><CheckCircle size={14} /> Complete</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
