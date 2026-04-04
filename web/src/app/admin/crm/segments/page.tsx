"use client";

import { useEffect, useState } from "react";
import { Users, Plus, RefreshCw, X } from "lucide-react";
import api from "@/lib/api";

interface Segment {
  id: number;
  name: string;
  type: "Manual" | "Dynamic" | "AI";
  description?: string;
  rulesJson?: string;
  memberCount: number;
  status: string;
  lastCalculatedAt?: string;
  createdAt: string;
}

const TYPE_COLORS: Record<string, string> = {
  Manual: "bg-blue-100 text-blue-700",
  Dynamic: "bg-purple-100 text-purple-700",
  AI: "bg-emerald-100 text-emerald-700",
};

export default function CrmSegmentsPage() {
  const [segments, setSegments] = useState<Segment[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [recalculating, setRecalculating] = useState<number | null>(null);

  const [form, setForm] = useState({
    name: "",
    type: "Manual" as "Manual" | "Dynamic" | "AI",
    description: "",
    rulesJson: "",
  });
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    setLoading(true);
    api
      .get("/crm/segments")
      .then((res) => setSegments(res.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const handleCreate = async () => {
    if (!form.name.trim()) return;
    setSubmitting(true);
    try {
      await api.post("/crm/segments", {
        name: form.name,
        type: form.type,
        description: form.description,
        rulesJson: form.type === "Dynamic" ? form.rulesJson : undefined,
      });
      setShowModal(false);
      setForm({ name: "", type: "Manual", description: "", rulesJson: "" });
      load();
    } catch {
      // error handled silently
    }
    setSubmitting(false);
  };

  const handleRecalculate = async (id: number) => {
    setRecalculating(id);
    try {
      await api.post(`/crm/segments/${id}/recalculate`);
      load();
    } catch {
      // error handled silently
    }
    setRecalculating(null);
  };

  const formatDate = (d?: string) => {
    if (!d) return "-";
    return new Date(d).toLocaleDateString("ko-KR", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          Customer Segments
        </h1>
        <button
          onClick={() => setShowModal(true)}
          className="flex items-center gap-1.5 px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 transition-colors"
        >
          <Plus size={16} />
          New Segment
        </button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : segments.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <Users size={48} className="mx-auto mb-3 opacity-40" />
          <p>No segments created yet.</p>
        </div>
      ) : (
        <>
          <p className="text-sm text-gray-500 mb-3">
            Total {segments.length} segments
          </p>
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="text-left p-3 font-medium text-gray-500">
                    Name
                  </th>
                  <th className="text-center p-3 font-medium text-gray-500">
                    Type
                  </th>
                  <th className="text-right p-3 font-medium text-gray-500">
                    Members
                  </th>
                  <th className="text-center p-3 font-medium text-gray-500">
                    Status
                  </th>
                  <th className="text-left p-3 font-medium text-gray-500">
                    Last Calculated
                  </th>
                  <th className="text-center p-3 font-medium text-gray-500">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody>
                {segments.map((seg) => (
                  <tr
                    key={seg.id}
                    className="border-b last:border-0 hover:bg-gray-50"
                  >
                    <td className="p-3">
                      <p className="font-medium text-[var(--color-secondary)]">
                        {seg.name}
                      </p>
                      {seg.description && (
                        <p className="text-xs text-gray-400 mt-0.5">
                          {seg.description}
                        </p>
                      )}
                    </td>
                    <td className="p-3 text-center">
                      <span
                        className={`px-2 py-0.5 text-xs rounded-full ${TYPE_COLORS[seg.type] || "bg-gray-100 text-gray-700"}`}
                      >
                        {seg.type}
                      </span>
                    </td>
                    <td className="p-3 text-right font-medium">
                      {seg.memberCount.toLocaleString()}
                    </td>
                    <td className="p-3 text-center">
                      <span
                        className={`px-2 py-0.5 text-xs rounded-full ${
                          seg.status === "Active"
                            ? "bg-emerald-100 text-emerald-700"
                            : "bg-gray-100 text-gray-700"
                        }`}
                      >
                        {seg.status}
                      </span>
                    </td>
                    <td className="p-3 text-gray-500 text-xs">
                      {formatDate(seg.lastCalculatedAt)}
                    </td>
                    <td className="p-3 text-center">
                      <button
                        onClick={() => handleRecalculate(seg.id)}
                        disabled={recalculating === seg.id}
                        className="inline-flex items-center gap-1 px-3 py-1.5 text-xs border rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
                      >
                        <RefreshCw
                          size={12}
                          className={
                            recalculating === seg.id ? "animate-spin" : ""
                          }
                        />
                        Recalculate
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {/* Create Segment Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-lg mx-4 p-6">
            <div className="flex items-center justify-between mb-5">
              <h2 className="text-lg font-bold text-[var(--color-secondary)]">
                New Segment
              </h2>
              <button
                onClick={() => setShowModal(false)}
                className="p-1 hover:bg-gray-100 rounded"
              >
                <X size={20} />
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Name
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, name: e.target.value }))
                  }
                  placeholder="e.g. VIP Customers"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Type
                </label>
                <select
                  value={form.type}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      type: e.target.value as "Manual" | "Dynamic" | "AI",
                    }))
                  }
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
                >
                  <option value="Manual">Manual</option>
                  <option value="Dynamic">Dynamic</option>
                  <option value="AI">AI</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Description
                </label>
                <input
                  type="text"
                  value={form.description}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, description: e.target.value }))
                  }
                  placeholder="Segment description"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
                />
              </div>

              {form.type === "Dynamic" && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Rules (JSON)
                  </label>
                  <textarea
                    value={form.rulesJson}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, rulesJson: e.target.value }))
                    }
                    placeholder='{"minOrderCount": 5, "minTotalSpent": 100000}'
                    rows={4}
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm font-mono focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
                  />
                </div>
              )}
            </div>

            <div className="flex justify-end gap-2 mt-6">
              <button
                onClick={() => setShowModal(false)}
                className="px-4 py-2 border rounded-lg text-sm hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleCreate}
                disabled={submitting || !form.name.trim()}
                className="px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50 transition-colors"
              >
                {submitting ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
