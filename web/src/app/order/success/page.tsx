"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { CheckCircle, AlertCircle, Loader2 } from "lucide-react";
import { confirmPayment } from "@/lib/orderApi";

function PaymentSuccessContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const t = useTranslations();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const paymentKey = searchParams.get("paymentKey");
    const orderId = searchParams.get("orderId");
    const amount = searchParams.get("amount");

    if (!paymentKey || !orderId || !amount) {
      setStatus("error");
      setErrorMessage(t("order.success.invalidInfo"));
      return;
    }

    confirmPayment(paymentKey, orderId, Number(amount))
      .then((result) => {
        setStatus("success");
        setTimeout(() => {
          router.push(`/order/complete?id=${result.orderId}`);
        }, 1500);
      })
      .catch((err) => {
        setStatus("error");
        setErrorMessage(err?.response?.data?.error || t("order.success.failed"));
      });
  }, [searchParams, router]);

  return (
    <div className="max-w-md mx-auto px-4 py-20 text-center">
      {status === "loading" && (
        <>
          <Loader2 size={64} className="mx-auto text-[var(--color-primary)] animate-spin mb-6" />
          <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">{t("order.success.approving")}</h1>
          <p className="text-gray-500 text-sm">{t("order.success.pleaseWait")}</p>
        </>
      )}

      {status === "success" && (
        <>
          <CheckCircle size={64} className="mx-auto text-emerald-500 mb-6" />
          <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">{t("order.success.completed")}</h1>
          <p className="text-gray-500 text-sm">{t("order.success.redirecting")}</p>
        </>
      )}

      {status === "error" && (
        <>
          <AlertCircle size={64} className="mx-auto text-red-500 mb-6" />
          <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">{t("order.success.failed")}</h1>
          <p className="text-red-500 text-sm mb-6">{errorMessage}</p>
          <button
            onClick={() => router.push("/order")}
            className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
          >
            {t("order.success.backToOrder")}
          </button>
        </>
      )}
    </div>
  );
}

export default function PaymentSuccessPage() {
  return (
    <Suspense fallback={
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    }>
      <PaymentSuccessContent />
    </Suspense>
  );
}
