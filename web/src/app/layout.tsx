import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "@/components/layout/Providers";
import { Header } from "@/components/layout/Header";
import { Footer } from "@/components/layout/Footer";
import { NotificationToast } from "@/components/NotificationToast";
import { PWARegistrar } from "@/components/PWARegistrar";

const TENANT_NAME = process.env.NEXT_PUBLIC_TENANT_NAME || "SynDock Shop";
const TENANT_DESC = process.env.NEXT_PUBLIC_TENANT_DESC || "멀티테넌트 쇼핑몰 플랫폼";

export const metadata: Metadata = {
  title: {
    default: TENANT_NAME,
    template: `%s | ${TENANT_NAME}`,
  },
  description: TENANT_DESC,
  openGraph: {
    type: "website",
    siteName: TENANT_NAME,
    title: TENANT_NAME,
    description: TENANT_DESC,
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="ko">
      <head>
        <link rel="manifest" href="/manifest.json" />
        <meta name="theme-color" content="#D4AF37" />
        <meta name="apple-mobile-web-app-capable" content="yes" />
        <meta name="apple-mobile-web-app-status-bar-style" content="default" />
        <link rel="apple-touch-icon" href="/icons/icon-192.svg" />
      </head>
      <body className="min-h-screen flex flex-col">
        <Providers>
          <Header />
          <NotificationToast />
          <PWARegistrar />
          <main className="flex-1">{children}</main>
          <Footer />
        </Providers>
      </body>
    </html>
  );
}
