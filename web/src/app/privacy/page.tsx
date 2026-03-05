"use client";

import { useTranslations } from "next-intl";
import { useTenantStore } from "@/stores/tenantStore";

export default function PrivacyPage() {
  const t = useTranslations();
  const { name } = useTenantStore();

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("legal.privacyPolicy")}
      </h1>

      <div className="space-y-8 text-sm text-gray-700 leading-relaxed">
        {/* 1. 개인정보의 처리 목적 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section1Title")}
          </h2>
          <p>
            {t("legal.privacy.section1Body", { name })}
          </p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>{t("legal.privacy.section1Item1")}</li>
            <li>{t("legal.privacy.section1Item2")}</li>
            <li>{t("legal.privacy.section1Item3")}</li>
          </ul>
        </section>

        {/* 2. 개인정보의 처리 및 보유기간 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section2Title")}
          </h2>
          <p>
            {t("legal.privacy.section2Body")}
          </p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>{t("legal.privacy.section2Item1")}</li>
            <li>{t("legal.privacy.section2Item2")}</li>
            <li>{t("legal.privacy.section2Item3")}</li>
            <li>{t("legal.privacy.section2Item4")}</li>
          </ul>
        </section>

        {/* 3. 개인정보의 제3자 제공 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section3Title")}
          </h2>
          <p>
            {t("legal.privacy.section3Body")}
          </p>
        </section>

        {/* 4. 정보주체의 권리·의무 및 행사방법 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section4Title")}
          </h2>
          <p>{t("legal.privacy.section4Body")}</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>{t("legal.privacy.section4Item1")}</li>
            <li>{t("legal.privacy.section4Item2")}</li>
            <li>{t("legal.privacy.section4Item3")}</li>
            <li>{t("legal.privacy.section4Item4")}</li>
          </ul>
        </section>

        {/* 5. 처리하는 개인정보 항목 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section5Title")}
          </h2>
          <p>{t("legal.privacy.section5Body")}</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>{t("legal.privacy.section5Item1")}</li>
            <li>{t("legal.privacy.section5Item2")}</li>
            <li>{t("legal.privacy.section5Item3")}</li>
          </ul>
        </section>

        {/* 6. 개인정보의 안전성 확보조치 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section6Title")}
          </h2>
          <p>{t("legal.privacy.section6Body")}</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>{t("legal.privacy.section6Item1")}</li>
            <li>{t("legal.privacy.section6Item2")}</li>
            <li>{t("legal.privacy.section6Item3")}</li>
          </ul>
        </section>

        {/* 7. 개인정보 보호책임자 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section7Title")}
          </h2>
          <p>
            {t("legal.privacy.section7Body")}
          </p>
          <div className="mt-2 p-4 bg-gray-50 rounded-lg">
            <p>{t("legal.privacy.section7Officer", { name })}</p>
            <p>{t("legal.privacy.section7Contact")}</p>
          </div>
        </section>

        <p className="text-gray-400 text-xs pt-4 border-t border-gray-200">
          {t("legal.effectiveNotice", { document: t("legal.privacyDocument") })}
        </p>
      </div>
    </div>
  );
}
