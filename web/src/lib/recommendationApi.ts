import api from "./api";

export interface RecommendedProduct {
  productId: number;
  productName: string;
  imageUrl: string | null;
  price: number;
  salePrice: number | null;
  score: number;
}

export async function getProductRecommendations(
  productId: number,
  count = 6
): Promise<RecommendedProduct[]> {
  const { data } = await api.get<RecommendedProduct[]>(
    `/recommendations/product/${productId}`,
    { params: { count } }
  );
  return data;
}

export async function getUserRecommendations(
  count = 6
): Promise<RecommendedProduct[]> {
  const { data } = await api.get<RecommendedProduct[]>(
    "/recommendations/user",
    { params: { count } }
  );
  return data;
}

export async function getPopularProducts(
  count = 6
): Promise<RecommendedProduct[]> {
  const { data } = await api.get<RecommendedProduct[]>(
    "/recommendations/popular",
    { params: { count } }
  );
  return data;
}
