"use client";

import Link from "next/link";
import { motion } from "framer-motion";
import { ChevronRight } from "lucide-react";
import { useTranslations } from "next-intl";
import type { ProductSummary } from "@/types/product";
import { ProductCard } from "./ProductCard";

interface ProductSectionProps {
  title: string;
  subtitle?: string;
  products: ProductSummary[];
  moreHref?: string;
  onAddToCart?: (product: ProductSummary) => void;
}

export function ProductSection({ title, subtitle, products, moreHref, onAddToCart }: ProductSectionProps) {
  const t = useTranslations("common");
  return (
    <section className="max-w-7xl mx-auto px-4 py-16">
      <div className="flex items-end justify-between mb-10">
        <div>
          <h2 className="text-3xl font-bold text-[var(--color-secondary)] mb-2">{title}</h2>
          {subtitle && <p className="text-gray-500">{subtitle}</p>}
        </div>
        {moreHref && (
          <Link
            href={moreHref}
            className="hidden md:flex items-center gap-1 text-[var(--color-primary)] font-medium hover:underline"
          >
            {t("more")} <ChevronRight size={18} />
          </Link>
        )}
      </div>

      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
        {products.map((product, i) => (
          <motion.div
            key={product.id}
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.3, delay: i * 0.05 }}
          >
            <ProductCard product={product} onAddToCart={onAddToCart} />
          </motion.div>
        ))}
      </div>

      {moreHref && (
        <div className="mt-8 text-center md:hidden">
          <Link
            href={moreHref}
            className="inline-flex items-center gap-1 px-6 py-3 border border-[var(--color-primary)] text-[var(--color-primary)] rounded-lg font-medium hover:bg-[var(--color-primary)] hover:text-white transition-colors"
          >
            {t("more")} <ChevronRight size={18} />
          </Link>
        </div>
      )}
    </section>
  );
}
