import type { Metadata } from "next";
import { generateProductMetadata, generateProductJsonLd, generateBreadcrumbJsonLd } from "@/lib/seo";
import ProductDetailClient from "./ProductDetailClient";

const API_URL = process.env.API_URL || "http://127.0.0.1:5100";
const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || "https://shop.syndock.com";
const TENANT_SLUG = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";
const TENANT_NAME = process.env.NEXT_PUBLIC_TENANT_NAME || "SynDock Shop";

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

export default async function ProductDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const product = await fetchProduct(id);

  const primaryImage = product?.images?.find((img: { isPrimary: boolean }) => img.isPrimary);
  const imageUrl = primaryImage?.url || product?.images?.[0]?.url;

  const productJsonLd = product
    ? generateProductJsonLd({
        name: product.name,
        description: product.description,
        price: product.price,
        salePrice: product.salePrice,
        imageUrl,
        slug: product.slug || id,
        categoryName: product.categoryName,
      })
    : null;

  const breadcrumbJsonLd = product
    ? generateBreadcrumbJsonLd([
        { name: TENANT_NAME, url: SITE_URL },
        { name: "상품", url: `${SITE_URL}/products` },
        ...(product.categoryName
          ? [{ name: product.categoryName, url: `${SITE_URL}/categories/${product.categorySlug || ""}` }]
          : []),
        { name: product.name, url: `${SITE_URL}/products/${product.slug || id}` },
      ])
    : null;

  return (
    <>
      {productJsonLd && (
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(productJsonLd) }}
        />
      )}
      {breadcrumbJsonLd && (
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(breadcrumbJsonLd) }}
        />
      )}
      <ProductDetailClient />
    </>
  );
}
