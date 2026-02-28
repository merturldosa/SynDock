"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Plus, Edit2, Trash2, Search } from "lucide-react";
import { getProducts } from "@/lib/productApi";
import { deleteProduct } from "@/lib/adminApi";
import type { ProductSummary, PagedResponse } from "@/types/product";

function formatPrice(price: number): string {
  return price.toLocaleString("ko-KR") + "원";
}

export default function AdminProductsPage() {
  const [data, setData] = useState<PagedResponse<ProductSummary> | null>(null);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [searchInput, setSearchInput] = useState("");
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    getProducts({ page, pageSize: 20, search: search || undefined })
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [page, search]);

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`"${name}" 상품을 삭제하시겠습니까?`)) return;
    try {
      await deleteProduct(id);
      load();
    } catch {
      alert("삭제에 실패했습니다.");
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    setSearch(searchInput);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          상품 관리
        </h1>
        <Link
          href="/admin/products/new"
          className="flex items-center gap-2 px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90"
        >
          <Plus size={16} /> 상품 등록
        </Link>
      </div>

      {/* Search */}
      <form onSubmit={handleSearch} className="mb-4">
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Search
              size={16}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
            />
            <input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="상품명 검색..."
              className="w-full pl-9 pr-3 py-2.5 border rounded-lg text-sm"
            />
          </div>
          <button
            type="submit"
            className="px-4 py-2.5 bg-[var(--color-secondary)] text-white rounded-lg text-sm"
          >
            검색
          </button>
        </div>
      </form>

      {/* Product count */}
      {data && (
        <p className="text-sm text-gray-500 mb-3">
          총 {data.totalCount.toLocaleString()}개 상품
        </p>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>등록된 상품이 없습니다.</p>
        </div>
      ) : (
        <>
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="text-left p-3 font-medium text-gray-500">
                    이미지
                  </th>
                  <th className="text-left p-3 font-medium text-gray-500">
                    상품명
                  </th>
                  <th className="text-left p-3 font-medium text-gray-500">
                    카테고리
                  </th>
                  <th className="text-right p-3 font-medium text-gray-500">
                    가격
                  </th>
                  <th className="text-center p-3 font-medium text-gray-500">
                    관리
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((product) => (
                  <tr
                    key={product.id}
                    className="border-b last:border-0 hover:bg-gray-50"
                  >
                    <td className="p-3">
                      <div className="w-12 h-12 rounded-lg overflow-hidden bg-gray-100">
                        {product.primaryImageUrl ? (
                          <Image
                            src={product.primaryImageUrl}
                            alt={product.name}
                            width={48}
                            height={48}
                            className="object-cover w-full h-full"
                            unoptimized
                          />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-lg opacity-20">
                            📦
                          </div>
                        )}
                      </div>
                    </td>
                    <td className="p-3">
                      <p className="font-medium text-[var(--color-secondary)] line-clamp-1">
                        {product.name}
                      </p>
                    </td>
                    <td className="p-3 text-gray-500">{product.categoryName}</td>
                    <td className="p-3 text-right">
                      {product.priceType === "Inquiry" ? (
                        <span className="text-[var(--color-primary)] font-medium">
                          상담요망
                        </span>
                      ) : (
                        <span className="font-medium">
                          {formatPrice(product.salePrice || product.price)}
                        </span>
                      )}
                    </td>
                    <td className="p-3">
                      <div className="flex items-center justify-center gap-2">
                        <Link
                          href={`/admin/products/${product.id}/edit`}
                          className="p-1.5 text-gray-400 hover:text-[var(--color-primary)] transition-colors"
                        >
                          <Edit2 size={16} />
                        </Link>
                        <button
                          onClick={() => handleDelete(product.id, product.name)}
                          className="p-1.5 text-gray-400 hover:text-red-500 transition-colors"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {data.totalCount > 20 && (
            <div className="flex items-center justify-center gap-2 mt-6">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
              >
                이전
              </button>
              <span className="text-sm text-gray-500">
                {page} / {Math.ceil(data.totalCount / 20)}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page * 20 >= data.totalCount}
                className="px-3 py-2 text-sm border rounded-lg disabled:opacity-40"
              >
                다음
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
