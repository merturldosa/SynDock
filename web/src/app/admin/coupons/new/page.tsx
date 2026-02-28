"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import Link from "next/link";
import { createCoupon } from "@/lib/couponApi";

export default function CreateCouponPage() {
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const [form, setForm] = useState({
    code: "",
    name: "",
    description: "",
    discountType: "Fixed",
    discountValue: 0,
    minOrderAmount: 0,
    maxDiscountAmount: 0,
    startDate: new Date().toISOString().slice(0, 10),
    endDate: "",
    maxUsageCount: 0,
  });

  const update = (field: string, value: string | number) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.code.trim() || !form.name.trim() || !form.endDate) return;

    setSubmitting(true);
    setError("");

    try {
      await createCoupon({
        code: form.code.toUpperCase().trim(),
        name: form.name.trim(),
        description: form.description || undefined,
        discountType: form.discountType,
        discountValue: form.discountValue,
        minOrderAmount: form.minOrderAmount,
        maxDiscountAmount: form.maxDiscountAmount || undefined,
        startDate: new Date(form.startDate).toISOString(),
        endDate: new Date(form.endDate).toISOString(),
        maxUsageCount: form.maxUsageCount,
      });
      router.push("/admin/coupons");
    } catch {
      setError("쿠폰 생성에 실패했습니다.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl">
      <Link
        href="/admin/coupons"
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ChevronLeft size={16} /> 쿠폰 목록
      </Link>

      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        쿠폰 생성
      </h1>

      {error && (
        <div className="p-3 mb-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="bg-white rounded-xl shadow-sm p-5 space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                쿠폰 코드
              </label>
              <input
                type="text"
                value={form.code}
                onChange={(e) => update("code", e.target.value)}
                placeholder="예: WELCOME2026"
                className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                쿠폰명
              </label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => update("name", e.target.value)}
                placeholder="예: 신규 가입 쿠폰"
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                required
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              설명
            </label>
            <input
              type="text"
              value={form.description}
              onChange={(e) => update("description", e.target.value)}
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                할인 유형
              </label>
              <select
                value={form.discountType}
                onChange={(e) => update("discountType", e.target.value)}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
                <option value="Fixed">정액 할인 (원)</option>
                <option value="Percentage">정률 할인 (%)</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                할인 값
              </label>
              <input
                type="number"
                value={form.discountValue}
                onChange={(e) => update("discountValue", Number(e.target.value))}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                min={0}
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                최소 주문 금액
              </label>
              <input
                type="number"
                value={form.minOrderAmount}
                onChange={(e) =>
                  update("minOrderAmount", Number(e.target.value))
                }
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                min={0}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                최대 할인 금액 (% 할인 시)
              </label>
              <input
                type="number"
                value={form.maxDiscountAmount}
                onChange={(e) =>
                  update("maxDiscountAmount", Number(e.target.value))
                }
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                min={0}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                시작일
              </label>
              <input
                type="date"
                value={form.startDate}
                onChange={(e) => update("startDate", e.target.value)}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                종료일
              </label>
              <input
                type="date"
                value={form.endDate}
                onChange={(e) => update("endDate", e.target.value)}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                required
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              최대 사용 횟수 (0 = 무제한)
            </label>
            <input
              type="number"
              value={form.maxUsageCount}
              onChange={(e) => update("maxUsageCount", Number(e.target.value))}
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
              min={0}
            />
          </div>
        </div>

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={submitting}
            className="px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50"
          >
            {submitting ? "생성 중..." : "쿠폰 생성"}
          </button>
          <button
            type="button"
            onClick={() => router.back()}
            className="px-6 py-2.5 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
          >
            취소
          </button>
        </div>
      </form>
    </div>
  );
}
