import api from "./api";
import type { ReviewSummary, PagedQnA, WishlistItem } from "@/types/review";

// Reviews
export async function getProductReviews(productId: number, page = 1, pageSize = 10): Promise<ReviewSummary> {
  const { data } = await api.get<ReviewSummary>(`/review/product/${productId}`, { params: { page, pageSize } });
  return data;
}

export async function createReview(productId: number, rating: number, content?: string): Promise<{ reviewId: number }> {
  const { data } = await api.post<{ reviewId: number }>("/review", { productId, rating, content });
  return data;
}

export async function deleteReview(id: number): Promise<void> {
  await api.delete(`/review/${id}`);
}

// Q&A
export async function getProductQnAs(productId: number, page = 1, pageSize = 10): Promise<PagedQnA> {
  const { data } = await api.get<PagedQnA>(`/qna/product/${productId}`, { params: { page, pageSize } });
  return data;
}

export async function createQnA(productId: number, title: string, content: string, isSecret = false): Promise<{ qnaId: number }> {
  const { data } = await api.post<{ qnaId: number }>("/qna", { productId, title, content, isSecret });
  return data;
}

export async function answerQnA(qnaId: number, content: string): Promise<{ replyId: number }> {
  const { data } = await api.post<{ replyId: number }>(`/qna/${qnaId}/answer`, { content });
  return data;
}

export async function deleteQnA(id: number): Promise<void> {
  await api.delete(`/qna/${id}`);
}

// Wishlist
export async function getWishlist(): Promise<WishlistItem[]> {
  const { data } = await api.get<WishlistItem[]>("/wishlist");
  return data;
}

export async function toggleWishlist(productId: number): Promise<{ isWished: boolean }> {
  const { data } = await api.post<{ isWished: boolean }>("/wishlist/toggle", { productId });
  return data;
}

export async function checkWishlist(productIds: number[]): Promise<{ wishedProductIds: number[] }> {
  const { data } = await api.post<{ wishedProductIds: number[] }>("/wishlist/check", { productIds });
  return data;
}
