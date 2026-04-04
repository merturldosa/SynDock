"use client";

import { useEffect, useState } from "react";
import api from "@/lib/api";
import toast from "react-hot-toast";
import {
  X,
  Plus,
  CheckCircle,
  Banknote,
  DollarSign,
  Calculator,
} from "lucide-react";

interface PayrollRecord {
  id: number;
  employeeId: number;
  employeeName?: string;
  employeeNumber?: string;
  payPeriod: string;
  basePay: number;
  overtimePay: number;
  bonus: number;
  deductions: number;
  tax: number;
  insurance: number;
  netPay: number;
  status: string;
}

const STATUS_COLORS: Record<string, string> = {
  Draft: "bg-yellow-100 text-yellow-700",
  Approved: "bg-blue-100 text-blue-700",
  Paid: "bg-green-100 text-green-700",
};

export default function PayrollPage() {
  const [records, setRecords] = useState<PayrollRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [showGenModal, setShowGenModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [payPeriod, setPayPeriod] = useState(new Date().toISOString().slice(0, 7));

  const fetchPayroll = async () => {
    try {
      const { data } = await api.get("/hr/payroll");
      setRecords(Array.isArray(data) ? data : data.items ?? []);
    } catch {
      toast.error("Failed to load payroll");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPayroll();
  }, []);

  const handleGenerate = async () => {
    setSubmitting(true);
    try {
      await api.post("/hr/payroll/generate", { payPeriod });
      toast.success("Payroll generated");
      setShowGenModal(false);
      fetchPayroll();
    } catch {
      toast.error("Failed to generate payroll");
    } finally {
      setSubmitting(false);
    }
  };

  const handleApprove = async (id: number) => {
    try {
      await api.put(`/hr/payroll/${id}/approve`);
      toast.success("Payroll approved");
      fetchPayroll();
    } catch {
      toast.error("Failed to approve");
    }
  };

  const handlePay = async (id: number) => {
    try {
      await api.put(`/hr/payroll/${id}/pay`);
      toast.success("Payment processed");
      fetchPayroll();
    } catch {
      toast.error("Failed to process payment");
    }
  };

  const totals = records.reduce(
    (acc, r) => ({
      basePay: acc.basePay + r.basePay,
      overtimePay: acc.overtimePay + r.overtimePay,
      bonus: acc.bonus + r.bonus,
      deductions: acc.deductions + r.deductions,
      tax: acc.tax + r.tax,
      insurance: acc.insurance + r.insurance,
      netPay: acc.netPay + r.netPay,
    }),
    { basePay: 0, overtimePay: 0, bonus: 0, deductions: 0, tax: 0, insurance: 0, netPay: 0 }
  );

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
          <h1 className="text-2xl font-bold text-gray-900">Payroll</h1>
          <p className="text-sm text-gray-500 mt-1">Generate and manage employee payroll</p>
        </div>
        <button
          onClick={() => setShowGenModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
        >
          <Plus size={16} />
          Generate Payroll
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <Calculator className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">Total Base Pay</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{totals.basePay.toLocaleString()}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-red-50 flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-red-600" />
            </div>
            <span className="text-sm text-gray-500">Total Deductions</span>
          </div>
          <p className="text-2xl font-bold text-red-600">{(totals.deductions + totals.tax + totals.insurance).toLocaleString()}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <Banknote className="w-5 h-5 text-green-600" />
            </div>
            <span className="text-sm text-gray-500">Total Net Pay</span>
          </div>
          <p className="text-2xl font-bold text-green-600">{totals.netPay.toLocaleString()}</p>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-gray-500">Employee</th>
                <th className="px-4 py-3 text-left font-medium text-gray-500">Period</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Base Pay</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Overtime</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Bonus</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Deductions</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Tax</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Insurance</th>
                <th className="px-4 py-3 text-right font-medium text-gray-500">Net Pay</th>
                <th className="px-4 py-3 text-center font-medium text-gray-500">Status</th>
                <th className="px-4 py-3 text-center font-medium text-gray-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              {records.length === 0 ? (
                <tr>
                  <td colSpan={11} className="px-4 py-8 text-center text-gray-400">No payroll records</td>
                </tr>
              ) : (
                records.map((rec) => (
                  <tr key={rec.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <div className="font-medium text-gray-800">{rec.employeeName || `#${rec.employeeId}`}</div>
                      {rec.employeeNumber && <div className="text-xs text-gray-400">{rec.employeeNumber}</div>}
                    </td>
                    <td className="px-4 py-3 text-gray-600">{rec.payPeriod}</td>
                    <td className="px-4 py-3 text-right">{rec.basePay.toLocaleString()}</td>
                    <td className="px-4 py-3 text-right text-orange-600">{rec.overtimePay > 0 ? rec.overtimePay.toLocaleString() : "-"}</td>
                    <td className="px-4 py-3 text-right text-blue-600">{rec.bonus > 0 ? rec.bonus.toLocaleString() : "-"}</td>
                    <td className="px-4 py-3 text-right text-red-500">{rec.deductions > 0 ? `-${rec.deductions.toLocaleString()}` : "-"}</td>
                    <td className="px-4 py-3 text-right text-red-500">{rec.tax > 0 ? `-${rec.tax.toLocaleString()}` : "-"}</td>
                    <td className="px-4 py-3 text-right text-red-500">{rec.insurance > 0 ? `-${rec.insurance.toLocaleString()}` : "-"}</td>
                    <td className="px-4 py-3 text-right font-bold text-gray-900">{rec.netPay.toLocaleString()}</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[rec.status] || "bg-gray-100 text-gray-600"}`}>
                        {rec.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <div className="flex items-center justify-center gap-1">
                        {rec.status === "Draft" && (
                          <button
                            onClick={() => handleApprove(rec.id)}
                            className="px-2 py-1 text-xs font-medium text-blue-600 bg-blue-50 rounded-lg hover:bg-blue-100 transition-colors"
                          >
                            Approve
                          </button>
                        )}
                        {rec.status === "Approved" && (
                          <button
                            onClick={() => handlePay(rec.id)}
                            className="px-2 py-1 text-xs font-medium text-green-600 bg-green-50 rounded-lg hover:bg-green-100 transition-colors"
                          >
                            Pay
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
            {records.length > 0 && (
              <tfoot className="bg-gray-50 border-t-2 border-gray-200">
                <tr>
                  <td className="px-4 py-3 font-bold text-gray-700" colSpan={2}>Totals</td>
                  <td className="px-4 py-3 text-right font-bold">{totals.basePay.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right font-bold text-orange-600">{totals.overtimePay.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right font-bold text-blue-600">{totals.bonus.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right font-bold text-red-500">-{totals.deductions.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right font-bold text-red-500">-{totals.tax.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right font-bold text-red-500">-{totals.insurance.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right font-bold text-green-700 text-base">{totals.netPay.toLocaleString()}</td>
                  <td colSpan={2} />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      </div>

      {/* Generate Modal */}
      {showGenModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-sm mx-4">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Generate Payroll</h3>
              <button onClick={() => setShowGenModal(false)} className="text-gray-400 hover:text-gray-600">
                <X size={20} />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Pay Period</label>
                <input
                  type="month"
                  value={payPeriod}
                  onChange={(e) => setPayPeriod(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                />
              </div>
              <p className="text-xs text-gray-500">
                This will generate payroll records for all active employees for the selected period based on their attendance and salary configuration.
              </p>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200">
              <button
                onClick={() => setShowGenModal(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleGenerate}
                disabled={submitting}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
              >
                {submitting ? "Generating..." : "Generate"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
