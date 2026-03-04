"use client";

import Link from "next/link";
import Image from "next/image";
import { useTenantStore } from "@/stores/tenantStore";

export function PromoBanner() {
  const { config } = useTenantStore();
  const banner = config?.promoBanner;

  if (!banner?.isActive || !banner?.title) return null;

  return (
    <section className="max-w-7xl mx-auto px-4 py-4">
      <div
        className="rounded-2xl overflow-hidden shadow-sm"
        style={{ backgroundColor: banner.backgroundColor || "var(--color-primary)" }}
      >
        {banner.linkUrl ? (
          <Link href={banner.linkUrl} className="block">
            <BannerContent banner={banner} />
          </Link>
        ) : (
          <BannerContent banner={banner} />
        )}
      </div>
    </section>
  );
}

function BannerContent({ banner }: { banner: NonNullable<ReturnType<typeof useTenantStore.getState>["config"]>["promoBanner"] }) {
  if (!banner) return null;

  return (
    <div className="flex items-center gap-6 p-6">
      {banner.imageUrl && (
        <div className="relative w-20 h-20 flex-shrink-0 rounded-xl overflow-hidden">
          <Image
            src={banner.imageUrl}
            alt={banner.title || ""}
            fill
            className="object-cover"
          />
        </div>
      )}
      <div className="flex-1 text-white">
        <h3 className="font-bold text-lg">{banner.title}</h3>
        {banner.description && (
          <p className="text-white/80 text-sm mt-1">{banner.description}</p>
        )}
      </div>
    </div>
  );
}
