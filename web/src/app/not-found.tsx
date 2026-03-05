import Link from "next/link";
import { SearchX } from "lucide-react";
import { getTranslations } from "next-intl/server";

export default async function NotFoundPage() {
  const t = await getTranslations();

  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center px-4">
      <SearchX size={64} className="text-gray-300 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-2">
        {t("notFound.title")}
      </h1>
      <p className="text-gray-500 text-center max-w-md">
        {t("notFound.description")}
      </p>
      <Link
        href="/"
        className="mt-6 px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
      >
        {t("notFound.goHome")}
      </Link>
    </div>
  );
}
