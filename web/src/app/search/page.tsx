"use client";

import { useState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import Link from "next/link";
import { Search } from "lucide-react";
import { useTranslations } from "next-intl";
import { getSearchSuggestions, type SearchSuggestion } from "@/lib/productApi";
import { formatPrice } from "@/lib/format";

export default function SearchPage() {
  const t = useTranslations();
  const router = useRouter();
  const [query, setQuery] = useState("");
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Debounced search suggestions
  useEffect(() => {
    if (query.trim().length < 2) {
      setSuggestions([]);
      setShowSuggestions(false);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        const results = await getSearchSuggestions(query.trim());
        setSuggestions(results);
        setShowSuggestions(true);
      } catch {
        setSuggestions([]);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [query]);

  // Click outside to close dropdown
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setShowSuggestions(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      setShowSuggestions(false);
      router.push(`/products?search=${encodeURIComponent(query.trim())}`);
    }
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-20">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] text-center mb-8">
        {t("search.title")}
      </h1>
      <div ref={containerRef} className="relative">
        <form onSubmit={handleSearch} className="flex gap-3">
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onFocus={() => suggestions.length > 0 && setShowSuggestions(true)}
            placeholder={t("search.placeholder")}
            className="flex-1 h-12 px-4 rounded-lg border border-gray-300 bg-white text-[var(--color-secondary)] placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)]"
            autoFocus
          />
          <button
            type="submit"
            className="h-12 px-6 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity flex items-center gap-2"
          >
            <Search size={20} /> {t("search.title")}
          </button>
        </form>

        {/* Autocomplete dropdown */}
        {showSuggestions && (
          <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-200 rounded-lg shadow-lg z-50 max-h-80 overflow-y-auto">
            {suggestions.length > 0 ? (
              suggestions.map((item) => (
                <Link
                  key={item.id}
                  href={`/products/${item.id}`}
                  onClick={() => setShowSuggestions(false)}
                  className="flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors border-b border-gray-100 last:border-b-0"
                >
                  <div className="w-12 h-12 flex-shrink-0 rounded-md overflow-hidden bg-gray-100">
                    {item.primaryImageUrl ? (
                      <Image
                        src={item.primaryImageUrl}
                        alt={item.name}
                        width={48}
                        height={48}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-gray-400 text-xs">
                        No img
                      </div>
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-900 truncate">
                      {item.name}
                    </p>
                    <p className="text-sm text-[var(--color-primary)] font-semibold">
                      {item.salePrice != null
                        ? formatPrice(item.salePrice)
                        : formatPrice(item.price)}
                      {item.salePrice != null && (
                        <span className="ml-2 text-xs text-gray-400 line-through">
                          {formatPrice(item.price)}
                        </span>
                      )}
                    </p>
                  </div>
                </Link>
              ))
            ) : (
              <div className="px-4 py-6 text-center text-gray-500 text-sm">
                {t("search.noResults")}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
