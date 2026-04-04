"use client";
import { useEffect, useState } from "react";
import { Users, Plus, X, Star, ChevronRight, ChevronLeft } from "lucide-react";
import api from "@/lib/api";

interface Supplier {
  id: number;
  code: string;
  name: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
  businessNumber?: string;
  leadTimeDays: number;
  grade: string;
  onTimeRate: number;
  defectRate: number;
  status: string;
  notes?: string;
  createdAt: string;
  evaluations?: Evaluation[];
}

interface Evaluation {
  id: number;
  period: string;
  qualityScore: number;
  deliveryScore: number;
  priceScore: number;
  serviceScore: number;
  overallScore: number;
  evaluatedAt: string;
}

const gradeColors: Record<string, string> = {
  S: "bg-purple-100 text-purple-700 border-purple-300",
  A: "bg-blue-100 text-blue-700 border-blue-300",
  B: "bg-green-100 text-green-700 border-green-300",
  C: "bg-yellow-100 text-yellow-700 border-yellow-300",
  D: "bg-red-100 text-red-700 border-red-300",
};

const statusColors: Record<string, string> = {
  Active: "bg-green-100 text-green-700",
  Inactive: "bg-gray-100 text-gray-500",
  Blacklisted: "bg-red-100 text-red-700",
};

