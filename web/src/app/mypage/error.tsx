"use client";

import { AlertTriangle, RotateCcw } from "lucide-react";
import { useTranslations } from "next-intl";

export default function MypageErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const t = useTranslations();

  return (
    <div className="flex flex-col items-center justify-center py-20 px-4">
      <div className="bg-white rounded-2xl shadow-sm p-10 max-w-md w-full text-center">
        <div className="w-16 h-16 bg-red-50 rounded-full flex items-center justify-center mx-auto mb-6">
          <AlertTriangle size={32} className="text-red-500" />
        </div>
        <h1 className="text-xl font-bold text-[var(--color-secondary)] mb-2">
          {t("error.title")}
        </h1>
        <p className="text-gray-500 text-sm mb-6">
          {t("error.description")}
        </p>
        {error.digest && (
          <p className="text-xs text-gray-400 mb-4 font-mono">
            Error ID: {error.digest}
          </p>
        )}
        <button
          onClick={() => reset()}
          className="inline-flex items-center gap-2 px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity"
        >
          <RotateCcw size={16} />
          {t("error.retry")}
        </button>
      </div>
    </div>
  );
}
