"use client";

import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { Suspense } from "react";
import { CheckCircle } from "lucide-react";
import { CheckoutSteps } from "@/components/checkout/CheckoutSteps";

function OrderCompleteContent() {
  const searchParams = useSearchParams();
  const t = useTranslations();
  const orderId = searchParams.get("id");

  return (
    <div className="max-w-lg mx-auto px-4 py-12 text-center">
      <CheckoutSteps currentStep="complete" />
      <CheckCircle size={72} className="mx-auto text-emerald-500 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">{t("order.complete.title")}</h1>
      <p className="text-gray-500 mb-2">{t("order.complete.thanks")}</p>
      {orderId && (
        <p className="text-sm text-gray-400 mb-8">{t("order.complete.checkOrder")}</p>
      )}

      <div className="flex gap-3 justify-center">
        {orderId && (
          <Link
            href={`/order/${orderId}`}
            className="px-6 py-3 bg-[var(--color-secondary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
          >
            {t("order.complete.viewDetail")}
          </Link>
        )}
        <Link
          href="/products"
          className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          {t("order.complete.continueShopping")}
        </Link>
      </div>
    </div>
  );
}

export default function OrderCompletePage() {
  return (
    <Suspense fallback={
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    }>
      <OrderCompleteContent />
    </Suspense>
  );
}
