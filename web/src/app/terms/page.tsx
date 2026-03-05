"use client";

import { useTranslations } from "next-intl";
import { useTenantStore } from "@/stores/tenantStore";

export default function TermsPage() {
  const t = useTranslations();
  const { name } = useTenantStore();

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("legal.termsOfService")}
      </h1>

      <div className="space-y-8 text-sm text-gray-700 leading-relaxed">
        {/* 1. 목적 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section1Title")}
          </h2>
          <p>
            {t("legal.terms.section1Body", { name })}
          </p>
        </section>

        {/* 2. 정의 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section2Title")}
          </h2>
          <ul className="list-disc pl-5 space-y-1">
            <li>{t("legal.terms.section2Item1")}</li>
            <li>{t("legal.terms.section2Item2")}</li>
            <li>{t("legal.terms.section2Item3")}</li>
          </ul>
        </section>

        {/* 3. 약관의 게시와 개정 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section3Title")}
          </h2>
          <ul className="list-decimal pl-5 space-y-1">
            <li>{t("legal.terms.section3Item1")}</li>
            <li>{t("legal.terms.section3Item2")}</li>
            <li>{t("legal.terms.section3Item3")}</li>
          </ul>
        </section>

        {/* 4. 서비스의 제공 및 변경 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section4Title")}
          </h2>
          <p>{t("legal.terms.section4Body")}</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>{t("legal.terms.section4Item1")}</li>
            <li>{t("legal.terms.section4Item2")}</li>
            <li>{t("legal.terms.section4Item3")}</li>
          </ul>
        </section>

        {/* 5. 구매신청 및 결제 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section5Title")}
          </h2>
          <p>
            {t("legal.terms.section5Body")}
          </p>
          <ul className="list-decimal pl-5 mt-2 space-y-1">
            <li>{t("legal.terms.section5Item1")}</li>
            <li>{t("legal.terms.section5Item2")}</li>
            <li>{t("legal.terms.section5Item3")}</li>
            <li>{t("legal.terms.section5Item4")}</li>
            <li>{t("legal.terms.section5Item5")}</li>
          </ul>
        </section>

        {/* 6. 청약철회 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section6Title")}
          </h2>
          <ul className="list-decimal pl-5 space-y-1">
            <li>
              {t("legal.terms.section6Item1")}
            </li>
            <li>
              {t("legal.terms.section6Item2")}
              <ul className="list-disc pl-5 mt-1 space-y-1">
                <li>{t("legal.terms.section6Sub1")}</li>
                <li>{t("legal.terms.section6Sub2")}</li>
                <li>{t("legal.terms.section6Sub3")}</li>
              </ul>
            </li>
          </ul>
        </section>

        {/* 7. 개인정보보호 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section7Title")}
          </h2>
          <p>
            {t("legal.terms.section7Body")}{" "}
            <a href="/privacy" className="text-[var(--color-primary)] hover:underline">
              {t("legal.privacyPolicy")}
            </a>
            {t("legal.terms.section7Link")}
          </p>
        </section>

        {/* 8. 분쟁해결 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.terms.section8Title")}
          </h2>
          <ul className="list-decimal pl-5 space-y-1">
            <li>
              {t("legal.terms.section8Item1")}
            </li>
            <li>
              {t("legal.terms.section8Item2")}
            </li>
          </ul>
        </section>

        <p className="text-gray-400 text-xs pt-4 border-t border-gray-200">
          {t("legal.effectiveNotice", { document: t("legal.termsDocument") })}
        </p>
      </div>
    </div>
  );
}
