import type { Metadata } from "next";
import { generatePostMetadata } from "@/lib/seo";
import PostDetailClient from "./PostDetailClient";

const API_URL = process.env.API_URL || "http://127.0.0.1:5100";
const TENANT_SLUG = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";

async function fetchPost(id: string) {
  try {
    const res = await fetch(`${API_URL}/api/post/${id}`, {
      headers: { "X-Tenant-Id": TENANT_SLUG },
      next: { revalidate: 60 },
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const post = await fetchPost(id);
  if (!post) {
    return { title: "게시글을 찾을 수 없습니다" };
  }

  return generatePostMetadata({
    title: post.title,
    content: post.content,
    userName: post.userName,
    imageUrl: post.images?.[0]?.url,
  });
}

export default function PostDetailPage() {
  return <PostDetailClient />;
}
