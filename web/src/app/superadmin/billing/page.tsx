"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getAllBilling, type TenantBilling } from "@/lib/platformApi";

const STATUS_COLORS: Record<string, string> = {
  Active: "bg-emerald-100 text-emerald-700",
  Trial: "bg-blue-100 text-blue-700",
  Suspended: "bg-red-100 text-red-700",
  Cancelled: "bg-gray-100 text-gray-700",
  None: "bg-gray-100 text-gray-400",
};

const PLAN_LABELS: Record<string, string> = {
  Free: "무료",
  Basic: "베이직",
  Pro: "프로",
  Enterprise: "엔터프라이즈",
};

export default function BillingOverviewPage() {
  const [billings, setBillings] = useState<TenantBilling[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState("all");

  useEffect(() => {
    getAllBilling()
      .then(setBillings)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const filtered =
    filter === "all"
      ? billings
      : billings.filter((b) => b.billingStatus === filter);

  const totalRevenue = billings
    .filter((b) => b.billingStatus === "Active")
    .reduce((sum, b) => sum + b.monthlyPrice, 0);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">빌링 관리</h1>

      {/* Summary cards */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        <div className="bg-white rounded-xl p-4 shadow-sm">
          <p className="text-xs text-gray-400">전체 테넌트</p>
          <p className="text-2xl font-bold text-gray-900">{billings.length}</p>
        </div>
        <div className="bg-white rounded-xl p-4 shadow-sm">
          <p className="text-xs text-gray-400">활성 구독</p>
          <p className="text-2xl font-bold text-emerald-600">
            {billings.filter((b) => b.billingStatus === "Active").length}
          </p>
        </div>
        <div className="bg-white rounded-xl p-4 shadow-sm">
          <p className="text-xs text-gray-400">체험 중</p>
          <p className="text-2xl font-bold text-blue-600">
            {billings.filter((b) => b.billingStatus === "Trial").length}
          </p>
        </div>
        <div className="bg-white rounded-xl p-4 shadow-sm">
          <p className="text-xs text-gray-400">월 수익</p>
          <p className="text-2xl font-bold text-gray-900">
            {totalRevenue.toLocaleString()}원
          </p>
        </div>
      </div>

      {/* Filter */}
      <div className="flex gap-2 mb-4">
        {["all", "Active", "Trial", "Suspended"].map((status) => (
          <button
            key={status}
            onClick={() => setFilter(status)}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium ${
              filter === status
                ? "bg-gray-900 text-white"
                : "bg-gray-100 text-gray-600 hover:bg-gray-200"
            }`}
          >
            {status === "all" ? "전체" : status}
          </button>
        ))}
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 text-gray-500 text-xs">
            <tr>
              <th className="text-left px-4 py-3 font-medium">테넌트</th>
              <th className="text-left px-4 py-3 font-medium">플랜</th>
              <th className="text-left px-4 py-3 font-medium">상태</th>
              <th className="text-right px-4 py-3 font-medium">월 요금</th>
              <th className="text-left px-4 py-3 font-medium">다음 결제</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {filtered.map((b) => (
              <tr key={b.tenantId} className="hover:bg-gray-50">
                <td className="px-4 py-3 font-medium text-gray-900">
                  {b.tenantName}
                  <span className="text-xs text-gray-400 ml-2">
                    ({b.tenantSlug})
                  </span>
                </td>
                <td className="px-4 py-3">
                  {PLAN_LABELS[b.planType] || b.planType}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                      STATUS_COLORS[b.billingStatus] || STATUS_COLORS.None
                    }`}
                  >
                    {b.billingStatus}
                  </span>
                </td>
                <td className="px-4 py-3 text-right">
                  {b.monthlyPrice > 0
                    ? `${b.monthlyPrice.toLocaleString()}원`
                    : "-"}
                </td>
                <td className="px-4 py-3 text-gray-500">
                  {b.nextBillingAt
                    ? new Date(b.nextBillingAt).toLocaleDateString("ko-KR")
                    : "-"}
                </td>
                <td className="px-4 py-3 text-right">
                  <Link
                    href={`/superadmin/tenants/${b.tenantSlug}/billing`}
                    className="text-xs text-emerald-600 hover:underline"
                  >
                    관리
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
