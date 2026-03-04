import type { MetadataRoute } from "next";

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || "https://shop.syndock.com";
const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://127.0.0.1:5100";
const TENANT_SLUG = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";

interface ProductItem {
  slug: string;
  updatedAt?: string;
}

interface CategoryItem {
  slug: string;
}

async function fetchJson<T>(path: string): Promise<T | null> {
  try {
    const res = await fetch(`${API_URL}/api${path}`, {
      headers: { "X-Tenant-Id": TENANT_SLUG },
      next: { revalidate: 3600 },
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const entries: MetadataRoute.Sitemap = [];

  // Static pages
  const staticPages = [
    { url: SITE_URL, changeFrequency: "daily" as const, priority: 1.0 },
    { url: `${SITE_URL}/products`, changeFrequency: "daily" as const, priority: 0.9 },
    { url: `${SITE_URL}/categories`, changeFrequency: "weekly" as const, priority: 0.8 },
    { url: `${SITE_URL}/community`, changeFrequency: "daily" as const, priority: 0.7 },
    { url: `${SITE_URL}/auth/login`, changeFrequency: "monthly" as const, priority: 0.3 },
    { url: `${SITE_URL}/auth/register`, changeFrequency: "monthly" as const, priority: 0.3 },
  ];
  entries.push(...staticPages);

  // Dynamic: Products
  const products = await fetchJson<ProductItem[]>("/products/slugs");
  if (products) {
    for (const p of products) {
      entries.push({
        url: `${SITE_URL}/products/${p.slug}`,
        lastModified: p.updatedAt ? new Date(p.updatedAt) : new Date(),
        changeFrequency: "weekly",
        priority: 0.8,
      });
    }
  }

  // Dynamic: Categories
  const categories = await fetchJson<CategoryItem[]>("/categories/slugs");
  if (categories) {
    for (const c of categories) {
      entries.push({
        url: `${SITE_URL}/categories/${c.slug}`,
        changeFrequency: "weekly",
        priority: 0.7,
      });
    }
  }

  return entries;
}
