"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { CheckCircle, AlertCircle, Loader2 } from "lucide-react";
import { confirmPayment } from "@/lib/orderApi";

function PaymentSuccessContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const paymentKey = searchParams.get("paymentKey");
    const orderId = searchParams.get("orderId");
    const amount = searchParams.get("amount");

    if (!paymentKey || !orderId || !amount) {
      setStatus("error");
      setErrorMessage("결제 정보가 올바르지 않습니다.");
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
        setErrorMessage(err?.response?.data?.error || "결제 승인에 실패했습니다.");
      });
  }, [searchParams, router]);

  return (
    <div className="max-w-md mx-auto px-4 py-20 text-center">
      {status === "loading" && (
        <>
          <Loader2 size={64} className="mx-auto text-[var(--color-primary)] animate-spin mb-6" />
          <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">결제 승인 중...</h1>
          <p className="text-gray-500 text-sm">잠시만 기다려 주세요.</p>
        </>
      )}

      {status === "success" && (
        <>
          <CheckCircle size={64} className="mx-auto text-emerald-500 mb-6" />
          <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">결제가 완료되었습니다</h1>
          <p className="text-gray-500 text-sm">주문 완료 페이지로 이동합니다...</p>
        </>
      )}

      {status === "error" && (
        <>
          <AlertCircle size={64} className="mx-auto text-red-500 mb-6" />
          <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">결제 승인 실패</h1>
          <p className="text-red-500 text-sm mb-6">{errorMessage}</p>
          <button
            onClick={() => router.push("/order")}
            className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
          >
            주문서로 돌아가기
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
