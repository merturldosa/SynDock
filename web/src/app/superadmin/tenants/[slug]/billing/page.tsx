"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import {
  getTenantBilling,
  updateTenantBilling,
  type TenantBilling,
} from "@/lib/platformApi";

const PLANS = [
  { type: "Free", price: 0, label: "무료" },
  { type: "Basic", price: 29000, label: "베이직 (29,000원/월)" },
  { type: "Pro", price: 79000, label: "프로 (79,000원/월)" },
  { type: "Enterprise", price: 199000, label: "엔터프라이즈 (199,000원/월)" },
];

const STATUSES = ["Active", "Trial", "Suspended", "Cancelled"];

export default function TenantBillingPage() {
  const params = useParams();
  const router = useRouter();
  const slug = params.slug as string;

  const [billing, setBilling] = useState<TenantBilling | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [selectedPlan, setSelectedPlan] = useState("Free");
  const [selectedStatus, setSelectedStatus] = useState("Trial");

  useEffect(() => {
    getTenantBilling(slug)
      .then((b) => {
        setBilling(b);
        setSelectedPlan(b.planType);
        setSelectedStatus(b.billingStatus);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [slug]);

  const handleSave = async () => {
    const plan = PLANS.find((p) => p.type === selectedPlan);
    setSaving(true);
    try {
      await updateTenantBilling(slug, {
        planType: selectedPlan,
        monthlyPrice: plan?.price ?? 0,
        billingStatus: selectedStatus,
      });
      alert("빌링 정보가 업데이트되었습니다.");
      router.push("/superadmin/billing");
    } catch {
      alert("업데이트에 실패했습니다.");
    }
    setSaving(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="max-w-2xl">
      <button
        onClick={() => router.back()}
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ChevronLeft size={16} /> 돌아가기
      </button>

      <h1 className="text-2xl font-bold text-gray-900 mb-6">
        {billing?.tenantName || slug} 빌링 관리
      </h1>

      {/* Current status */}
      {billing && (
        <div className="bg-white rounded-xl shadow-sm p-6 mb-6">
          <h2 className="font-semibold text-gray-900 mb-4">현재 상태</h2>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-gray-400">플랜</p>
              <p className="font-medium">{billing.planType}</p>
            </div>
            <div>
              <p className="text-gray-400">상태</p>
              <p className="font-medium">{billing.billingStatus}</p>
            </div>
            <div>
              <p className="text-gray-400">월 요금</p>
              <p className="font-medium">
                {billing.monthlyPrice > 0
                  ? `${billing.monthlyPrice.toLocaleString()}원`
                  : "무료"}
              </p>
            </div>
            <div>
              <p className="text-gray-400">다음 결제일</p>
              <p className="font-medium">
                {billing.nextBillingAt
                  ? new Date(billing.nextBillingAt).toLocaleDateString("ko-KR")
                  : "-"}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Edit */}
      <div className="bg-white rounded-xl shadow-sm p-6 space-y-4">
        <h2 className="font-semibold text-gray-900 mb-4">변경</h2>

        <div>
          <label className="block text-sm text-gray-500 mb-1">플랜</label>
          <select
            value={selectedPlan}
            onChange={(e) => setSelectedPlan(e.target.value)}
            className="w-full px-3 py-2.5 border rounded-lg text-sm"
          >
            {PLANS.map((p) => (
              <option key={p.type} value={p.type}>
                {p.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm text-gray-500 mb-1">상태</label>
          <select
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(e.target.value)}
            className="w-full px-3 py-2.5 border rounded-lg text-sm"
          >
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>

        <div className="flex gap-3 pt-4">
          <button
            onClick={() => router.back()}
            className="flex-1 py-3 border rounded-lg text-sm font-medium text-gray-500 hover:bg-gray-50"
          >
            취소
          </button>
          <button
            onClick={handleSave}
            disabled={saving}
            className="flex-1 py-3 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700 disabled:opacity-60"
          >
            {saving ? "저장 중..." : "저장"}
          </button>
        </div>
      </div>
    </div>
  );
}
