"use client";

import { useEffect, useRef, useState } from "react";
import { Globe } from "lucide-react";
import { useTranslations } from "next-intl";

const LOCALES = [
  { code: "ko", flag: "\u{1F1F0}\u{1F1F7}", label: "ko" },
  { code: "en", flag: "\u{1F1FA}\u{1F1F8}", label: "en" },
  { code: "ja", flag: "\u{1F1EF}\u{1F1F5}", label: "ja" },
] as const;

function getCurrentLocale(): string {
  if (typeof document === "undefined") return "ko";
  const match = document.cookie.match(/(?:^|;\s*)NEXT_LOCALE=([^;]*)/);
  return match?.[1] || localStorage.getItem("locale") || "ko";
}

export function LanguageSwitcher() {
  const t = useTranslations("language");
  const [open, setOpen] = useState(false);
  const [current, setCurrent] = useState("ko");
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setCurrent(getCurrentLocale());
  }, []);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const switchLocale = (code: string) => {
    document.cookie = `NEXT_LOCALE=${code};path=/;max-age=${60 * 60 * 24 * 365}`;
    localStorage.setItem("locale", code);
    setOpen(false);
    window.location.reload();
  };

  const currentLocale = LOCALES.find((l) => l.code === current) || LOCALES[0];

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-1 hover:text-white transition-colors text-xs"
        title={t("select")}
      >
        <Globe size={14} />
        <span>{currentLocale.flag} {t(currentLocale.code as "ko" | "en" | "ja")}</span>
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-1 bg-white rounded-lg shadow-lg border border-gray-200 py-1 min-w-[140px] z-50">
          {LOCALES.map((locale) => (
            <button
              key={locale.code}
              onClick={() => switchLocale(locale.code)}
              className={`w-full flex items-center gap-2 px-3 py-2 text-sm transition-colors ${
                current === locale.code
                  ? "bg-[var(--color-primary)]/10 text-[var(--color-primary)] font-medium"
                  : "text-gray-700 hover:bg-gray-50"
              }`}
            >
              <span>{locale.flag}</span>
              <span>{t(locale.code as "ko" | "en" | "ja")}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
