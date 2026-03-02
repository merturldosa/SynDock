"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { createCoupon } from "@/lib/couponApi";

export default function CreateCouponPage() {
  const t = useTranslations();
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
      setError(t("admin.coupons.createFailed"));
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
        <ChevronLeft size={16} /> {t("admin.coupons.couponList")}
      </Link>

      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("admin.coupons.addNew")}
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
                {t("admin.coupons.code")}
              </label>
              <input
                type="text"
                value={form.code}
                onChange={(e) => update("code", e.target.value)}
                placeholder={t("admin.coupons.codePlaceholder")}
                className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {t("admin.coupons.name")}
              </label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => update("name", e.target.value)}
                placeholder={t("admin.coupons.namePlaceholder")}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
                required
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              {t("admin.coupons.description")}
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
                {t("admin.coupons.discountType")}
              </label>
              <select
                value={form.discountType}
                onChange={(e) => update("discountType", e.target.value)}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
                <option value="Fixed">{t("admin.coupons.fixedDiscount")}</option>
                <option value="Percentage">{t("admin.coupons.percentDiscount")}</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {t("admin.coupons.discountValue")}
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
                {t("admin.coupons.minOrderAmount")}
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
                {t("admin.coupons.maxDiscountAmount")}
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
                {t("admin.coupons.startDate")}
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
                {t("admin.coupons.endDate")}
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
              {t("admin.coupons.maxUsesZeroUnlimited")}
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
            {submitting ? t("admin.coupons.creating") : t("admin.coupons.addNew")}
          </button>
          <button
            type="button"
            onClick={() => router.back()}
            className="px-6 py-2.5 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
          >
            {t("common.cancel")}
          </button>
        </div>
      </form>
    </div>
  );
}
