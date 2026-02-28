"use client";

import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { Suspense } from "react";
import { CheckCircle } from "lucide-react";

function OrderCompleteContent() {
  const searchParams = useSearchParams();
  const orderId = searchParams.get("id");

  return (
    <div className="max-w-lg mx-auto px-4 py-20 text-center">
      <CheckCircle size={72} className="mx-auto text-emerald-500 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-3">주문이 완료되었습니다!</h1>
      <p className="text-gray-500 mb-2">감사합니다. 주문이 정상적으로 접수되었습니다.</p>
      {orderId && (
        <p className="text-sm text-gray-400 mb-8">주문번호는 주문내역에서 확인할 수 있습니다.</p>
      )}

      <div className="flex gap-3 justify-center">
        {orderId && (
          <Link
            href={`/order/${orderId}`}
            className="px-6 py-3 bg-[var(--color-secondary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
          >
            주문 상세보기
          </Link>
        )}
        <Link
          href="/products"
          className="px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          쇼핑 계속하기
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
