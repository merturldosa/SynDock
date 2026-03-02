"use client";

import Link from "next/link";
import { motion } from "framer-motion";
import { useEffect, useState } from "react";
import { getCategories } from "@/lib/productApi";
import { useTranslations } from "next-intl";
import type { CategoryInfo } from "@/types/product";

export function CategorySection() {
  const t = useTranslations();
  const [categories, setCategories] = useState<CategoryInfo[]>([]);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => {});
  }, []);

  if (categories.length === 0) return null;

  return (
    <section className="max-w-7xl mx-auto px-4 py-16">
      <div className="text-center mb-12">
        <h2 className="text-3xl font-bold text-[var(--color-secondary)] mb-3">{t("category.title")}</h2>
        <p className="text-gray-500">{t("category.subtitle")}</p>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        {categories.map((cat, i) => (
          <motion.div
            key={cat.id}
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.3, delay: i * 0.05 }}
          >
            <Link
              href={`/products?category=${cat.slug}`}
              className="group flex flex-col items-center p-6 bg-white rounded-2xl shadow-sm hover:shadow-lg transition-all hover:-translate-y-1"
            >
              <span className="text-4xl mb-3">{cat.icon || "📦"}</span>
              <span className="font-semibold text-[var(--color-secondary)] group-hover:text-[var(--color-primary)] transition-colors">
                {cat.name}
              </span>
              <span className="text-xs text-gray-500 mt-1">{t("common.nItems", { count: cat.productCount })}</span>
            </Link>
          </motion.div>
        ))}
      </div>
    </section>
  );
}