export default function ScmSuppliersPage() {
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState<Supplier | null>(null);
  const [detail, setDetail] = useState<Supplier | null>(null);
  const [form, setForm] = useState({ name: "", code: "", contactName: "", email: "", phone: "", address: "", businessNumber: "", leadTimeDays: 7 });
  const [editForm, setEditForm] = useState({ status: "Active", grade: "C", notes: "" });

  const fetchSuppliers = () => {
    setLoading(true);
    api.get("/scm/suppliers").then(r => setSuppliers(r.data)).catch(() => {}).finally(() => setLoading(false));
  };
  useEffect(() => { fetchSuppliers(); }, []);

  const createSupplier = async () => {
    await api.post("/scm/suppliers", form);
    setShowCreate(false);
    setForm({ name: "", code: "", contactName: "", email: "", phone: "", address: "", businessNumber: "", leadTimeDays: 7 });
    fetchSuppliers();
  };

  const updateSupplier = async () => {
    if (!showEdit) return;
    await api.put(`/scm/suppliers/${showEdit.id}`, editForm);
    setShowEdit(null);
    fetchSuppliers();
    if (detail?.id === showEdit.id) {
      setDetail({ ...detail, ...editForm });
    }
  };

  const openDetail = async (supplier: Supplier) => {
    try {
      const r = await api.get(`/scm/suppliers/${supplier.id}`);
      setDetail(r.data);
    } catch {
      setDetail(supplier);
    }
  };

  const openEdit = (supplier: Supplier) => {
    setEditForm({ status: supplier.status, grade: supplier.grade, notes: supplier.notes || "" });
    setShowEdit(supplier);
  };

  return (
    <div className="flex gap-6">
      <div className={detail ? "flex-1 min-w-0" : "w-full"}>
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><Users size={24} /> Supplier Management</h1>
            <p className="text-gray-500 text-sm mt-1">{suppliers.length} suppliers registered</p>
          </div>
          <button onClick={() => setShowCreate(true)} className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"><Plus size={16} /> Add Supplier</button>
        </div>

        <div className="bg-white rounded-xl border overflow-hidden">
          {loading ? <div className="p-12 text-center text-gray-400">Loading...</div> : suppliers.length === 0 ? <div className="p-12 text-center text-gray-400">No suppliers yet</div> : (
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr className="text-gray-500 text-xs uppercase">
                  <th className="text-left px-4 py-3">Code</th>
                  <th className="text-left px-4 py-3">Name</th>
                  <th className="text-left px-4 py-3">Contact</th>
                  <th className="text-center px-4 py-3">Grade</th>
                  <th className="text-right px-4 py-3">Lead Time</th>
                  <th className="text-right px-4 py-3">On-Time</th>
                  <th className="text-right px-4 py-3">Defect</th>
                  <th className="text-center px-4 py-3">Status</th>
                  <th className="text-center px-4 py-3">Actions</th>
                </tr>
              </thead>
              <tbody>
                {suppliers.map(s => (
                  <tr key={s.id} className="border-t hover:bg-gray-50 cursor-pointer" onClick={() => openDetail(s)}>
                    <td className="px-4 py-3 font-mono text-xs">{s.code}</td>
                    <td className="px-4 py-3 font-medium">{s.name}</td>
                    <td className="px-4 py-3 text-gray-500">{s.contactName || "-"}</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`px-2 py-0.5 rounded border text-xs font-bold ${gradeColors[s.grade] || "bg-gray-100 text-gray-500"}`}>{s.grade}</span>
                    </td>
                    <td className="px-4 py-3 text-right">{s.leadTimeDays}d</td>
                    <td className="px-4 py-3 text-right">{(s.onTimeRate * 100).toFixed(1)}%</td>
                    <td className="px-4 py-3 text-right">{(s.defectRate * 100).toFixed(2)}%</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`px-2 py-0.5 rounded text-xs ${statusColors[s.status] || "bg-gray-100 text-gray-500"}`}>{s.status}</span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <button onClick={(e) => { e.stopPropagation(); openEdit(s); }} className="text-xs text-blue-600 hover:underline">Edit</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {detail && (
        <div className="w-96 shrink-0">
          <div className="bg-white rounded-xl border p-5 sticky top-4">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h2 className="text-lg font-bold text-gray-900">{detail.name}</h2>
                <p className="text-xs text-gray-500 font-mono">{detail.code}</p>
              </div>
              <button onClick={() => setDetail(null)} className="text-gray-400 hover:text-gray-600"><X size={18} /></button>
            </div>
            <div className="space-y-3 text-sm">
              <div className="flex justify-between"><span className="text-gray-500">Grade</span><span className={`px-2 py-0.5 rounded border text-xs font-bold ${gradeColors[detail.grade] || ""}`}>{detail.grade}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Status</span><span className={`px-2 py-0.5 rounded text-xs ${statusColors[detail.status] || ""}`}>{detail.status}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Contact</span><span>{detail.contactName || "-"}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Email</span><span className="text-xs">{detail.email || "-"}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Phone</span><span>{detail.phone || "-"}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Lead Time</span><span>{detail.leadTimeDays} days</span></div>
              <div className="flex justify-between"><span className="text-gray-500">On-Time Rate</span><span>{(detail.onTimeRate * 100).toFixed(1)}%</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Defect Rate</span><span>{(detail.defectRate * 100).toFixed(2)}%</span></div>
              {detail.businessNumber && <div className="flex justify-between"><span className="text-gray-500">Business #</span><span className="font-mono text-xs">{detail.businessNumber}</span></div>}
              {detail.notes && <div className="pt-2 border-t"><p className="text-gray-500 text-xs mb-1">Notes</p><p className="text-xs text-gray-700">{detail.notes}</p></div>}
            </div>

            <div className="mt-5 pt-4 border-t">
              <h3 className="text-sm font-semibold text-gray-900 flex items-center gap-1 mb-3"><Star size={14} /> Evaluation History</h3>
              {!detail.evaluations || detail.evaluations.length === 0 ? (
                <p className="text-xs text-gray-400">No evaluations yet</p>
              ) : (
                <div className="space-y-2 max-h-60 overflow-y-auto">
                  {detail.evaluations.map(ev => (
                    <div key={ev.id} className="bg-gray-50 rounded-lg p-3 text-xs">
                      <div className="flex justify-between mb-2">
                        <span className="font-medium">{ev.period}</span>
                        <span className="text-gray-400">{new Date(ev.evaluatedAt).toLocaleDateString()}</span>
                      </div>
                      <div className="grid grid-cols-2 gap-1 text-gray-600">
                        <span>Quality: {ev.qualityScore}</span>
                        <span>Delivery: {ev.deliveryScore}</span>
                        <span>Price: {ev.priceScore}</span>
                        <span>Service: {ev.serviceScore}</span>
                      </div>
                      <div className="mt-1 pt-1 border-t border-gray-200 font-semibold text-gray-800">Overall: {ev.overallScore.toFixed(1)}</div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-lg">
            <h3 className="text-lg font-bold mb-4">New Supplier</h3>
            <div className="grid grid-cols-2 gap-3">
              <input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} placeholder="Supplier Name" className="w-full px-3 py-2 border rounded-lg text-sm col-span-2" />
              <input value={form.code} onChange={e => setForm({ ...form, code: e.target.value.toUpperCase() })} placeholder="Code (e.g. SUP-001)" className="w-full px-3 py-2 border rounded-lg text-sm font-mono" />
              <input value={form.businessNumber} onChange={e => setForm({ ...form, businessNumber: e.target.value })} placeholder="Business Number" className="w-full px-3 py-2 border rounded-lg text-sm" />
              <input value={form.contactName} onChange={e => setForm({ ...form, contactName: e.target.value })} placeholder="Contact Name" className="w-full px-3 py-2 border rounded-lg text-sm" />
              <input value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} placeholder="Email" className="w-full px-3 py-2 border rounded-lg text-sm" />
              <input value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} placeholder="Phone" className="w-full px-3 py-2 border rounded-lg text-sm" />
              <input type="number" value={form.leadTimeDays} onChange={e => setForm({ ...form, leadTimeDays: parseInt(e.target.value) || 0 })} placeholder="Lead Time (days)" className="w-full px-3 py-2 border rounded-lg text-sm" />
              <input value={form.address} onChange={e => setForm({ ...form, address: e.target.value })} placeholder="Address" className="w-full px-3 py-2 border rounded-lg text-sm col-span-2" />
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createSupplier} disabled={!form.name || !form.code} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">Create</button>
            </div>
          </div>
        </div>
      )}

      {showEdit && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold mb-1">Edit Supplier</h3>
            <p className="text-sm text-gray-500 mb-4">{showEdit.name} ({showEdit.code})</p>
            <div className="space-y-3">
              <div>
                <label className="block text-xs text-gray-500 mb-1">Status</label>
                <select value={editForm.status} onChange={e => setEditForm({ ...editForm, status: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm">
                  {["Active", "Inactive", "Blacklisted"].map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Grade</label>
                <select value={editForm.grade} onChange={e => setEditForm({ ...editForm, grade: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm">
                  {["S", "A", "B", "C", "D"].map(g => <option key={g} value={g}>{g}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-500 mb-1">Notes</label>
                <textarea value={editForm.notes} onChange={e => setEditForm({ ...editForm, notes: e.target.value })} placeholder="Internal notes..." className="w-full px-3 py-2 border rounded-lg text-sm" rows={3} />
              </div>
            </div>
            <div className="flex gap-2 mt-4 justify-end">
              <button onClick={() => setShowEdit(null)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={updateSupplier} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700">Save</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
