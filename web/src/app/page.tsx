"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { HeroSection } from "@/components/home/HeroSection";
import { CategorySection } from "@/components/home/CategorySection";
import { ProductSection } from "@/components/home/ProductSection";
import { LiturgySummary } from "@/components/home/LiturgySummary";
import { PromoBanner } from "@/components/home/PromoBanner";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import { useTranslations } from "next-intl";
import { getProducts, getCategories } from "@/lib/productApi";
import type { ProductSummary, CategoryInfo } from "@/types/product";

export default function Home() {
  const t = useTranslations("products");
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();
  const { addToCart } = useCartStore();
  const [section1, setSection1] = useState<{ products: ProductSummary[]; category: CategoryInfo | null }>({ products: [], category: null });
  const [section2, setSection2] = useState<{ products: ProductSummary[]; category: CategoryInfo | null }>({ products: [], category: null });

  useEffect(() => {
    // Load categories first, then fetch products from the top 2 categories
    getCategories()
      .then((cats) => {
        if (cats.length > 0) {
          const cat1 = cats[0];
          setSection1((prev) => ({ ...prev, category: cat1 }));
          getProducts({ category: cat1.slug || undefined, pageSize: 8 })
            .then((res) => setSection1({ products: res.items, category: cat1 }))
            .catch(() => {});
        }
        if (cats.length > 1) {
          const cat2 = cats[1];
          setSection2((prev) => ({ ...prev, category: cat2 }));
          getProducts({ category: cat2.slug || undefined, pageSize: 8 })
            .then((res) => setSection2({ products: res.items, category: cat2 }))
            .catch(() => {});
        }
      })
      .catch(() => {});
  }, []);

  const handleAddToCart = async (product: ProductSummary) => {
    if (!isAuthenticated) {
      sessionStorage.setItem("returnTo", "/");
      router.push("/login");
      return;
    }
    await addToCart(product.id);
  };

  return (
    <>
      <HeroSection />
      <PromoBanner />
      <CategorySection />
      <LiturgySummary />
      {section1.category && (
        <ProductSection
          title={section1.category.name}
          subtitle={t("popularInCategory", { name: section1.category.name })}
          products={section1.products}
          moreHref={`/products?category=${section1.category.slug}`}
          onAddToCart={handleAddToCart}
        />
      )}
      {section2.category && (
        <ProductSection
          title={section2.category.name}
          subtitle={t("popularInCategory", { name: section2.category.name })}
          products={section2.products}
          moreHref={`/products?category=${section2.category.slug}`}
          onAddToCart={handleAddToCart}
        />
      )}
    </>
  );
}
