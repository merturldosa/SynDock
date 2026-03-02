"use client";

import { Suspense } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { XCircle } from "lucide-react";

function PaymentFailContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const t = useTranslations();
  const code = searchParams.get("code");
  const message = searchParams.get("message");

  return (
    <div className="max-w-md mx-auto px-4 py-20 text-center">
      <XCircle size={64} className="mx-auto text-red-500 mb-6" />
      <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">{t("order.fail.title")}</h1>

      {message && (
        <p className="text-red-500 text-sm mb-2">{message}</p>
      )}
      {code && (
        <p className="text-gray-400 text-xs mb-6">{t("order.fail.errorCode", { code })}</p>
      )}

      <div className="flex gap-3 justify-center mt-6">
        <button
          onClick={() => router.push("/order")}
          className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          {t("order.fail.retry")}
        </button>
        <button
          onClick={() => router.push("/mypage/orders")}
          className="px-6 py-3 border border-gray-300 rounded-lg font-medium text-gray-700 hover:bg-gray-50 transition-colors"
        >
          {t("order.fail.viewOrders")}
        </button>
      </div>
    </div>
  );
}

export default function PaymentFailPage() {
  return (
    <Suspense fallback={
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-red-500 border-t-transparent" />
      </div>
    }>
      <PaymentFailContent />
    </Suspense>
  );
}
