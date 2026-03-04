"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { ProductCard } from "@/components/home/ProductCard";
import { getProducts, getCategories, getSearchSuggestions, type SearchSuggestion } from "@/lib/productApi";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";
import { useTranslations } from "next-intl";
import type { ProductSummary, CategoryInfo } from "@/types/product";
import Link from "next/link";
import Image from "next/image";
import { Search, X, SlidersHorizontal, Star } from "lucide-react";

function ProductList() {
  const t = useTranslations();
  const router = useRouter();
  const searchParams = useSearchParams();
  const categoryFilter = searchParams.get("category");
  const sortParam = searchParams.get("sort");
  const searchParam = searchParams.get("search");
  const minPriceParam = searchParams.get("minPrice");
  const maxPriceParam = searchParams.get("maxPrice");
  const minRatingParam = searchParams.get("minRating");
  const isFeaturedParam = searchParams.get("isFeatured");
  const isNewParam = searchParams.get("isNew");
  const { addToCart } = useCartStore();
  const { isAuthenticated } = useAuthStore();

  const [products, setProducts] = useState<ProductSummary[]>([]);
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  // Search & suggestions
  const [searchTerm, setSearchTerm] = useState(searchParam || "");
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const searchRef = useRef<HTMLDivElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  // Filter state
  const [filterOpen, setFilterOpen] = useState(false);
  const [minPrice, setMinPrice] = useState(minPriceParam || "");
  const [maxPrice, setMaxPrice] = useState(maxPriceParam || "");
  const [minRating, setMinRating] = useState(minRatingParam ? Number(minRatingParam) : 0);
  const [isFeatured, setIsFeatured] = useState(isFeaturedParam === "true");
  const [isNew, setIsNew] = useState(isNewParam === "true");

  useEffect(() => {
    getCategories().then(setCategories).catch(() => {});
  }, []);

  // Close suggestions on outside click
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (searchRef.current && !searchRef.current.contains(e.target as Node)) {
        setShowSuggestions(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Build URL params
  const buildParams = useCallback(() => {
    const p = new URLSearchParams();
    if (categoryFilter) p.set("category", categoryFilter);
    if (sortParam) p.set("sort", sortParam);
    if (searchParam) p.set("search", searchParam);
    if (minPriceParam) p.set("minPrice", minPriceParam);
    if (maxPriceParam) p.set("maxPrice", maxPriceParam);
    if (minRatingParam) p.set("minRating", minRatingParam);
    if (isFeaturedParam) p.set("isFeatured", isFeaturedParam);
    if (isNewParam) p.set("isNew", isNewParam);
    return p;
  }, [categoryFilter, sortParam, searchParam, minPriceParam, maxPriceParam, minRatingParam, isFeaturedParam, isNewParam]);

  useEffect(() => {
    setLoading(true);
    setPage(1);
    getProducts({
      category: categoryFilter || undefined,
      sort: sortParam || undefined,
      search: searchParam || undefined,
      minPrice: minPriceParam ? Number(minPriceParam) : undefined,
      maxPrice: maxPriceParam ? Number(maxPriceParam) : undefined,
      minRating: minRatingParam ? Number(minRatingParam) : undefined,
      isFeatured: isFeaturedParam === "true" ? true : undefined,
      isNew: isNewParam === "true" ? true : undefined,
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
  }, [categoryFilter, sortParam, searchParam, minPriceParam, maxPriceParam, minRatingParam, isFeaturedParam, isNewParam]);

  const loadMore = () => {
    const nextPage = page + 1;
    getProducts({
      category: categoryFilter || undefined,
      sort: sortParam || undefined,
      search: searchParam || undefined,
      minPrice: minPriceParam ? Number(minPriceParam) : undefined,
      maxPrice: maxPriceParam ? Number(maxPriceParam) : undefined,
      minRating: minRatingParam ? Number(minRatingParam) : undefined,
      isFeatured: isFeaturedParam === "true" ? true : undefined,
      isNew: isNewParam === "true" ? true : undefined,
      page: nextPage,
      pageSize: 24,
    }).then((res) => {
      setProducts((prev) => [...prev, ...res.items]);
      setPage(nextPage);
    });
  };

  // Search suggestions with debounce
  const handleSearchInput = (value: string) => {
    setSearchTerm(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (value.length >= 1) {
      debounceRef.current = setTimeout(() => {
        getSearchSuggestions(value).then((s) => {
          setSuggestions(s);
          setShowSuggestions(s.length > 0);
        }).catch(() => {});
      }, 300);
    } else {
      setSuggestions([]);
      setShowSuggestions(false);
    }
  };

  const handleSearch = (e?: React.FormEvent) => {
    e?.preventDefault();
    setShowSuggestions(false);
    const p = buildParams();
    if (searchTerm) p.set("search", searchTerm);
    else p.delete("search");
    router.push(`/products?${p.toString()}`);
  };

  const applyFilters = () => {
    const p = new URLSearchParams();
    if (categoryFilter) p.set("category", categoryFilter);
    if (sortParam) p.set("sort", sortParam);
    if (searchParam) p.set("search", searchParam);
    if (minPrice) p.set("minPrice", minPrice);
    if (maxPrice) p.set("maxPrice", maxPrice);
    if (minRating > 0) p.set("minRating", String(minRating));
    if (isFeatured) p.set("isFeatured", "true");
    if (isNew) p.set("isNew", "true");
    setFilterOpen(false);
    router.push(`/products?${p.toString()}`);
  };

  const clearFilters = () => {
    setMinPrice("");
    setMaxPrice("");
    setMinRating(0);
    setIsFeatured(false);
    setIsNew(false);
    const p = new URLSearchParams();
    if (categoryFilter) p.set("category", categoryFilter);
    if (sortParam) p.set("sort", sortParam);
    if (searchParam) p.set("search", searchParam);
    setFilterOpen(false);
    router.push(`/products?${p.toString()}`);
  };

  const removeFilter = (key: string) => {
    const p = buildParams();
    p.delete(key);
    if (key === "minPrice") setMinPrice("");
    if (key === "maxPrice") setMaxPrice("");
    if (key === "minRating") setMinRating(0);
    if (key === "isFeatured") setIsFeatured(false);
    if (key === "isNew") setIsNew(false);
    router.push(`/products?${p.toString()}`);
  };

  const currentCategory = categories.find((c) => c.slug === categoryFilter);
  const hasActiveFilters = !!(minPriceParam || maxPriceParam || minRatingParam || isFeaturedParam || isNewParam);

  // Active filter chips
  const activeFilters: { key: string; label: string }[] = [];
  if (minPriceParam) activeFilters.push({ key: "minPrice", label: t("products.priceAbove", { price: Number(minPriceParam).toLocaleString() }) });
  if (maxPriceParam) activeFilters.push({ key: "maxPrice", label: t("products.priceBelow", { price: Number(maxPriceParam).toLocaleString() }) });
  if (minRatingParam) activeFilters.push({ key: "minRating", label: t("products.ratingAbove", { rating: minRatingParam }) });
  if (isFeaturedParam === "true") activeFilters.push({ key: "isFeatured", label: t("products.featured") });
  if (isNewParam === "true") activeFilters.push({ key: "isNew", label: t("products.new") });

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link href="/" className="hover:text-[var(--color-primary)]">{t("common.home")}</Link>
        <span>/</span>
        <span className="text-[var(--color-secondary)] font-medium">
          {currentCategory ? currentCategory.name : t("common.allProducts")}
        </span>
      </div>

      {/* Search Bar */}
      <div ref={searchRef} className="relative mb-6">
        <form onSubmit={handleSearch} className="flex gap-2">
          <div className="relative flex-1">
            <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => handleSearchInput(e.target.value)}
              onFocus={() => suggestions.length > 0 && setShowSuggestions(true)}
              placeholder={t("common.searchPlaceholder")}
              className="w-full pl-10 pr-4 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]"
            />
            {searchTerm && (
              <button
                type="button"
                onClick={() => { setSearchTerm(""); setSuggestions([]); setShowSuggestions(false); }}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
              >
                <X size={16} />
              </button>
            )}
          </div>
          <button
            type="submit"
            className="px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
          >
            {t("common.search")}
          </button>
          <button
            type="button"
            onClick={() => setFilterOpen(!filterOpen)}
            className={`px-3 py-2.5 border rounded-lg text-sm font-medium flex items-center gap-1.5 transition-colors ${
              hasActiveFilters
                ? "border-[var(--color-primary)] text-[var(--color-primary)] bg-[var(--color-primary)]/5"
                : "border-gray-200 text-gray-600 hover:bg-gray-50"
            }`}
          >
            <SlidersHorizontal size={16} />
            {t("common.filter")}
            {hasActiveFilters && (
              <span className="w-5 h-5 rounded-full bg-[var(--color-primary)] text-white text-xs flex items-center justify-center">
                {activeFilters.length}
              </span>
            )}
          </button>
        </form>

        {/* Suggestions Dropdown */}
        {showSuggestions && suggestions.length > 0 && (
          <div className="absolute z-20 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-80 overflow-y-auto">
            {suggestions.map((s) => (
              <Link
                key={s.id}
                href={`/products/${s.id}`}
                onClick={() => setShowSuggestions(false)}
                className="flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors"
              >
                <div className="relative w-10 h-10 rounded bg-gray-100 shrink-0 overflow-hidden">
                  {s.primaryImageUrl ? (
                    <Image src={s.primaryImageUrl} alt={s.name} fill className="object-cover" sizes="40px" />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-sm opacity-30">📦</div>
                  )}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">{s.name}</p>
                  <p className="text-xs text-[var(--color-primary)] font-bold">
                    {s.salePrice ? (
                      <>
                        <span className="line-through text-gray-400 mr-1">{s.price.toLocaleString()}{t("common.currency")}</span>
                        {s.salePrice.toLocaleString()}{t("common.currency")}
                      </>
                    ) : (
                      <>{s.price.toLocaleString()}{t("common.currency")}</>
                    )}
                  </p>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>

      {/* Filter Panel */}
      {filterOpen && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Price Range */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">{t("products.priceRange")}</label>
              <div className="flex items-center gap-2">
                <input
                  type="number"
                  value={minPrice}
                  onChange={(e) => setMinPrice(e.target.value)}
                  placeholder={t("products.minPrice")}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)]"
                />
                <span className="text-gray-400">~</span>
                <input
                  type="number"
                  value={maxPrice}
                  onChange={(e) => setMaxPrice(e.target.value)}
                  placeholder={t("products.maxPrice")}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[var(--color-primary)]"
                />
              </div>
            </div>

            {/* Rating */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">{t("products.minRating")}</label>
              <div className="flex gap-1">
                {[1, 2, 3, 4, 5].map((n) => (
                  <button
                    key={n}
                    type="button"
                    onClick={() => setMinRating(minRating === n ? 0 : n)}
                    className="p-1"
                  >
                    <Star
                      size={24}
                      className={n <= minRating
                        ? "fill-yellow-400 text-yellow-400"
                        : "text-gray-300"
                      }
                    />
                  </button>
                ))}
                {minRating > 0 && (
                  <span className="text-sm text-gray-500 ml-2 self-center">{t("products.ratingAbove", { rating: minRating })}</span>
                )}
              </div>
            </div>

            {/* Toggles */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">{t("products.productType")}</label>
              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={() => setIsFeatured(!isFeatured)}
                  className={`px-3 py-1.5 rounded-full text-sm border transition-colors ${
                    isFeatured
                      ? "bg-[var(--color-primary)] text-white border-[var(--color-primary)]"
                      : "border-gray-200 text-gray-600 hover:border-gray-300"
                  }`}
                >
                  {t("products.featured")}
                </button>
                <button
                  type="button"
                  onClick={() => setIsNew(!isNew)}
                  className={`px-3 py-1.5 rounded-full text-sm border transition-colors ${
                    isNew
                      ? "bg-[var(--color-primary)] text-white border-[var(--color-primary)]"
                      : "border-gray-200 text-gray-600 hover:border-gray-300"
                  }`}
                >
                  {t("products.new")}
                </button>
              </div>
            </div>
          </div>
          <div className="flex justify-end gap-2 mt-4 pt-4 border-t border-gray-100">
            <button
              type="button"
              onClick={clearFilters}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-900"
            >
              {t("common.reset")}
            </button>
            <button
              type="button"
              onClick={applyFilters}
              className="px-6 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 transition-opacity"
            >
              {t("common.apply")}
            </button>
          </div>
        </div>
      )}

      {/* Active Filter Chips */}
      {activeFilters.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-4">
          {activeFilters.map((f) => (
            <span
              key={f.key}
              className="inline-flex items-center gap-1 px-3 py-1 bg-[var(--color-primary)]/10 text-[var(--color-primary)] rounded-full text-xs font-medium"
            >
              {f.label}
              <button onClick={() => removeFilter(f.key)} className="hover:text-red-500">
                <X size={14} />
              </button>
            </span>
          ))}
          <button
            onClick={clearFilters}
            className="text-xs text-gray-500 hover:text-red-500 underline"
          >
            {t("common.clearAll")}
          </button>
        </div>
      )}

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
          {t("common.all")}
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
        <p className="text-sm text-gray-500">{t("common.productsCount", { count: totalCount.toLocaleString() })}</p>
        <div className="flex gap-2 text-sm">
          {[
            { value: "", label: t("products.sort.default") },
            { value: "price_asc", label: t("products.sort.priceLow") },
            { value: "price_desc", label: t("products.sort.priceHigh") },
            { value: "name", label: t("products.sort.name") },
          ].map((opt) => (
            <Link
              key={opt.value}
              href={`/products?${new URLSearchParams({
                ...(categoryFilter ? { category: categoryFilter } : {}),
                ...(opt.value ? { sort: opt.value } : {}),
                ...(searchParam ? { search: searchParam } : {}),
                ...(minPriceParam ? { minPrice: minPriceParam } : {}),
                ...(maxPriceParam ? { maxPrice: maxPriceParam } : {}),
                ...(minRatingParam ? { minRating: minRatingParam } : {}),
                ...(isFeaturedParam ? { isFeatured: isFeaturedParam } : {}),
                ...(isNewParam ? { isNew: isNewParam } : {}),
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
              <p className="text-lg mb-2">
                {searchParam ? t("products.searchNoResults", { term: searchParam }) : t("common.noResults")}
              </p>
              <Link href="/products" className="text-[var(--color-primary)] hover:underline">
                {t("common.viewAllProducts")}
              </Link>
            </div>
          )}

          {page < totalPages && (
            <div className="mt-10 text-center">
              <button
                onClick={loadMore}
                className="px-8 py-3 border border-[var(--color-primary)] text-[var(--color-primary)] rounded-lg font-medium hover:bg-[var(--color-primary)] hover:text-white transition-colors"
              >
                {t("common.remaining", { count: totalCount - products.length })}
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
