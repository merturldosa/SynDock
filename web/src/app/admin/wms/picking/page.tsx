"use client";
import { useEffect, useState } from "react";
import { ClipboardList, Plus, Play, CheckCircle, X, AlertTriangle } from "lucide-react";
import api from "@/lib/api";

interface PickingItem {
  id: number;
  productId: number;
  product?: { name: string; sku?: string };
  locationId?: number;
  location?: { code: string };
  quantityRequired: number;
  quantityPicked: number;
  isPicked: boolean;
}

interface PickingOrder {
  id: number;
  pickingNumber: string;
  orderId: number;
  orderNumber?: string;
  status: string;
  assignedTo?: string;
  priority: string;
  items: PickingItem[];
  totalItems: number;
  pickedItems: number;
  createdAt: string;
  completedAt?: string;
}

const statusColors: Record<string, string> = {
  Pending: "bg-yellow-100 text-yellow-700",
  InProgress: "bg-blue-100 text-blue-700",
  Completed: "bg-green-100 text-green-700",
  Cancelled: "bg-gray-100 text-gray-500",
};

const priorityColors: Record<string, string> = {
  Low: "text-gray-400",
  Normal: "text-gray-600",
  High: "text-orange-500",
  Urgent: "text-red-600",
};

const tabs = ["All", "Pending", "InProgress", "Completed"];

