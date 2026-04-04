"use client";

import { useEffect, useState } from "react";
import api from "@/lib/api";
import toast from "react-hot-toast";
import {
  Plus,
  X,
  TrendingUp,
  DollarSign,
  BarChart3,
  Percent,
} from "lucide-react";

interface CostAnalysis {
  id: number;
  productId: number;
  productName?: string;
  analysisPeriod: string;
  materialCost: number;
  laborCost: number;
  overheadCost: number;
  totalCost: number;
  revenue: number;
  grossProfit: number;
  marginPercent: number;
  unitsSold: number;
  costPerUnit: number;
}

interface Product {
  id: number;
  name: string;
}

export default function CostAnalysisPage() {
  const [analyses, setAnalyses] = useState<CostAnalysis[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [form, setForm] = useState({
    productId: "",
    analysisPeriod: new Date().toISOString().slice(0, 7),
    materialCost: "",
    laborCost: "",
    overheadCost: "",
    revenue: "",
    unitsSold: "",
  });

  const fetchAnalyses = async () => {
    try {
      const { data } = await api.get("/accounting/cost-analysis");
      setAnalyses(Array.isArray(data) ? data : data.items ?? []);
    } catch {
      toast.error("Failed to load cost analysis");
    } finally {
      setLoading(false);
    }
  };

  const fetchProducts = async () => {
    try {
      const { data } = await api.get("/product");
      const items = Array.isArray(data) ? data : data.items ?? [];
      setProducts(items.map((p: { id: number; name: string }) => ({ id: p.id, name: p.name })));
    } catch {
      /* silent */
    }
  };

  useEffect(() => {
    fetchAnalyses();
    fetchProducts();
  }, []);

  const handleCreate = async () => {
    if (!form.productId) {
      toast.error("Select a product");
      return;
    }
    setSubmitting(true);
    try {
      await api.post("/accounting/cost-analysis", {
        productId: Number(form.productId),
        analysisPeriod: form.analysisPeriod,
        materialCost: Number(form.materialCost) || 0,
        laborCost: Number(form.laborCost) || 0,
        overheadCost: Number(form.overheadCost) || 0,
        revenue: Number(form.revenue) || 0,
        unitsSold: Number(form.unitsSold) || 0,
      });
      toast.success("Cost analysis created");
      setShowModal(false);
      setForm({ productId: "", analysisPeriod: new Date().toISOString().slice(0, 7), materialCost: "", laborCost: "", overheadCost: "", revenue: "", unitsSold: "" });
      fetchAnalyses();
    } catch {
      toast.error("Failed to create cost analysis");
    } finally {
      setSubmitting(false);
    }
  };

  const totalRevenue = analyses.reduce((s, a) => s + a.revenue, 0);
  const totalCost = analyses.reduce((s, a) => s + a.totalCost, 0);
  const avgMargin = analyses.length > 0 ? analyses.reduce((s, a) => s + a.marginPercent, 0) / analyses.length : 0;

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-blue-600 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Cost Analysis</h1>
          <p className="text-sm text-gray-500 mt-1">Analyze product costs and profitability</p>
        </div>
        <button
          onClick={() => setShowModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          <Plus size={16} />
          New Analysis
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <TrendingUp className="w-5 h-5 text-green-600" />
            </div>
            <span className="text-sm text-gray-500">Total Revenue</span>
          </div>
          <p className="text-2xl font-bold text-green-600">{totalRevenue.toLocaleString()}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-red-50 flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-red-600" />
            </div>
            <span className="text-sm text-gray-500">Total Cost</span>
          </div>
          <p className="text-2xl font-bold text-red-600">{totalCost.toLocaleString()}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <Percent className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">Avg Margin</span>
          </div>
          <p className="text-2xl font-bold text-blue-600">{avgMargin.toFixed(1)}%</p>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-gray-500">Period</th>
                <th className="px-4 py-3 text-left font-medium text-gray-500">Product</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Material</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Labor</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Overhead</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Total Cost</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Revenue</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Gross Profit</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Margin %</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Units Sold</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Cost/Unit</th>
              </tr>
            </thead>
            <tbody>
              {analyses.length === 0 ? (
                <tr>
                  <td colSpan={11} className="px-4 py-8 text-center text-gray-400">No cost analyses found</td>
                </tr>
              ) : (
                analyses.map((a) => (
                  <tr key={a.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 text-gray-600">{a.analysisPeriod}</td>
                    <td className="px-4 py-3 font-medium text-gray-800">{a.productName || `Product #${a.productId}`}</td>
                    <td className="px-4 py-3 text-right text-gray-600">{a.materialCost.toLocaleString()}</td>
                    <td className="px-4 py-3 text-right text-gray-600">{a.laborCost.toLocaleString()}</td>
                    <td className="px-4 py-3 text-right text-gray-600">{a.overheadCost.toLocaleString()}</td>
                    <td className="px-4 py-3 text-right font-medium text-red-600">{a.totalCost.toLocaleString()}</td>
                    <td className="px-4 py-3 text-right font-medium text-green-600">{a.revenue.toLocaleString()}</td>
                    <td className="px-4 py-3 text-right font-medium">
                      <span className={a.grossProfit >= 0 ? "text-green-700" : "text-red-600"}>
                        {a.grossProfit.toLocaleString()}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                        a.marginPercent >= 30
                          ? "bg-green-100 text-green-700"
                          : a.marginPercent >= 10
                            ? "bg-yellow-100 text-yellow-700"
                            : "bg-red-100 text-red-700"
                      }`}>
                        {a.marginPercent.toFixed(1)}%
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right text-gray-600">{a.unitsSold.toLocaleString()}</td>
                    <td className="px-4 py-3 text-right text-gray-600">{a.costPerUnit.toLocaleString()}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Create Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">New Cost Analysis</h3>
              <button onClick={() => setShowModal(false)} className="text-gray-400 hover:text-gray-600">
                <X size={20} />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Product *</label>
                <select
                  value={form.productId}
                  onChange={(e) => setForm({ ...form, productId: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                >
                  <option value="">Select Product</option>
                  {products.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Analysis Period</label>
                <input
                  type="month"
                  value={form.analysisPeriod}
                  onChange={(e) => setForm({ ...form, analysisPeriod: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Material Cost</label>
                  <input
                    type="number"
                    value={form.materialCost}
                    onChange={(e) => setForm({ ...form, materialCost: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    min="0"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Labor Cost</label>
                  <input
                    type="number"
                    value={form.laborCost}
                    onChange={(e) => setForm({ ...form, laborCost: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    min="0"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Overhead Cost</label>
                  <input
                    type="number"
                    value={form.overheadCost}
                    onChange={(e) => setForm({ ...form, overheadCost: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    min="0"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Revenue</label>
                  <input
                    type="number"
                    value={form.revenue}
                    onChange={(e) => setForm({ ...form, revenue: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    min="0"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Units Sold</label>
                  <input
                    type="number"
                    value={form.unitsSold}
                    onChange={(e) => setForm({ ...form, unitsSold: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                    min="0"
                  />
                </div>
              </div>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200">
              <button
                onClick={() => setShowModal(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleCreate}
                disabled={submitting}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
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
