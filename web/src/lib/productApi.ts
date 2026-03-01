import api from "./api";
import type {
  ProductSummary,
  ProductDetail,
  CategoryInfo,
  PagedResponse,
} from "@/types/product";

export interface GetProductsParams {
  category?: string;
  search?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  isFeatured?: boolean;
  isNew?: boolean;
}

export async function getProducts(
  params: GetProductsParams = {}
): Promise<PagedResponse<ProductSummary>> {
  const { data } = await api.get<PagedResponse<ProductSummary>>("/products", {
    params,
  });
  return data;
}

export async function getProductById(id: number): Promise<ProductDetail> {
  const { data } = await api.get<ProductDetail>(`/products/${id}`);
  return data;
}

export async function getCategories(): Promise<CategoryInfo[]> {
  const { data } = await api.get<CategoryInfo[]>("/categories");
  return data;
}

export interface SearchSuggestion {
  id: number;
  name: string;
  primaryImageUrl: string | null;
  price: number;
  salePrice: number | null;
}

export async function getSearchSuggestions(
  term: string
): Promise<SearchSuggestion[]> {
  const { data } = await api.get<SearchSuggestion[]>("/products/suggestions", {
    params: { term },
  });
  return data;
}