export default function WmsPickingPage() {
  const [orders, setOrders] = useState<PickingOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [activeTab, setActiveTab] = useState("All");
  const [showCreate, setShowCreate] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState<PickingOrder | null>(null);
  const [form, setForm] = useState({ orderId: "", assignedTo: "", priority: "Normal" });

  const fetchOrders = () => {
    setLoading(true);
    setError("");
    const params = activeTab !== "All" ? `?status=${activeTab}` : "";
    api.get(`/wms/picking${params}`)
      .then(r => setOrders(r.data))
      .catch(() => setError("Failed to load picking orders"))
      .finally(() => setLoading(false));
  };

  useEffect(() => { fetchOrders(); }, [activeTab]);

  const createOrder = async () => {
    try {
      await api.post("/wms/picking", {
        orderId: parseInt(form.orderId),
        assignedTo: form.assignedTo || null,
        priority: form.priority,
      });
      setShowCreate(false);
      setForm({ orderId: "", assignedTo: "", priority: "Normal" });
      fetchOrders();
    } catch {
      setError("Failed to create picking order");
    }
  };

  const startPicking = async (id: number) => {
    try {
      await api.put(`/wms/picking/${id}/start`);
      fetchOrders();
      if (selectedOrder?.id === id) {
        const r = await api.get(`/wms/picking/${id}`);
        setSelectedOrder(r.data);
      }
    } catch {
      setError("Failed to start picking");
    }
  };

  const completePicking = async (id: number) => {
    try {
      await api.put(`/wms/picking/${id}/complete`);
      fetchOrders();
      setSelectedOrder(null);
    } catch {
      setError("Failed to complete picking");
    }
  };

  const confirmPickItem = async (pickingId: number, itemId: number, quantity: number) => {
    try {
      await api.put(`/wms/picking/${pickingId}/items/${itemId}/pick`, { quantityPicked: quantity });
      const r = await api.get(`/wms/picking/${pickingId}`);
      setSelectedOrder(r.data);
      fetchOrders();
    } catch {
      setError("Failed to confirm pick");
    }
  };

  const openDetail = async (order: PickingOrder) => {
    try {
      const r = await api.get(`/wms/picking/${order.id}`);
      setSelectedOrder(r.data);
    } catch {
      setSelectedOrder(order);
    }
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><ClipboardList size={24} /> Picking Orders</h1>
          <p className="text-gray-500 text-sm mt-1">{orders.length} orders</p>
        </div>
        <button onClick={() => setShowCreate(true)} className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"><Plus size={16} /> Create Picking</button>
      </div>

      <div className="flex gap-1 mb-4 bg-gray-100 rounded-lg p-1 w-fit">
        {tabs.map(tab => (
          <button key={tab} onClick={() => setActiveTab(tab)} className={`px-4 py-1.5 rounded-md text-sm font-medium transition ${activeTab === tab ? "bg-white text-gray-900 shadow-sm" : "text-gray-500 hover:text-gray-700"}`}>
            {tab === "InProgress" ? "In Progress" : tab}
          </button>
        ))}
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      <div className="flex gap-4">
        <div className={`bg-white rounded-xl border overflow-hidden ${selectedOrder ? "flex-1" : "w-full"}`}>
          {loading ? <div className="p-12 text-center text-gray-400">Loading...</div> : orders.length === 0 ? (
            <div className="p-12 text-center text-gray-400">
              <ClipboardList className="w-12 h-12 mx-auto mb-3 text-gray-300" />
              <p>No picking orders</p>
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 text-gray-500 text-xs uppercase">
                  <th className="text-left px-4 py-3">Picking #</th>
                  <th className="text-left px-4 py-3">Order</th>
                  <th className="text-left px-4 py-3">Status</th>
                  <th className="text-left px-4 py-3">Assigned</th>
                  <th className="text-center px-4 py-3">Items</th>
                  <th className="text-center px-4 py-3">Priority</th>
                  <th className="text-center px-4 py-3">Actions</th>
                </tr>
              </thead>
              <tbody>
                {orders.map(order => (
                  <tr key={order.id} onClick={() => openDetail(order)} className={`border-t hover:bg-gray-50 cursor-pointer ${selectedOrder?.id === order.id ? "bg-blue-50" : ""}`}>
                    <td className="px-4 py-3 font-mono text-xs">{order.pickingNumber}</td>
                    <td className="px-4 py-3 text-gray-700">{order.orderNumber || `#${order.orderId}`}</td>
                    <td className="px-4 py-3"><span className={`px-2 py-0.5 rounded text-xs font-medium ${statusColors[order.status] || "bg-gray-100"}`}>{order.status}</span></td>
                    <td className="px-4 py-3 text-gray-500">{order.assignedTo || "-"}</td>
                    <td className="px-4 py-3 text-center text-xs">
                      <span className="font-medium">{order.pickedItems || 0}</span>
                      <span className="text-gray-400">/{order.totalItems || order.items?.length || 0}</span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <span className={`text-xs font-medium ${priorityColors[order.priority] || ""}`}>
                        {order.priority === "Urgent" && <AlertTriangle size={12} className="inline mr-1" />}
                        {order.priority}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      {order.status === "Pending" && (
                        <button onClick={(e) => { e.stopPropagation(); startPicking(order.id); }} className="text-blue-600 hover:text-blue-800" title="Start"><Play size={14} /></button>
                      )}
                      {order.status === "InProgress" && (
                        <button onClick={(e) => { e.stopPropagation(); completePicking(order.id); }} className="text-green-600 hover:text-green-800" title="Complete"><CheckCircle size={14} /></button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {selectedOrder && (
          <div className="w-96 bg-white rounded-xl border p-4 h-fit">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="font-bold text-gray-900">{selectedOrder.pickingNumber}</h3>
                <p className="text-xs text-gray-500">Order {selectedOrder.orderNumber || `#${selectedOrder.orderId}`}</p>
              </div>
              <button onClick={() => setSelectedOrder(null)} className="text-gray-400 hover:text-gray-600"><X size={16} /></button>
            </div>

            <div className="flex gap-2 mb-4">
              <span className={`px-2 py-0.5 rounded text-xs font-medium ${statusColors[selectedOrder.status] || "bg-gray-100"}`}>{selectedOrder.status}</span>
              <span className={`text-xs font-medium ${priorityColors[selectedOrder.priority] || ""}`}>{selectedOrder.priority}</span>
            </div>

            {selectedOrder.assignedTo && <p className="text-xs text-gray-500 mb-4">Assigned: {selectedOrder.assignedTo}</p>}

            <h4 className="text-sm font-medium text-gray-700 mb-2">Pick Items</h4>
            <div className="space-y-2">
              {(selectedOrder.items || []).map(item => (
                <div key={item.id} className={`p-3 rounded-lg border text-xs ${item.isPicked ? "bg-green-50 border-green-200" : "bg-white"}`}>
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">{item.product?.name || `Product #${item.productId}`}</p>
                      {item.product?.sku && <p className="text-gray-400 font-mono">{item.product.sku}</p>}
                      {item.location && <p className="text-gray-400 mt-0.5">Loc: {item.location.code}</p>}
                    </div>
                    <div className="text-right">
                      <p className="font-mono">{item.quantityPicked}/{item.quantityRequired}</p>
                      {!item.isPicked && selectedOrder.status === "InProgress" && (
                        <button
                          onClick={() => confirmPickItem(selectedOrder.id, item.id, item.quantityRequired)}
                          className="mt-1 px-2 py-0.5 bg-blue-600 text-white rounded text-xs hover:bg-blue-700"
                        >
                          Pick
                        </button>
                      )}
                      {item.isPicked && <CheckCircle size={14} className="text-green-500 ml-auto mt-1" />}
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {selectedOrder.status === "Pending" && (
              <button onClick={() => startPicking(selectedOrder.id)} className="w-full mt-4 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 flex items-center justify-center gap-2"><Play size={14} /> Start Picking</button>
            )}
            {selectedOrder.status === "InProgress" && (
              <button onClick={() => completePicking(selectedOrder.id)} className="w-full mt-4 px-4 py-2 bg-green-600 text-white rounded-lg text-sm hover:bg-green-700 flex items-center justify-center gap-2"><CheckCircle size={14} /> Complete Picking</button>
            )}
          </div>
        )}
      </div>

      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-4">Create Picking Order</h3>
            <div className="space-y-3">
              <div>
                <label className="block text-xs text-gray-500 mb-1">Order ID</label>
                <input type="number" value={form.orderId} onChange={e => setForm({...form, orderId: e.target.value})} placeholder="Order ID" className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Assigned To</label>
                <input value={form.assignedTo} onChange={e => setForm({...form, assignedTo: e.target.value})} placeholder="Worker name (optional)" className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Priority</label>
                <select value={form.priority} onChange={e => setForm({...form, priority: e.target.value})} className="w-full px-3 py-2 border rounded-lg text-sm">
                  {["Low","Normal","High","Urgent"].map(p => <option key={p} value={p}>{p}</option>)}
                </select>
              </div>
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createOrder} disabled={!form.orderId} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed">Create</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
