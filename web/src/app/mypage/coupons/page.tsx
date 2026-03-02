"use client";

import { useEffect, useState } from "react";
import { Ticket } from "lucide-react";
import { useTranslations } from "next-intl";
import { getMyCoupons, type UserCouponDto } from "@/lib/couponApi";

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

export default function MyCouponsPage() {
  const t = useTranslations();
  const [coupons, setCoupons] = useState<UserCouponDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyCoupons()
      .then(setCoupons)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-8">
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("mypage.coupons.title")}
      </h1>

      {coupons.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <Ticket size={48} className="mx-auto mb-3 opacity-30" />
          <p>{t("mypage.coupons.empty")}</p>
        </div>
      ) : (
        <div className="space-y-3">
          {coupons.map((coupon) => {
            const daysLeft = Math.ceil(
              (new Date(coupon.endDate).getTime() - Date.now()) / 86400000
            );
            return (
              <div
                key={coupon.id}
                className="bg-white rounded-xl shadow-sm overflow-hidden flex"
              >
                {/* Left accent */}
                <div className="w-24 bg-[var(--color-primary)] flex flex-col items-center justify-center text-white p-3 flex-shrink-0">
                  <p className="text-xl font-bold">
                    {coupon.discountType === "Percentage"
                      ? `${coupon.discountValue}%`
                      : formatPrice(coupon.discountValue).replace("원", "")}
                  </p>
                  <p className="text-xs">
                    {coupon.discountType === "Percentage" ? t("mypage.coupons.discount") : t("mypage.coupons.wonDiscount")}
                  </p>
                </div>

                {/* Content */}
                <div className="flex-1 p-4">
                  <p className="font-medium text-[var(--color-secondary)]">
                    {coupon.name}
                  </p>
                  {coupon.description && (
                    <p className="text-xs text-gray-400 mt-0.5">
                      {coupon.description}
                    </p>
                  )}
                  <div className="flex items-center gap-3 mt-2 text-xs text-gray-400">
                    <span>{t("mypage.coupons.code")}: {coupon.code}</span>
                    {coupon.minOrderAmount > 0 && (
                      <span>
                        {t("mypage.coupons.minOrder", { amount: formatPrice(coupon.minOrderAmount) })}
                      </span>
                    )}
                  </div>
                  <p
                    className={`text-xs mt-1 ${
                      daysLeft <= 3 ? "text-red-500 font-medium" : "text-gray-400"
                    }`}
                  >
                    {daysLeft > 0
                      ? t("mypage.coupons.daysLeft", { days: daysLeft, date: new Date(coupon.endDate).toLocaleDateString("ko-KR") })
                      : t("mypage.points.expired")}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
