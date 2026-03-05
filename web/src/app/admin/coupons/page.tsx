"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Plus, Trash2, Send } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  getCoupons,
  deleteCoupon,
  issueCoupon,
  type CouponDto,
  type PagedCoupons,
} from "@/lib/couponApi";
import { formatPrice } from "@/lib/format";

export default function AdminCouponsPage() {
  const t = useTranslations();
  const [data, setData] = useState<PagedCoupons | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    getCoupons(page)
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [page]);

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(t("admin.coupons.deleteConfirm", { name }))) return;
    try {
      await deleteCoupon(id);
      load();
    } catch {
      alert(t("admin.coupons.deleteFailed"));
    }
  };

  const handleIssueAll = async (id: number, name: string) => {
    if (!confirm(t("admin.coupons.issueConfirm", { name }))) return;
    try {
      const result = await issueCoupon(id);
      alert(t("admin.coupons.issuedCount", { count: result.issuedCount }));
    } catch {
      alert(t("admin.coupons.issueFailed"));
    }
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          {t("admin.coupons.title")}
        </h1>
        <Link
          href="/admin/coupons/new"
          className="flex items-center gap-2 px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90"
        >
          <Plus size={16} /> {t("admin.coupons.addNew")}
        </Link>
      </div>

      {data && (
        <p className="text-sm text-gray-500 mb-3">
          {t("admin.coupons.totalCount", { count: data.totalCount })}
        </p>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>{t("admin.coupons.noCoupons")}</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.coupons.name")}</th>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.coupons.code")}</th>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.coupons.discount")}</th>
                <th className="text-center p-3 font-medium text-gray-500">{t("admin.coupons.usageSlash")}</th>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.coupons.period")}</th>
                <th className="text-center p-3 font-medium text-gray-500">{t("admin.coupons.statusLabel")}</th>
                <th className="text-center p-3 font-medium text-gray-500">{t("admin.coupons.manage")}</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((coupon) => {
                const now = new Date();
                const expired = new Date(coupon.endDate) < now;
                return (
                  <tr key={coupon.id} className="border-b last:border-0 hover:bg-gray-50">
                    <td className="p-3 font-medium text-[var(--color-secondary)]">
                      {coupon.name}
                    </td>
                    <td className="p-3 font-mono text-xs text-gray-500">{coupon.code}</td>
                    <td className="p-3">
                      {coupon.discountType === "Percentage"
                        ? `${coupon.discountValue}%`
                        : formatPrice(coupon.discountValue)}
                    </td>
                    <td className="p-3 text-center text-gray-500">
                      {coupon.currentUsageCount} / {coupon.maxUsageCount || t("admin.coupons.unlimited")}
                    </td>
                    <td className="p-3 text-xs text-gray-500">
                      {new Date(coupon.startDate).toLocaleDateString("ko-KR")} ~{" "}
                      {new Date(coupon.endDate).toLocaleDateString("ko-KR")}
                    </td>
                    <td className="p-3 text-center">
                      <span
                        className={`px-2 py-0.5 text-xs rounded-full ${
                          expired
                            ? "bg-gray-100 text-gray-500"
                            : coupon.isActive
                            ? "bg-emerald-100 text-emerald-700"
                            : "bg-red-100 text-red-700"
                        }`}
                      >
                        {expired ? t("admin.coupons.expired") : coupon.isActive ? t("admin.coupons.active") : t("admin.coupons.inactive")}
                      </span>
                    </td>
                    <td className="p-3">
                      <div className="flex items-center justify-center gap-2">
                        <button
                          onClick={() => handleIssueAll(coupon.id, coupon.name)}
                          className="p-1.5 text-gray-400 hover:text-blue-500 transition-colors"
                          title={t("admin.coupons.issueAll")}
                        >
                          <Send size={16} />
                        </button>
                        <button
                          onClick={() => handleDelete(coupon.id, coupon.name)}
                          className="p-1.5 text-gray-400 hover:text-red-500 transition-colors"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
