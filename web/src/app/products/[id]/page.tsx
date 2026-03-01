import type { Metadata } from "next";
import { generateProductMetadata } from "@/lib/seo";
import ProductDetailClient from "./ProductDetailClient";

const API_URL = process.env.API_URL || "http://127.0.0.1:5100";
const TENANT_SLUG = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";

async function fetchProduct(id: string) {
  try {
    const res = await fetch(`${API_URL}/api/products/${id}`, {
      headers: { "X-Tenant-Id": TENANT_SLUG },
      next: { revalidate: 60 },
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const product = await fetchProduct(id);
  if (!product) {
    return { title: "상품을 찾을 수 없습니다" };
  }

  const primaryImage = product.images?.find((img: { isPrimary: boolean }) => img.isPrimary);

  return generateProductMetadata({
    name: product.name,
    description: product.description,
    price: product.price,
    salePrice: product.salePrice,
    imageUrl: primaryImage?.url || product.images?.[0]?.url,
    categoryName: product.categoryName,
  });
}

export default function ProductDetailPage() {
  return <ProductDetailClient />;
}
