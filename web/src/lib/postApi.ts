import api from "./api";
import type {
  PostDto,
  PagedPosts,
  SocialProfile,
  FollowUser,
  HashtagInfo,
} from "@/types/post";

// ── Posts ──
export async function getFeed(
  page = 1,
  pageSize = 20,
  postType?: string,
  userId?: number
): Promise<PagedPosts> {
  const params: Record<string, string | number> = { page, pageSize };
  if (postType) params.postType = postType;
  if (userId) params.userId = userId;
  const { data } = await api.get("/api/post/feed", { params });
  return data;
}

export async function getPost(id: number): Promise<PostDto> {
  const { data } = await api.get(`/api/post/${id}`);
  return data;
}

export async function createPost(body: {
  title?: string;
  content: string;
  postType: string;
  productId?: number;
  imageUrls?: string[];
  hashtags?: string[];
}): Promise<{ postId: number }> {
  const { data } = await api.post("/api/post", body);
  return data;
}

export async function deletePost(id: number): Promise<void> {
  await api.delete(`/api/post/${id}`);
}

export async function addComment(
  postId: number,
  content: string,
  parentId?: number
): Promise<{ commentId: number }> {
  const { data } = await api.post(`/api/post/${postId}/comment`, {
    content,
    parentId,
  });
  return data;
}

export async function toggleReaction(
  postId: number,
  reactionType: string
): Promise<{ isReacted: boolean }> {
  const { data } = await api.post(`/api/post/${postId}/reaction`, {
    reactionType,
  });
  return data;
}

// ── Follow ──
export async function toggleFollow(
  targetUserId: number
): Promise<{ isFollowing: boolean }> {
  const { data } = await api.post("/api/follow/toggle", { targetUserId });
  return data;
}

export async function getFollowers(userId: number): Promise<FollowUser[]> {
  const { data } = await api.get(`/api/follow/followers/${userId}`);
  return data;
}

export async function getFollowing(userId: number): Promise<FollowUser[]> {
  const { data } = await api.get(`/api/follow/following/${userId}`);
  return data;
}

export async function getSocialProfile(
  userId: number
): Promise<SocialProfile> {
  const { data } = await api.get(`/api/follow/profile/${userId}`);
  return data;
}

// ── Hashtags ──
export async function getTrendingHashtags(
  limit = 20
): Promise<HashtagInfo[]> {
  const { data } = await api.get("/api/hashtag/trending", {
    params: { limit },
  });
  return data;
}

export async function searchHashtags(keyword: string): Promise<HashtagInfo[]> {
  const { data } = await api.get("/api/hashtag/search", {
    params: { keyword },
  });
  return data;
}

export async function getPostsByHashtag(
  tag: string,
  page = 1,
  pageSize = 20
): Promise<PagedPosts> {
  const { data } = await api.get(`/api/hashtag/${tag}/posts`, {
    params: { page, pageSize },
  });
  return data;
}

// ── Upload ──
export async function uploadImage(
  file: File,
  folder = "general"
): Promise<{ url: string }> {
  const formData = new FormData();
  formData.append("file", file);
  const { data } = await api.post(`/api/upload/image?folder=${folder}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
}

export async function uploadImages(
  files: File[],
  folder = "general"
): Promise<{ urls: string[] }> {
  const formData = new FormData();
  files.forEach((f) => formData.append("files", f));
  const { data } = await api.post(`/api/upload/images?folder=${folder}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
}
