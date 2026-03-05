"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useTenantStore } from "@/stores/tenantStore";
import { getCategories } from "@/lib/productApi";
import { useTranslations } from "next-intl";
import toast from "react-hot-toast";
import type { CategoryInfo } from "@/types/product";

export function Footer() {
  const t = useTranslations();
  const { name: tenantName, config } = useTenantStore();
  const [categories, setCategories] = useState<CategoryInfo[]>([]);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => { toast.error("Failed to load categories"); });
  }, []);

  const footerCategories = categories.slice(0, 5);

  return (
    <footer className="bg-[var(--color-secondary)] text-white/60">
      <div className="max-w-7xl mx-auto px-4 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8 mb-8">
          {/* Brand */}
          <div>
            <p className="text-[var(--color-primary)] font-bold text-xl mb-3">
              {tenantName || "Shop"}
            </p>
            <p className="text-sm leading-relaxed">
              {config?.companyName || tenantName || "Shop"}
              <br />
              {t("footer.tagline")}
            </p>
          </div>

          {/* Categories */}
          <div>
            <h4 className="text-white font-semibold mb-3">{t("footer.categories")}</h4>
            <ul className="space-y-2 text-sm">
              {footerCategories.map((cat) => (
                <li key={cat.id}>
                  <Link
                    href={`/products?category=${cat.slug}`}
                    className="hover:text-[var(--color-primary)] transition-colors"
                  >
                    {cat.name}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Customer Service */}
          <div>
            <h4 className="text-white font-semibold mb-3">{t("footer.customerService")}</h4>
            <ul className="space-y-2 text-sm">
              {config?.contactPhone && (
                <li className="text-white font-medium text-base">{config.contactPhone}</li>
              )}
              {config?.contactFax && <li>FAX: {config.contactFax}</li>}
              <li>{t("footer.businessHours")}</li>
              {config?.contactEmail && (
                <li>
                  <a
                    href={`mailto:${config.contactEmail}`}
                    className="hover:text-[var(--color-primary)] transition-colors"
                  >
                    {config.contactEmail}
                  </a>
                </li>
              )}
            </ul>
          </div>

          {/* Company Info */}
          <div>
            <h4 className="text-white font-semibold mb-3">{t("footer.companyInfo")}</h4>
            <ul className="space-y-2 text-sm">
              {config?.companyName && <li>{t("footer.companyName")}: {config.companyName}</li>}
              {config?.ceoName && <li>{t("footer.ceo")}: {config.ceoName}</li>}
              {config?.businessNumber && <li>{t("footer.businessNumber")}: {config.businessNumber}</li>}
              {config?.companyAddress && <li>{config.companyAddress}</li>}
            </ul>
          </div>
        </div>

        <div className="border-t border-white/10 pt-6 flex flex-col md:flex-row items-center justify-between gap-4 text-xs">
          <p>&copy; {new Date().getFullYear()} {config?.companyName || tenantName || "Shop"}. {t("footer.allRightsReserved")}</p>
          <div className="flex gap-4">
            <Link href="/privacy" className="hover:text-[var(--color-primary)] transition-colors">{t("footer.privacyPolicy")}</Link>
            <Link href="/terms" className="hover:text-[var(--color-primary)] transition-colors">{t("footer.terms")}</Link>
            {config?.privacyOfficer && <span>{t("footer.privacyOfficer")}: {config.privacyOfficer}</span>}
          </div>
        </div>
      </div>
    </footer>
  );
}
