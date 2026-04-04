"use client";
import { useEffect, useState } from "react";
import { ClipboardList, Plus, Truck, CheckCircle, Send, Package, AlertTriangle } from "lucide-react";
import api from "@/lib/api";

interface Supplier { id: number; name: string; code: string; }
interface ProcurementItem { id?: number; productId: number; productName?: string; quantity: number; unitPrice: number; }
interface ProcurementOrder {
  id: number;
  orderNumber: string;
  supplierId: number;
  supplierName: string;
  status: string;
  items: ProcurementItem[];
  totalAmount: number;
  expectedDeliveryDate: string;
  actualDeliveryDate?: string;
  trackingNumber?: string;
  notes?: string;
  createdAt: string;
}

const statusTabs = ["All", "Draft", "Submitted", "Confirmed", "Shipped", "Delivered"];
const statusColors: Record<string, string> = {
  Draft: "bg-gray-100 text-gray-600",
  Submitted: "bg-blue-100 text-blue-700",
  Confirmed: "bg-indigo-100 text-indigo-700",
  Shipped: "bg-yellow-100 text-yellow-700",
  Delivered: "bg-green-100 text-green-700",
};

export default function ScmProcurementPage() {
  const [orders, setOrders] = useState<ProcurementOrder[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState("All");
  const [showCreate, setShowCreate] = useState(false);
  const [trackingModal, setTrackingModal] = useState<ProcurementOrder | null>(null);
  const [trackingNumber, setTrackingNumber] = useState("");
  const [form, setForm] = useState({ supplierId: 0, expectedDeliveryDate: "", notes: "" });
  const [items, setItems] = useState<ProcurementItem[]>([{ productId: 0, quantity: 1, unitPrice: 0 }]);

  const fetchOrders = () => {
    setLoading(true);
    api.get("/scm/procurement-orders").then(r => setOrders(r.data)).catch(() => {}).finally(() => setLoading(false));
  };
  const fetchSuppliers = () => {
    api.get("/scm/suppliers").then(r => setSuppliers(r.data)).catch(() => {});
  };
  useEffect(() => { fetchOrders(); fetchSuppliers(); }, []);

  const filtered = activeTab === "All" ? orders : orders.filter(o => o.status === activeTab);

  const isOverdue = (order: ProcurementOrder) => {
    if (order.status === "Delivered") return false;
    return new Date(order.expectedDeliveryDate) < new Date();
  };

  const createOrder = async () => {
    const validItems = items.filter(i => i.productId > 0 && i.quantity > 0);
    if (!form.supplierId || validItems.length === 0) return;
    await api.post("/scm/procurement-orders", { ...form, items: validItems });
    setShowCreate(false);
    setForm({ supplierId: 0, expectedDeliveryDate: "", notes: "" });
    setItems([{ productId: 0, quantity: 1, unitPrice: 0 }]);
    fetchOrders();
  };

  const performAction = async (orderId: number, action: string, body?: object) => {
    await api.put(`/scm/procurement-orders/${orderId}/${action}`, body || {});
    fetchOrders();
  };

  const shipWithTracking = async () => {
    if (!trackingModal) return;
    await performAction(trackingModal.id, "ship", { trackingNumber });
    setTrackingModal(null);
    setTrackingNumber("");
  };

  const addItem = () => setItems([...items, { productId: 0, quantity: 1, unitPrice: 0 }]);
  const removeItem = (idx: number) => setItems(items.filter((_, i) => i !== idx));
  const updateItem = (idx: number, field: keyof ProcurementItem, value: number) => {
    const updated = [...items];
    (updated[idx] as any)[field] = value;
    setItems(updated);
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><ClipboardList size={24} /> Procurement Orders</h1>
          <p className="text-gray-500 text-sm mt-1">{orders.length} orders total</p>
        </div>
        <button onClick={() => setShowCreate(true)} className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"><Plus size={16} /> New Order</button>
      </div>

      <div className="flex gap-1 mb-4 bg-gray-100 rounded-lg p-1">
        {statusTabs.map(tab => (
          <button key={tab} onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${activeTab === tab ? "bg-white text-gray-900 shadow-sm" : "text-gray-500 hover:text-gray-700"}`}>
            {tab}
            {tab !== "All" && <span className="ml-1.5 text-xs text-gray-400">({orders.filter(o => tab === "All" || o.status === tab).length})</span>}
          </button>
        ))}
      </div>

      <div className="bg-white rounded-xl border overflow-hidden">
        {loading ? <div className="p-12 text-center text-gray-400">Loading...</div> : filtered.length === 0 ? <div className="p-12 text-center text-gray-400">No orders found</div> : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr className="text-gray-500 text-xs uppercase">
                <th className="text-left px-4 py-3">Order #</th>
                <th className="text-left px-4 py-3">Supplier</th>
                <th className="text-center px-4 py-3">Status</th>
                <th className="text-right px-4 py-3">Items</th>
                <th className="text-right px-4 py-3">Total</th>
                <th className="text-left px-4 py-3">Expected</th>
                <th className="text-left px-4 py-3">Actual</th>
                <th className="text-center px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(order => (
                <tr key={order.id} className={`border-t hover:bg-gray-50 ${isOverdue(order) ? "bg-red-50" : ""}`}>
                  <td className="px-4 py-3 font-mono text-xs font-medium">
                    <div className="flex items-center gap-1.5">
                      {isOverdue(order) && <AlertTriangle size={14} className="text-red-500" />}
                      {order.orderNumber}
                    </div>
                  </td>
                  <td className="px-4 py-3">{order.supplierName}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`px-2 py-0.5 rounded text-xs ${statusColors[order.status] || "bg-gray-100"}`}>{order.status}</span>
                  </td>
                  <td className="px-4 py-3 text-right">{order.items?.length || 0}</td>
                  <td className="px-4 py-3 text-right font-medium">{order.totalAmount?.toLocaleString()}원</td>
                  <td className={`px-4 py-3 text-xs ${isOverdue(order) ? "text-red-600 font-semibold" : "text-gray-500"}`}>
                    {new Date(order.expectedDeliveryDate).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-xs text-gray-500">
                    {order.actualDeliveryDate ? new Date(order.actualDeliveryDate).toLocaleDateString() : "-"}
                  </td>
                  <td className="px-4 py-3 text-center">
                    <div className="flex items-center justify-center gap-1">
                      {order.status === "Draft" && (
                        <button onClick={() => performAction(order.id, "submit")} className="px-2 py-1 text-xs bg-blue-50 text-blue-600 rounded hover:bg-blue-100 flex items-center gap-1" title="Submit">
                          <Send size={12} /> Submit
                        </button>
                      )}
                      {order.status === "Submitted" && (
                        <button onClick={() => performAction(order.id, "confirm")} className="px-2 py-1 text-xs bg-indigo-50 text-indigo-600 rounded hover:bg-indigo-100 flex items-center gap-1" title="Confirm">
                          <CheckCircle size={12} /> Confirm
                        </button>
                      )}
                      {order.status === "Confirmed" && (
                        <button onClick={() => { setTrackingModal(order); setTrackingNumber(order.trackingNumber || ""); }} className="px-2 py-1 text-xs bg-yellow-50 text-yellow-600 rounded hover:bg-yellow-100 flex items-center gap-1" title="Ship">
                          <Truck size={12} /> Ship
                        </button>
                      )}
                      {order.status === "Shipped" && (
                        <button onClick={() => performAction(order.id, "deliver")} className="px-2 py-1 text-xs bg-green-50 text-green-600 rounded hover:bg-green-100 flex items-center gap-1" title="Deliver">
                          <Package size={12} /> Deliver
                        </button>
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
          <div className="bg-white rounded-2xl p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <h3 className="text-lg font-bold mb-4">New Procurement Order</h3>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Supplier</label>
                  <select value={form.supplierId} onChange={e => setForm({ ...form, supplierId: parseInt(e.target.value) })} className="w-full px-3 py-2 border rounded-lg text-sm">
                    <option value={0}>Select supplier...</option>
                    {suppliers.map(s => <option key={s.id} value={s.id}>{s.name} ({s.code})</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Expected Delivery</label>
                  <input type="date" value={form.expectedDeliveryDate} onChange={e => setForm({ ...form, expectedDeliveryDate: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm" />
                </div>
              </div>

              <div>
                <div className="flex justify-between items-center mb-2">
                  <label className="text-xs text-gray-500 font-medium">Order Items</label>
                  <button onClick={addItem} className="text-xs text-blue-600 hover:underline flex items-center gap-1"><Plus size={12} /> Add Item</button>
                </div>
                <div className="space-y-2">
                  {items.map((item, idx) => (
                    <div key={idx} className="flex gap-2 items-center">
                      <input type="number" value={item.productId || ""} onChange={e => updateItem(idx, "productId", parseInt(e.target.value) || 0)} placeholder="Product ID" className="flex-1 px-3 py-2 border rounded-lg text-sm" />
                      <input type="number" value={item.quantity || ""} onChange={e => updateItem(idx, "quantity", parseInt(e.target.value) || 0)} placeholder="Qty" className="w-24 px-3 py-2 border rounded-lg text-sm" />
                      <input type="number" value={item.unitPrice || ""} onChange={e => updateItem(idx, "unitPrice", parseFloat(e.target.value) || 0)} placeholder="Unit Price" className="w-32 px-3 py-2 border rounded-lg text-sm" />
                      {items.length > 1 && <button onClick={() => removeItem(idx)} className="text-red-400 hover:text-red-600 text-xs">Remove</button>}
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-xs text-gray-500 mb-1">Notes</label>
                <textarea value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} placeholder="Order notes..." className="w-full px-3 py-2 border rounded-lg text-sm" rows={2} />
              </div>
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createOrder} disabled={!form.supplierId || items.every(i => !i.productId)} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">Create Order</button>
            </div>
          </div>
        </div>
      )}

      {trackingModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-1">Ship Order</h3>
            <p className="text-sm text-gray-500 mb-4">{trackingModal.orderNumber}</p>
            <div>
              <label className="block text-xs text-gray-500 mb-1">Tracking Number</label>
              <input value={trackingNumber} onChange={e => setTrackingNumber(e.target.value)} placeholder="Enter tracking number..." className="w-full px-3 py-2 border rounded-lg text-sm font-mono" />
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setTrackingModal(null)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={shipWithTracking} className="px-4 py-2 bg-yellow-500 text-white rounded-lg text-sm hover:bg-yellow-600">Mark as Shipped</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
