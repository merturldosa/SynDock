"use client";

import { AlertTriangle } from "lucide-react";
import { useTranslations } from "next-intl";

export default function ErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const t = useTranslations();

  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center px-4">
      <AlertTriangle size={64} className="text-gray-300 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-2">
        {t("error.title")}
      </h1>
      <p className="text-gray-500 text-center max-w-md">
        {t("error.description")}
      </p>
      <button
        onClick={() => reset()}
        className="mt-6 px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
      >
        {t("error.retry")}
      </button>
    </div>
  );
}
