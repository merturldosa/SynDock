"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { ProductCard } from "@/components/home/ProductCard";
import { getProducts, getCategories } from "@/lib/productApi";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";
import type { ProductSummary, CategoryInfo } from "@/types/product";
import Link from "next/link";

function ProductList() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const categoryFilter = searchParams.get("category");
  const sortParam = searchParams.get("sort");
  const searchParam = searchParams.get("search");
  const { addToCart } = useCartStore();
  const { isAuthenticated } = useAuthStore();

  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => {});
  }, []);

  useEffect(() => {
    setLoading(true);
    setPage(1);
    getProducts({
      category: categoryFilter || undefined,
      sort: sortParam || undefined,
      search: searchParam || undefined,
      page: 1,
      pageSize: 24,
    })
      .then((res) => {
        setProducts(res.items);
        setTotalCount(res.totalCount);
        setTotalPages(res.totalPages);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [categoryFilter, sortParam, searchParam]);

  const loadMore = () => {
    const nextPage = page + 1;
    getProducts({
      category: categoryFilter || undefined,
      sort: sortParam || undefined,
      search: searchParam || undefined,
      page: nextPage,
      pageSize: 24,
    }).then((res) => {
      setProducts((prev) => [...prev, ...res.items]);
      setPage(nextPage);
    });
  };

  const currentCategory = categories.find((c) => c.slug === categoryFilter);

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link href="/" className="hover:text-[var(--color-primary)]">홈</Link>
        <span>/</span>
        <span className="text-[var(--color-secondary)] font-medium">
          {currentCategory ? currentCategory.name : "전체상품"}
        </span>
      </div>

      {/* Category chips */}
      <div className="flex flex-wrap gap-2 mb-8">
        <Link
          href="/products"
          className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
            !categoryFilter
              ? "bg-[var(--color-secondary)] text-white"
              : "bg-white text-[var(--color-secondary)] border border-gray-200 hover:border-[var(--color-primary)]"
          }`}
        >
          전체
        </Link>
        {categories.map((cat) => (
          <Link
            key={cat.id}
            href={`/products?category=${cat.slug}`}
            className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
              categoryFilter === cat.slug
                ? "bg-[var(--color-secondary)] text-white"
                : "bg-white text-[var(--color-secondary)] border border-gray-200 hover:border-[var(--color-primary)]"
            }`}
          >
            {cat.icon} {cat.name}
            <span className="ml-1 text-xs opacity-60">({cat.productCount})</span>
          </Link>
        ))}
      </div>

      {/* Sort */}
      <div className="flex items-center justify-between mb-6">
        <p className="text-sm text-gray-500">{totalCount.toLocaleString()}개의 상품</p>
        <div className="flex gap-2 text-sm">
          {[
            { value: "", label: "기본" },
            { value: "price_asc", label: "낮은가격" },
            { value: "price_desc", label: "높은가격" },
            { value: "name", label: "이름순" },
          ].map((opt) => (
            <Link
              key={opt.value}
              href={`/products?${new URLSearchParams({
                ...(categoryFilter ? { category: categoryFilter } : {}),
                ...(opt.value ? { sort: opt.value } : {}),
              }).toString()}`}
              className={`px-3 py-1 rounded text-xs ${
                (sortParam || "") === opt.value
                  ? "bg-[var(--color-secondary)] text-white"
                  : "text-gray-500 hover:text-[var(--color-secondary)]"
              }`}
            >
              {opt.label}
            </Link>
          ))}
        </div>
      </div>

      {/* Loading */}
      {loading && (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      )}

      {/* Results */}
      {!loading && (
        <>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 md:gap-6">
            {products.map((product) => (
              <ProductCard
                key={product.id}
                product={product}
                onAddToCart={(p) => {
                  if (!isAuthenticated) {
                    sessionStorage.setItem("returnTo", "/products");
                    router.push("/login");
                    return;
                  }
                  addToCart(p.id);
                }}
              />
            ))}
          </div>

          {products.length === 0 && (
            <div className="text-center py-20 text-gray-500">
              <p className="text-lg mb-2">해당 카테고리에 상품이 없습니다.</p>
              <Link href="/products" className="text-[var(--color-primary)] hover:underline">
                전체상품 보기
              </Link>
            </div>
          )}

          {page < totalPages && (
            <div className="mt-10 text-center">
              <button
                onClick={loadMore}
                className="px-8 py-3 border border-[var(--color-primary)] text-[var(--color-primary)] rounded-lg font-medium hover:bg-[var(--color-primary)] hover:text-white transition-colors"
              >
                더보기 ({totalCount - products.length}개 남음)
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

export default function ProductsPage() {
  return (
    <Suspense fallback={
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    }>
      <ProductList />
    </Suspense>
  );
}
