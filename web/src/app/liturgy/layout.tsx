import type { Metadata } from "next";
import { generatePageMetadata } from "@/lib/seo";

export const metadata: Metadata = generatePageMetadata(
  "전례력",
  "오늘의 전례와 성인, 전례 시기 안내"
);

export default function LiturgyLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <>{children}</>;
}
