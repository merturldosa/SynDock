/**
 * Locale-aware number/price formatting utilities.
 * Reads the user's selected locale from the NEXT_LOCALE cookie (set by LanguageSwitcher).
 */

const LOCALE_MAP: Record<string, string> = {
  ko: "ko-KR",
  en: "en-US",
  ja: "ja-JP",
  "zh-CN": "zh-CN",
  vi: "vi-VN",
};

const CURRENCY_MAP: Record<string, string> = {
  ko: "KRW",
  en: "USD",
  ja: "JPY",
  "zh-CN": "CNY",
  vi: "VND",
};

function getLocaleKey(): string {
  if (typeof document === "undefined") return "ko";
  const match = document.cookie.match(/NEXT_LOCALE=([^;]+)/);
  return match?.[1] || "ko";
}

function getSystemLocale(): string {
  return LOCALE_MAP[getLocaleKey()] || "ko-KR";
}

/** Format a number with locale-aware thousand separators */
export function formatNumber(n: number): string {
  return n.toLocaleString(getSystemLocale());
}

/** Format a price with locale-aware currency symbol (KRW for ko, USD for en, etc.) */
export function formatPrice(price: number): string {
  const key = getLocaleKey();
  const locale = LOCALE_MAP[key] || "ko-KR";
  const currency = CURRENCY_MAP[key] || "KRW";

  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
    maximumFractionDigits: currency === "KRW" || currency === "JPY" || currency === "VND" ? 0 : 2,
  }).format(price);
}

/** Format a date with locale-aware formatting */
export function formatDate(date: string | Date): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return d.toLocaleString(getSystemLocale());
}

/** Format a date as short date (no time) */
export function formatDateShort(date: string | Date): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return d.toLocaleDateString(getSystemLocale());
}
