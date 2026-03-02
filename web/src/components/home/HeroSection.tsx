"use client";

import Link from "next/link";
import { motion } from "framer-motion";
import { useTenantStore } from "@/stores/tenantStore";
import { useTranslations } from "next-intl";

export function HeroSection() {
  const t = useTranslations();
  const { name: tenantName, config } = useTenantStore();

  const pattern = config?.heroPattern || "circle";
  const ctas = config?.heroCta || [
    { label: t("hero.browseProducts"), href: "/products", variant: "primary" as const },
  ];

  return (
    <section className="relative bg-[var(--color-secondary)] overflow-hidden">
      {/* Background pattern */}
      {pattern === "cross" ? (
        <div className="absolute inset-0 opacity-10">
          <div className="absolute top-10 left-10 text-[200px] leading-none text-[var(--color-primary)]">&#10013;</div>
          <div className="absolute bottom-10 right-10 text-[150px] leading-none text-[var(--color-primary)]">&#10013;</div>
        </div>
      ) : (
        <div className="absolute inset-0 opacity-5">
          <div className="absolute top-10 left-10 w-48 h-48 rounded-full bg-[var(--color-primary)]" />
          <div className="absolute bottom-10 right-10 w-36 h-36 rounded-full bg-[var(--color-primary)]" />
        </div>
      )}

      <div className="relative max-w-7xl mx-auto px-4 py-20 md:py-32">
        <div className="max-w-2xl">
          <motion.p
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5 }}
            className="text-[var(--color-primary)] text-sm font-medium tracking-widest uppercase mb-4"
          >
            {config?.heroTagline || "Welcome to our shop"}
          </motion.p>
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, delay: 0.1 }}
            className="text-4xl md:text-6xl font-bold text-white mb-6 leading-tight"
          >
            {config?.heroSubtitle || t("hero.defaultSubtitle")}
            <br />
            <span className="text-[var(--color-primary)]">{tenantName || "Shop"}</span>
          </motion.h1>
          {config?.heroDescription && (
            <motion.p
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, delay: 0.2 }}
              className="text-white/70 text-lg mb-10 leading-relaxed whitespace-pre-line"
            >
              {config.heroDescription}
            </motion.p>
          )}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, delay: 0.3 }}
            className="flex flex-wrap gap-4"
          >
            {ctas.map((cta, i) =>
              cta.variant === "outline" ? (
                <Link
                  key={i}
                  href={cta.href}
                  className="px-8 py-4 border border-white/30 text-white rounded-lg font-medium hover:bg-white/10 transition-colors"
                >
                  {cta.label}
                </Link>
              ) : (
                <Link
                  key={i}
                  href={cta.href}
                  className="px-8 py-4 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:bg-[var(--color-primary-light)] transition-colors"
                >
                  {cta.label}
                </Link>
              )
            )}
          </motion.div>
        </div>
      </div>

      {/* Bottom gradient */}
      <div className="absolute bottom-0 left-0 right-0 h-16 bg-gradient-to-t from-[var(--color-background)] to-transparent" />
    </section>
  );
}
