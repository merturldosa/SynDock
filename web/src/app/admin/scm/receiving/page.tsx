"use client";
import { useEffect, useState } from "react";
import { BarChart3, Users, ClipboardList, AlertTriangle, Clock, Star, Plus } from "lucide-react";
import api from "@/lib/api";

interface DashboardStats { activeSuppliers: number; openPOs: number; overduePOs: number; monthlyProcurement: number; }
interface LeadTimeEntry { supplierId: number; supplierName: string; supplierCode: string; avgLeadTimeDays: number; minLeadTimeDays: number; maxLeadTimeDays: number; onTimeRate: number; orderCount: number; }
interface Evaluation { id: number; supplierId: number; supplierName: string; period: string; qualityScore: number; deliveryScore: number; priceScore: number; serviceScore: number; overallScore: number; evaluatedAt: string; }
interface Supplier { id: number; name: string; code: string; }

export default function ScmReceivingPage() {
  const [stats, setStats] = useState<DashboardStats>({ activeSuppliers: 0, openPOs: 0, overduePOs: 0, monthlyProcurement: 0 });
  const [leadTimes, setLeadTimes] = useState<LeadTimeEntry[]>([]);
  const [evaluations, setEvaluations] = useState<Evaluation[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(true);
  const [showEvalCreate, setShowEvalCreate] = useState(false);
  const [evalForm, setEvalForm] = useState({ supplierId: 0, period: "", qualityScore: 80, deliveryScore: 80, priceScore: 80, serviceScore: 80 });

  useEffect(() => {
    setLoading(true);
    Promise.all([
      api.get("/scm/dashboard").catch(() => ({ data: { activeSuppliers: 0, openPOs: 0, overduePOs: 0, monthlyProcurement: 0 } })),
      api.get("/scm/lead-time-analysis").catch(() => ({ data: [] })),
      api.get("/scm/evaluations").catch(() => ({ data: [] })),
      api.get("/scm/suppliers").catch(() => ({ data: [] })),
    ]).then(([dashRes, ltRes, evalRes, supRes]) => {
      setStats(dashRes.data);
      setLeadTimes(ltRes.data);
      setEvaluations(evalRes.data);
      setSuppliers(supRes.data);
    }).finally(() => setLoading(false));
  }, []);

  const createEvaluation = async () => {
    if (!evalForm.supplierId || !evalForm.period) return;
    await api.post("/scm/evaluations", evalForm);
    setShowEvalCreate(false);
    setEvalForm({ supplierId: 0, period: "", qualityScore: 80, deliveryScore: 80, priceScore: 80, serviceScore: 80 });
    const r = await api.get("/scm/evaluations");
    setEvaluations(r.data);
  };

  const statCards = [
    { label: "Active Suppliers", value: stats.activeSuppliers, icon: Users, color: "text-blue-600", bg: "bg-blue-50" },
    { label: "Open POs", value: stats.openPOs, icon: ClipboardList, color: "text-indigo-600", bg: "bg-indigo-50" },
    { label: "Overdue POs", value: stats.overduePOs, icon: AlertTriangle, color: "text-red-600", bg: "bg-red-50" },
    { label: "Monthly Procurement", value: `${(stats.monthlyProcurement / 10000).toFixed(0)}만원`, icon: BarChart3, color: "text-green-600", bg: "bg-green-50" },
  ];

  if (loading) return <div className="p-12 text-center text-gray-400">Loading SCM Dashboard...</div>;

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><BarChart3 size={24} /> SCM Dashboard</h1>
        <p className="text-gray-500 text-sm mt-1">Supply chain overview and receiving management</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {statCards.map(card => (
          <div key={card.label} className="bg-white rounded-xl border p-5">
            <div className="flex items-center justify-between mb-3">
              <span className="text-sm text-gray-500">{card.label}</span>
              <div className={`w-10 h-10 ${card.bg} rounded-lg flex items-center justify-center`}>
                <card.icon size={20} className={card.color} />
              </div>
            </div>
            <p className={`text-2xl font-bold ${card.label === "Overdue POs" && stats.overduePOs > 0 ? "text-red-600" : "text-gray-900"}`}>
              {card.value}
            </p>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-xl border mb-8">
        <div className="p-5 border-b">
          <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2"><Clock size={18} /> Lead Time Analysis</h2>
        </div>
        {leadTimes.length === 0 ? <div className="p-8 text-center text-gray-400">No lead time data available</div> : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr className="text-gray-500 text-xs uppercase">
                <th className="text-left px-4 py-3">Supplier</th>
                <th className="text-right px-4 py-3">Avg Lead Time</th>
                <th className="text-right px-4 py-3">Min</th>
                <th className="text-right px-4 py-3">Max</th>
                <th className="text-right px-4 py-3">On-Time Rate</th>
                <th className="text-right px-4 py-3">Orders</th>
                <th className="px-4 py-3">Performance</th>
              </tr>
            </thead>
            <tbody>
              {leadTimes.map(lt => {
                const barWidth = Math.min(100, lt.onTimeRate * 100);
                const barColor = lt.onTimeRate >= 0.9 ? "bg-green-500" : lt.onTimeRate >= 0.7 ? "bg-yellow-500" : "bg-red-500";
                return (
                  <tr key={lt.supplierId} className="border-t hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <p className="font-medium">{lt.supplierName}</p>
                      <p className="text-xs text-gray-400 font-mono">{lt.supplierCode}</p>
                    </td>
                    <td className="px-4 py-3 text-right font-medium">{lt.avgLeadTimeDays.toFixed(1)}d</td>
                    <td className="px-4 py-3 text-right text-gray-500">{lt.minLeadTimeDays}d</td>
                    <td className="px-4 py-3 text-right text-gray-500">{lt.maxLeadTimeDays}d</td>
                    <td className="px-4 py-3 text-right font-medium">{(lt.onTimeRate * 100).toFixed(1)}%</td>
                    <td className="px-4 py-3 text-right text-gray-500">{lt.orderCount}</td>
                    <td className="px-4 py-3">
                      <div className="w-full bg-gray-200 rounded-full h-2">
                        <div className={`${barColor} h-2 rounded-full`} style={{ width: `${barWidth}%` }} />
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      <div className="bg-white rounded-xl border">
        <div className="p-5 border-b flex justify-between items-center">
          <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2"><Star size={18} /> Supplier Evaluations</h2>
          <button onClick={() => setShowEvalCreate(true)} className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"><Plus size={14} /> New Evaluation</button>
        </div>
        {evaluations.length === 0 ? <div className="p-8 text-center text-gray-400">No evaluations yet</div> : (
          <div className="divide-y">
            {evaluations.map(ev => {
              const avg = (ev.qualityScore + ev.deliveryScore + ev.priceScore + ev.serviceScore) / 4;
              const scoreColor = avg >= 90 ? "text-green-600" : avg >= 70 ? "text-blue-600" : avg >= 50 ? "text-yellow-600" : "text-red-600";
              return (
                <div key={ev.id} className="p-4 hover:bg-gray-50">
                  <div className="flex items-center justify-between mb-2">
                    <div>
                      <span className="font-medium text-gray-900">{ev.supplierName}</span>
                      <span className="text-gray-400 mx-2">|</span>
                      <span className="text-sm text-gray-500">{ev.period}</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className={`text-lg font-bold ${scoreColor}`}>{ev.overallScore?.toFixed(1) || avg.toFixed(1)}</span>
                      <span className="text-xs text-gray-400">{new Date(ev.evaluatedAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                  <div className="grid grid-cols-4 gap-4 text-xs">
                    {[
                      { label: "Quality", score: ev.qualityScore },
                      { label: "Delivery", score: ev.deliveryScore },
                      { label: "Price", score: ev.priceScore },
                      { label: "Service", score: ev.serviceScore },
                    ].map(item => (
                      <div key={item.label}>
                        <div className="flex justify-between text-gray-500 mb-1">
                          <span>{item.label}</span>
                          <span className="font-medium text-gray-700">{item.score}</span>
                        </div>
                        <div className="w-full bg-gray-200 rounded-full h-1.5">
                          <div className={`h-1.5 rounded-full ${item.score >= 90 ? "bg-green-500" : item.score >= 70 ? "bg-blue-500" : item.score >= 50 ? "bg-yellow-500" : "bg-red-500"}`} style={{ width: `${item.score}%` }} />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {showEvalCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl p-6 w-full max-w-lg">
            <h3 className="text-lg font-bold mb-4">New Supplier Evaluation</h3>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Supplier</label>
                  <select value={evalForm.supplierId} onChange={e => setEvalForm({ ...evalForm, supplierId: parseInt(e.target.value) })} className="w-full px-3 py-2 border rounded-lg text-sm">
                    <option value={0}>Select supplier...</option>
                    {suppliers.map(s => <option key={s.id} value={s.id}>{s.name} ({s.code})</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Period</label>
                  <input value={evalForm.period} onChange={e => setEvalForm({ ...evalForm, period: e.target.value })} placeholder="e.g. 2026-Q1" className="w-full px-3 py-2 border rounded-lg text-sm" />
                </div>
              </div>
              {(["qualityScore", "deliveryScore", "priceScore", "serviceScore"] as const).map(field => (
                <div key={field}>
                  <div className="flex justify-between text-xs mb-1">
                    <label className="text-gray-500 capitalize">{field.replace("Score", "")} Score</label>
                    <span className="font-medium text-gray-700">{evalForm[field]}</span>
                  </div>
                  <input type="range" min={0} max={100} value={evalForm[field]} onChange={e => setEvalForm({ ...evalForm, [field]: parseInt(e.target.value) })} className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-blue-600" />
                </div>
              ))}
            </div>
            <div className="flex gap-2 mt-5 justify-end">
              <button onClick={() => setShowEvalCreate(false)} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm">Cancel</button>
              <button onClick={createEvaluation} disabled={!evalForm.supplierId || !evalForm.period} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50">Submit Evaluation</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
