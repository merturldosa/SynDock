"use client";

import { AlertTriangle } from "lucide-react";

export default function ErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center px-4">
      <AlertTriangle size={64} className="text-gray-300 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-2">
        문제가 발생했습니다
      </h1>
      <p className="text-gray-500 text-center max-w-md">
        페이지를 불러오는 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.
      </p>
      <button
        onClick={() => reset()}
        className="mt-6 px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
      >
        다시 시도
      </button>
    </div>
  );
}
