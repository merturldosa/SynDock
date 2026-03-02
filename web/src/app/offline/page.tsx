"use client";

import { WifiOff } from "lucide-react";
import { useTranslations } from "next-intl";

export default function OfflinePage() {
  const t = useTranslations();

  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center px-4">
      <WifiOff size={64} className="text-gray-300 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-2">
        {t("offline.title")}
      </h1>
      <p className="text-gray-500 text-center max-w-md">
        {t("offline.description")}
      </p>
      <button
        onClick={() => window.location.reload()}
        className="mt-6 px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
      >
        {t("offline.retry")}
      </button>
    </div>
  );
}
