import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import MypageLayoutClient from "./MypageLayoutClient";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("mypage");
  return {
    title: t("meta.title"),
    description: t("meta.description"),
  };
}

export default function MypageLayout({ children }: { children: React.ReactNode }) {
  return <MypageLayoutClient>{children}</MypageLayoutClient>;
}
