import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "@/components/layout/Providers";
import { Header } from "@/components/layout/Header";
import { Footer } from "@/components/layout/Footer";
import { NotificationToast } from "@/components/NotificationToast";
import { PWARegistrar } from "@/components/PWARegistrar";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages } from "next-intl/server";
import { generateOrganizationJsonLd } from "@/lib/seo";

const TENANT_NAME = process.env.NEXT_PUBLIC_TENANT_NAME || "SynDock Shop";
const TENANT_DESC = process.env.NEXT_PUBLIC_TENANT_DESC || "Multi-tenant Shopping Mall Platform";

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

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const locale = await getLocale();
  const messages = await getMessages();

  return (
    <html lang={locale}>
      <head>
        <link rel="manifest" href="/manifest.json" />
        <meta name="theme-color" content={process.env.NEXT_PUBLIC_TENANT_THEME_COLOR || "#D4AF37"} />
        <meta name="apple-mobile-web-app-capable" content="yes" />
        <meta name="apple-mobile-web-app-status-bar-style" content="default" />
        <link rel="apple-touch-icon" href="/icons/icon-192.svg" />
      </head>
      <body className="min-h-screen flex flex-col">
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify(
              generateOrganizationJsonLd({
                name: TENANT_NAME,
                url: process.env.NEXT_PUBLIC_SITE_URL || "https://shop.syndock.com",
              })
            ),
          }}
        />
        <NextIntlClientProvider messages={messages}>
          <Providers>
            <Header />
            <NotificationToast />
            <PWARegistrar />
            <div className="flex-1">{children}</div>
            <Footer />
          </Providers>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
