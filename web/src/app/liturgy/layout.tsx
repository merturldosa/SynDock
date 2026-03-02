import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { generatePageMetadata } from "@/lib/seo";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations();
  return generatePageMetadata(
    t("liturgy.title"),
    t("liturgy.metaDescription")
  );
}

export default function LiturgyLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <>{children}</>;
}
