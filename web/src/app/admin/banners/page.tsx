"use client";

import { useEffect, useState } from "react";
import { Plus, Trash2, Pencil, ToggleLeft, ToggleRight, X } from "lucide-react";
import toast from "react-hot-toast";
import { useTranslations } from "next-intl";
import api from "@/lib/api";
import { formatDateShort } from "@/lib/format";

interface BannerDto {
  id: number;
  title: string;
  description: string | null;
  imageUrl: string | null;
  linkUrl: string | null;
  displayType: string;
  pageTarget: string | null;
  startDate: string | null;
  endDate: string | null;
  sortOrder: number;
  isActive: boolean;
}

interface BannerForm {
  title: string;
  description: string;
  imageUrl: string;
  linkUrl: string;
  displayType: string;
  pageTarget: string;
  startDate: string;
  endDate: string;
  sortOrder: number;
}

const EMPTY_FORM: BannerForm = {
  title: "",
  description: "",
  imageUrl: "",
  linkUrl: "",
  displayType: "Banner",
  pageTarget: "",
  startDate: "",
  endDate: "",
  sortOrder: 0,
};

export default function AdminBannersPage() {
  const t = useTranslations();
  const [banners, setBanners] = useState<BannerDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<BannerForm>(EMPTY_FORM);
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    setLoading(true);
    api
      .get("/banner")
      .then(({ data }) => setBanners(Array.isArray(data) ? data : data.items || []))
      .catch(() => toast.error(t("common.fetchError")))
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const openCreate = () => {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setShowForm(true);
  };

  const openEdit = (b: BannerDto) => {
    setEditingId(b.id);
    setForm({
      title: b.title,
      description: b.description || "",
      imageUrl: b.imageUrl || "",
      linkUrl: b.linkUrl || "",
      displayType: b.displayType,
      pageTarget: b.pageTarget || "",
      startDate: b.startDate ? b.startDate.slice(0, 10) : "",
      endDate: b.endDate ? b.endDate.slice(0, 10) : "",
      sortOrder: b.sortOrder,
    });
    setShowForm(true);
  };

  const handleSave = async () => {
    if (!form.title.trim()) { toast.error("Title is required"); return; }
    setSubmitting(true);
    try {
      const payload = {
        ...form,
        description: form.description || null,
        linkUrl: form.linkUrl || null,
        pageTarget: form.pageTarget || null,
        startDate: form.startDate || null,
        endDate: form.endDate || null,
      };
      if (editingId) {
        await api.put(`/banner/${editingId}`, { ...payload, isActive: true });
      } else {
        await api.post("/banner", payload);
      }
      toast.success(t("common.saved"));
      setShowForm(false);
      load();
    } catch {
      toast.error(t("common.saveFailed"));
    }
    setSubmitting(false);
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm(t("common.deleteConfirm"))) return;
    try {
      await api.delete(`/banner/${id}`);
      load();
    } catch {
      toast.error(t("common.deleteFailed"));
    }
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">Banner Management</h1>
        <button
          onClick={openCreate}
          className="flex items-center gap-2 px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90"
        >
          <Plus size={16} /> Add Banner
        </button>
      </div>

      {/* Form Modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-bold text-[var(--color-secondary)]">
                {editingId ? "Edit Banner" : "Create Banner"}
              </h2>
              <button onClick={() => setShowForm(false)} className="text-gray-400 hover:text-gray-600">
                <X size={20} />
              </button>
            </div>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Title *</label>
                <input type="text" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm resize-none h-20" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Image URL</label>
                <input type="text" value={form.imageUrl} onChange={(e) => setForm({ ...form, imageUrl: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm" placeholder="https://..." />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Link URL</label>
                <input type="text" value={form.linkUrl} onChange={(e) => setForm({ ...form, linkUrl: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm" placeholder="https://..." />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Display Type</label>
                  <select value={form.displayType} onChange={(e) => setForm({ ...form, displayType: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm">
                    <option value="Banner">Banner</option>
                    <option value="Popup">Popup</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Page Target</label>
                  <input type="text" value={form.pageTarget} onChange={(e) => setForm({ ...form, pageTarget: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm" placeholder="home, products" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
                  <input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
                  <input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} className="w-full px-3 py-2 border rounded-lg text-sm" />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Sort Order</label>
                <input type="number" value={form.sortOrder} onChange={(e) => setForm({ ...form, sortOrder: parseInt(e.target.value) || 0 })} className="w-full px-3 py-2 border rounded-lg text-sm" />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button onClick={() => setShowForm(false)} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">{t("common.cancel")}</button>
                <button onClick={handleSave} disabled={submitting} className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90 disabled:opacity-60">
                  {submitting ? t("common.saving") : t("common.save")}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Banner Table */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : banners.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>No banners yet. Click &quot;Add Banner&quot; to create one.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3 font-medium text-gray-500">Title</th>
                <th className="text-left p-3 font-medium text-gray-500">Type</th>
                <th className="text-left p-3 font-medium text-gray-500">Page</th>
                <th className="text-left p-3 font-medium text-gray-500">Period</th>
                <th className="text-center p-3 font-medium text-gray-500">Order</th>
                <th className="text-center p-3 font-medium text-gray-500">Status</th>
                <th className="text-center p-3 font-medium text-gray-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              {banners.map((b) => (
                <tr key={b.id} className="border-b last:border-0 hover:bg-gray-50">
                  <td className="p-3">
                    <div className="flex items-center gap-3">
                      {b.imageUrl && <img src={b.imageUrl} alt={b.title} className="w-12 h-8 object-cover rounded" />}
                      <div>
                        <p className="font-medium text-[var(--color-secondary)]">{b.title}</p>
                        {b.description && <p className="text-xs text-gray-400 line-clamp-1">{b.description}</p>}
                      </div>
                    </div>
                  </td>
                  <td className="p-3 text-gray-500">{b.displayType}</td>
                  <td className="p-3 text-gray-500">{b.pageTarget || "-"}</td>
                  <td className="p-3 text-xs text-gray-500">
                    {b.startDate ? formatDateShort(b.startDate) : "-"} ~ {b.endDate ? formatDateShort(b.endDate) : "-"}
                  </td>
                  <td className="p-3 text-center text-gray-500">{b.sortOrder}</td>
                  <td className="p-3 text-center">
                    <span className={`px-2 py-0.5 text-xs rounded-full ${b.isActive ? "bg-emerald-100 text-emerald-700" : "bg-gray-100 text-gray-500"}`}>
                      {b.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="p-3">
                    <div className="flex items-center justify-center gap-2">
                      <button onClick={() => openEdit(b)} className="p-1.5 text-gray-400 hover:text-blue-500 transition-colors"><Pencil size={16} /></button>
                      <button onClick={() => handleDelete(b.id)} className="p-1.5 text-gray-400 hover:text-red-500 transition-colors"><Trash2 size={16} /></button>
                    </div>
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
