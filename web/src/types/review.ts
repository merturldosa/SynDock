export interface ReviewDto {
  id: number;
  productId: number;
  userId: number;
  userName: string;
  rating: number;
  content: string | null;
  imageUrl: string | null;
  isVisible: boolean;
  createdAt: string;
}

export interface RatingDistribution {
  rating: number;
  count: number;
}

export interface ReviewSummary {
  totalCount: number;
  averageRating: number;
  photoReviewCount: number;
  ratingDistribution: RatingDistribution[];
  reviews: ReviewDto[];
}

export interface QnADto {
  id: number;
  productId: number;
  userId: number;
  userName: string;
  title: string;
  content: string;
  isAnswered: boolean;
  isSecret: boolean;
  reply: QnAReplyDto | null;
  createdAt: string;
}

export interface QnAReplyDto {
  id: number;
  userId: number;
  userName: string;
  content: string;
  createdAt: string;
}

export interface PagedQnA {
  items: QnADto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface WishlistItem {
  id: number;
  productId: number;
  productName: string;
  primaryImageUrl: string | null;
  price: number;
  salePrice: number | null;
  priceType: string;
  createdAt: string;
}
