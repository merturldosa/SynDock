import type { Metadata } from "next";

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || "https://shop.syndock.com";
const TENANT_NAME = process.env.NEXT_PUBLIC_TENANT_NAME || "SynDock Shop";

export function generateProductMetadata(product: {
  name: string;
  description?: string | null;
  price: number;
  salePrice?: number | null;
  imageUrl?: string | null;
  categoryName?: string;
}): Metadata {
  const title = `${product.name} | ${TENANT_NAME}`;
  const description =
    product.description?.slice(0, 160) ||
    `${product.name} - ${product.categoryName || "Products"} | ${TENANT_NAME}`;
  const priceText = product.salePrice
    ? `${product.salePrice.toLocaleString("ko-KR")}원`
    : `${product.price.toLocaleString("ko-KR")}원`;

  return {
    title,
    description: `${description} (${priceText})`,
    openGraph: {
      title,
      description,
      type: "website",
      siteName: TENANT_NAME,
      ...(product.imageUrl && {
        images: [{ url: product.imageUrl, width: 600, height: 600, alt: product.name }],
      }),
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      ...(product.imageUrl && { images: [product.imageUrl] }),
    },
  };
}

export function generatePostMetadata(post: {
  title?: string | null;
  content: string;
  userName: string;
  imageUrl?: string | null;
}): Metadata {
  const title = post.title
    ? `${post.title} | Community`
    : `${post.userName}'s post | Community`;
  const description = post.content.slice(0, 160);

  return {
    title,
    description,
    openGraph: {
      title,
      description,
      type: "article",
      siteName: TENANT_NAME,
      ...(post.imageUrl && {
        images: [{ url: post.imageUrl, width: 600, height: 400, alt: title }],
      }),
    },
    twitter: {
      card: post.imageUrl ? "summary_large_image" : "summary",
      title,
      description,
    },
  };
}

export function generatePageMetadata(
  title: string,
  description: string
): Metadata {
  return {
    title: `${title} | ${TENANT_NAME}`,
    description,
    openGraph: {
      title: `${title} | ${TENANT_NAME}`,
      description,
      type: "website",
      siteName: TENANT_NAME,
    },
  };
}

// --- JSON-LD Structured Data ---

export function generateProductJsonLd(product: {
  name: string;
  description?: string | null;
  price: number;
  salePrice?: number | null;
  imageUrl?: string | null;
  slug?: string;
  categoryName?: string;
  sku?: string;
  rating?: number;
  reviewCount?: number;
}) {
  const jsonLd: Record<string, unknown> = {
    "@context": "https://schema.org",
    "@type": "Product",
    name: product.name,
    description: product.description || product.name,
    ...(product.imageUrl && { image: product.imageUrl }),
    ...(product.sku && { sku: product.sku }),
    offers: {
      "@type": "Offer",
      price: product.salePrice ?? product.price,
      priceCurrency: "KRW",
      availability: "https://schema.org/InStock",
      url: product.slug
        ? `${SITE_URL}/products/${product.slug}`
        : undefined,
    },
  };

  if (product.rating && product.reviewCount) {
    jsonLd.aggregateRating = {
      "@type": "AggregateRating",
      ratingValue: product.rating,
      reviewCount: product.reviewCount,
    };
  }

  if (product.categoryName) {
    jsonLd.category = product.categoryName;
  }

  return jsonLd;
}

export function generateOrganizationJsonLd(config?: {
  name?: string;
  url?: string;
  logo?: string;
  contactEmail?: string;
  contactPhone?: string;
  socialLinks?: Record<string, string | undefined>;
}) {
  const name = config?.name || TENANT_NAME;
  const url = config?.url || SITE_URL;

  const sameAs = config?.socialLinks
    ? Object.values(config.socialLinks).filter(Boolean)
    : [];

  return {
    "@context": "https://schema.org",
    "@type": "Organization",
    name,
    url,
    ...(config?.logo && { logo: config.logo }),
    ...(config?.contactEmail && {
      contactPoint: {
        "@type": "ContactPoint",
        email: config.contactEmail,
        ...(config?.contactPhone && { telephone: config.contactPhone }),
        contactType: "customer service",
      },
    }),
    ...(sameAs.length > 0 && { sameAs }),
  };
}

export function generateBreadcrumbJsonLd(
  items: { name: string; url: string }[]
) {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: item.url,
    })),
  };
}
