import { getRequestConfig } from "next-intl/server";
import { cookies, headers } from "next/headers";

const SUPPORTED_LOCALES = ["ko", "en", "ja"] as const;
type Locale = (typeof SUPPORTED_LOCALES)[number];

function isSupported(locale: string): locale is Locale {
  return SUPPORTED_LOCALES.includes(locale as Locale);
}

function parseAcceptLanguage(header: string): Locale | null {
  const parts = header.split(",");
  for (const part of parts) {
    const lang = part.split(";")[0].trim().split("-")[0].toLowerCase();
    if (isSupported(lang)) return lang;
  }
  return null;
}

export default getRequestConfig(async () => {
  const cookieStore = await cookies();
  const headerStore = await headers();

  // 1) Cookie NEXT_LOCALE
  const cookieLocale = cookieStore.get("NEXT_LOCALE")?.value;
  if (cookieLocale && isSupported(cookieLocale)) {
    return {
      locale: cookieLocale,
      messages: (await import(`../messages/${cookieLocale}.json`)).default,
    };
  }

  // 2) Accept-Language header
  const acceptLang = headerStore.get("accept-language");
  if (acceptLang) {
    const detected = parseAcceptLanguage(acceptLang);
    if (detected) {
      return {
        locale: detected,
        messages: (await import(`../messages/${detected}.json`)).default,
      };
    }
  }

  // 3) Default: ko
  return {
    locale: "ko",
    messages: (await import("../messages/ko.json")).default,
  };
});
