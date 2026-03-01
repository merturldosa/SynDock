import api from "./api";
import type { ReviewSummary, PagedQnA, WishlistItem } from "@/types/review";

// Reviews
export async function getProductReviews(productId: number, page = 1, pageSize = 10): Promise<ReviewSummary> {
  const { data } = await api.get<ReviewSummary>(`/review/product/${productId}`, { params: { page, pageSize } });
  return data;
}

export async function createReview(productId: number, rating: number, content?: string, imageUrl?: string): Promise<{ reviewId: number }> {
  const { data } = await api.post<{ reviewId: number }>("/review", { productId, rating, content, imageUrl });
  return data;
}

export async function deleteReview(id: number): Promise<void> {
  await api.delete(`/review/${id}`);
}

export async function getPhotoReviews(productId?: number, page = 1, pageSize = 10): Promise<{ totalCount: number; reviews: import("@/types/review").ReviewDto[] }> {
  const { data } = await api.get("/review/photos", { params: { productId, page, pageSize } });
  return data;
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

// My Reviews & QnA
export interface MyReview {
  id: number;
  productId: number;
  productName: string;
  productImageUrl: string | null;
  rating: number;
  content: string | null;
  imageUrl: string | null;
  createdAt: string;
}

export interface PagedMyReviews {
  items: MyReview[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function getMyReviews(page = 1, pageSize = 10): Promise<PagedMyReviews> {
  const { data } = await api.get<PagedMyReviews>("/review/my", { params: { page, pageSize } });
  return data;
}

export interface MyQnA {
  id: number;
  productId: number;
  productName: string;
  productImageUrl: string | null;
  title: string;
  content: string;
  isSecret: boolean;
  isAnswered: boolean;
  createdAt: string;
}

export interface PagedMyQnAs {
  items: MyQnA[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function getMyQnAs(page = 1, pageSize = 10): Promise<PagedMyQnAs> {
  const { data } = await api.get<PagedMyQnAs>("/qna/my", { params: { page, pageSize } });
  return data;
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
