"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Search } from "lucide-react";

export default function SearchPage() {
  const router = useRouter();
  const [query, setQuery] = useState("");

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      router.push(`/products?search=${encodeURIComponent(query.trim())}`);
    }
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-20">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] text-center mb-8">
        상품 검색
      </h1>
      <form onSubmit={handleSearch} className="flex gap-3">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="상품명을 입력하세요..."
          className="flex-1 h-12 px-4 rounded-lg border border-gray-300 bg-white text-[var(--color-secondary)] placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-[var(--color-primary)] focus:border-[var(--color-primary)]"
          autoFocus
        />
        <button
          type="submit"
          className="h-12 px-6 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90 transition-opacity flex items-center gap-2"
        >
          <Search size={20} /> 검색
        </button>
      </form>
    </div>
  );
}
