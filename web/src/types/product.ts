export interface ProductSummary {
  id: number;
  name: string;
  slug: string | null;
  price: number;
  salePrice: number | null;
  priceType: "Fixed" | "Inquiry" | "Free";
  specification: string | null;
  categoryName: string;
  categorySlug: string | null;
  isFeatured: boolean;
  isNew: boolean;
  primaryImageUrl: string | null;
}

export interface ProductDetailSection {
  id: number;
  title: string;
  content: string | null;
  imageUrl: string | null;
  imageAltText: string | null;
  sectionType: string;
  sortOrder: number;
  isActive: boolean;
}

export interface ProductDetail {
  id: number;
  name: string;
  slug: string | null;
  description: string | null;
  price: number;
  salePrice: number | null;
  priceType: "Fixed" | "Inquiry" | "Free";
  specification: string | null;
  categoryId: number;
  categoryName: string;
  categorySlug: string | null;
  isFeatured: boolean;
  isNew: boolean;
  viewCount: number;
  images: ProductImage[];
  detailSections?: ProductDetailSection[];
}

export interface ProductImage {
  id: number;
  url: string;
  altText: string | null;
  isPrimary: boolean;
}

export interface CategoryInfo {
  id: number;
  name: string;
  slug: string | null;
  description: string | null;
  icon: string | null;
  productCount: number;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
}
