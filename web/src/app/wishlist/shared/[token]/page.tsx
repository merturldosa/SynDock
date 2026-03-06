import type { Metadata } from "next";
import Link from "next/link";
import { Heart } from "lucide-react";

const API_URL = process.env.API_URL || "http://127.0.0.1:5100";
const TENANT_SLUG = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";

interface SharedItem {
  productId: number;
  productName: string;
  imageUrl?: string;
  price: number;
  slug?: string;
}

async function fetchSharedWishlist(token: string): Promise<SharedItem[]> {
  try {
    const res = await fetch(`${API_URL}/api/wishlist/shared/${token}`, {
      headers: { "X-Tenant-Id": TENANT_SLUG },
      cache: "no-store",
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export const metadata: Metadata = {
  title: "Shared Wishlist",
};

export default async function SharedWishlistPage({
  params,
}: {
  params: Promise<{ token: string }>;
}) {
  const { token } = await params;
  const items = await fetchSharedWishlist(token);

  if (items.length === 0) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <Heart size={64} className="mx-auto text-gray-300 mb-6" />
        <h1 className="text-xl font-bold text-gray-800 mb-2">Wishlist</h1>
        <p className="text-gray-500 mb-6">This wishlist is empty or the link has expired.</p>
        <Link
          href="/products"
          className="inline-block px-6 py-3 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
        >
          Browse Products
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex items-center gap-2 mb-6">
        <Heart size={24} className="text-[var(--color-primary)]" />
        <h1 className="text-2xl font-bold text-[var(--color-secondary)]">
          Shared Wishlist
          <span className="text-[var(--color-primary)] ml-2">({items.length})</span>
        </h1>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {items.map((item) => (
          <Link
            key={item.productId}
            href={`/products/${item.productId}`}
            className="bg-white rounded-xl shadow-sm overflow-hidden hover:shadow-md transition-shadow"
          >
            <div className="aspect-square bg-gray-100">
              {item.imageUrl ? (
                <img
                  src={item.imageUrl}
                  alt={item.productName}
                  className="w-full h-full object-cover"
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center text-4xl opacity-20">
                  📦
                </div>
              )}
            </div>
            <div className="p-3">
              <h3 className="text-sm font-medium text-[var(--color-secondary)] line-clamp-2">
                {item.productName}
              </h3>
              <p className="text-sm font-bold text-[var(--color-primary)] mt-1">
                ₩{item.price.toLocaleString()}
              </p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
