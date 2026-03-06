import type { Metadata } from "next";
import Link from "next/link";
import { ArrowLeft, Calendar, BookOpen } from "lucide-react";

const API_URL = process.env.API_URL || "http://127.0.0.1:5100";
const TENANT_SLUG = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";

interface SaintDetail {
  id: number;
  koreanName: string;
  latinName?: string;
  englishName?: string;
  description?: string;
  feastDay?: string;
  patronage?: string;
  isActive: boolean;
}

interface SaintProductDto {
  productId: number;
  name: string;
  slug?: string;
  price: number;
  imageUrl?: string;
}

async function fetchSaint(id: string): Promise<SaintDetail | null> {
  try {
    const res = await fetch(`${API_URL}/api/saints/${id}`, {
      headers: { "X-Tenant-Id": TENANT_SLUG },
      next: { revalidate: 3600 },
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

async function fetchSaintProducts(id: string): Promise<SaintProductDto[]> {
  try {
    const res = await fetch(`${API_URL}/api/saints/${id}/products`, {
      headers: { "X-Tenant-Id": TENANT_SLUG },
      next: { revalidate: 3600 },
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const saint = await fetchSaint(id);
  if (!saint) {
    return { title: "Saint Not Found" };
  }
  return {
    title: saint.koreanName,
    description: saint.description?.slice(0, 160) || `${saint.koreanName} - Saint Detail`,
  };
}

export default async function SaintDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [saint, products] = await Promise.all([fetchSaint(id), fetchSaintProducts(id)]);

  if (!saint) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-16 text-center">
        <h1 className="text-2xl font-bold text-gray-800 mb-4">404</h1>
        <p className="text-gray-500 mb-6">Saint not found.</p>
        <Link
          href="/liturgy"
          className="inline-flex items-center gap-2 text-[var(--color-primary)] hover:underline font-medium"
        >
          <ArrowLeft size={16} />
          Back to Liturgy
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <Link
        href="/liturgy"
        className="inline-flex items-center gap-2 text-[var(--color-primary)] hover:underline font-medium mb-6"
      >
        <ArrowLeft size={16} />
        Back to Liturgy
      </Link>

      <div className="bg-white rounded-2xl shadow-sm p-8">
        <h1 className="text-3xl font-bold text-[var(--color-secondary)] mb-2">
          {saint.koreanName}
        </h1>

        {(saint.latinName || saint.englishName) && (
          <p className="text-gray-500 italic mb-6">
            {saint.latinName}
            {saint.latinName && saint.englishName && " / "}
            {saint.englishName}
          </p>
        )}

        <div className="flex flex-wrap gap-4 mb-8">
          {saint.feastDay && (
            <div className="flex items-center gap-2 px-4 py-2 bg-gray-50 rounded-lg">
              <Calendar size={16} className="text-[var(--color-primary)]" />
              <span className="text-sm text-gray-700">{saint.feastDay}</span>
            </div>
          )}
          {saint.patronage && (
            <div className="flex items-center gap-2 px-4 py-2 bg-gray-50 rounded-lg">
              <BookOpen size={16} className="text-[var(--color-primary)]" />
              <span className="text-sm text-gray-700">{saint.patronage}</span>
            </div>
          )}
        </div>

        {saint.description && (
          <div className="prose prose-gray max-w-none">
            <p className="text-gray-700 leading-relaxed whitespace-pre-line">
              {saint.description}
            </p>
          </div>
        )}
      </div>

      {/* Related Products */}
      {products.length > 0 && (
        <div className="mt-8">
          <h2 className="text-xl font-bold text-[var(--color-secondary)] mb-4">
            Related Products
          </h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {products.map((product) => (
              <Link
                key={product.productId}
                href={`/products/${product.productId}`}
                className="bg-white rounded-xl shadow-sm overflow-hidden hover:shadow-md transition-shadow"
              >
                <div className="aspect-square bg-gray-100">
                  {product.imageUrl ? (
                    <img
                      src={product.imageUrl}
                      alt={product.name}
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
                    {product.name}
                  </h3>
                  <p className="text-sm font-bold text-[var(--color-primary)] mt-1">
                    ₩{product.price.toLocaleString()}
                  </p>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
