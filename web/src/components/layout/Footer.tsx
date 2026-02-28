"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useTenantStore } from "@/stores/tenantStore";
import { getCategories } from "@/lib/productApi";
import type { CategoryInfo } from "@/types/product";

export function Footer() {
  const { name: tenantName, config } = useTenantStore();
  const [categories, setCategories] = useState<CategoryInfo[]>([]);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => {});
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
              정성을 다해 엄선한 상품을 만나보세요.
            </p>
          </div>

          {/* Categories */}
          <div>
            <h4 className="text-white font-semibold mb-3">카테고리</h4>
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
            <h4 className="text-white font-semibold mb-3">고객센터</h4>
            <ul className="space-y-2 text-sm">
              {config?.contactPhone && (
                <li className="text-white font-medium text-base">{config.contactPhone}</li>
              )}
              {config?.contactFax && <li>FAX: {config.contactFax}</li>}
              <li>평일 09:00 ~ 18:00</li>
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
            <h4 className="text-white font-semibold mb-3">회사정보</h4>
            <ul className="space-y-2 text-sm">
              {config?.companyName && <li>상호: {config.companyName}</li>}
              {config?.ceoName && <li>대표: {config.ceoName}</li>}
              {config?.businessNumber && <li>사업자등록번호: {config.businessNumber}</li>}
              {config?.companyAddress && <li>{config.companyAddress}</li>}
            </ul>
          </div>
        </div>

        <div className="border-t border-white/10 pt-6 flex flex-col md:flex-row items-center justify-between gap-4 text-xs">
          <p>&copy; {new Date().getFullYear()} {config?.companyName || tenantName || "Shop"}. All rights reserved.</p>
          <div className="flex gap-4">
            <Link href="/privacy" className="hover:text-[var(--color-primary)] transition-colors">개인정보처리방침</Link>
            <Link href="/terms" className="hover:text-[var(--color-primary)] transition-colors">이용약관</Link>
            {config?.privacyOfficer && <span>정보책임자: {config.privacyOfficer}</span>}
          </div>
        </div>
      </div>
    </footer>
  );
}
