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
    `${product.name} - ${product.categoryName || "상품"} | ${TENANT_NAME}`;
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
    ? `${post.title} | 커뮤니티`
    : `${post.userName}님의 게시글 | 커뮤니티`;
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
