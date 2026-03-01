"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { ChevronLeft } from "lucide-react";
import { ProductDetailSectionEditor } from "@/components/admin/ProductDetailSectionEditor";

export default function ProductSectionsPage() {
  const params = useParams();
  const productId = Number(params.id);

  if (!productId) return null;

  return (
    <div className="max-w-3xl">
      <div className="flex items-center gap-3 mb-6">
        <Link
          href={`/admin/products/${productId}/edit`}
          className="flex items-center gap-1 text-sm text-gray-500 hover:text-[var(--color-secondary)]"
        >
          <ChevronLeft size={16} /> 상품 수정으로 돌아가기
        </Link>
      </div>

      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        상세 섹션 관리
      </h1>

      <ProductDetailSectionEditor productId={productId} />
    </div>
  );
}
