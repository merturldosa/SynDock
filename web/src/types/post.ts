export interface PostImage {
  id: number;
  url: string;
  altText?: string;
  sortOrder: number;
}

export interface PostComment {
  id: number;
  userId: number;
  userName: string;
  content: string;
  parentId?: number;
  replies?: PostComment[];
  createdAt: string;
}

export interface PostDto {
  id: number;
  userId: number;
  userName: string;
  title?: string;
  content: string;
  postType: string;
  productId?: number;
  productName?: string;
  viewCount: number;
  reactionCount: number;
  commentCount: number;
  images: PostImage[];
  hashtags: string[];
  comments?: PostComment[];
  myReaction?: string;
  createdAt: string;
}

export interface PostSummary {
  id: number;
  userId: number;
  userName: string;
  title?: string;
  contentPreview: string;
  postType: string;
  thumbnailUrl?: string;
  reactionCount: number;
  commentCount: number;
  hashtags: string[];
  createdAt: string;
}

export interface PagedPosts {
  totalCount: number;
  page: number;
  pageSize: number;
  items: PostSummary[];
}

export interface FollowUser {
  userId: number;
  userName: string;
  name?: string;
  followedAt: string;
}

export interface SocialProfile {
  userId: number;
  userName: string;
  name?: string;
  postCount: number;
  followerCount: number;
  followingCount: number;
  isFollowing: boolean;
}

export interface HashtagInfo {
  id: number;
  tag: string;
  postCount: number;
}
