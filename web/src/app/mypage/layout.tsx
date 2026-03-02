"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { useTranslations } from "next-intl";
import {
  User, Package, MapPin, Heart, Star, MessageCircleQuestion,
  FolderOpen, Coins, Ticket, Bell,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";

const NAV_ITEMS = [
  { href: "/mypage", labelKey: "info", icon: User, exact: true },
  { href: "/mypage/orders", labelKey: "orders", icon: Package },
  { href: "/mypage/addresses", labelKey: "addresses", icon: MapPin },
  { href: "/mypage/wishlist", labelKey: "wishlist", icon: Heart },
  { href: "/mypage/reviews", labelKey: "reviews", icon: Star },
  { href: "/mypage/qna", labelKey: "qna", icon: MessageCircleQuestion },
  { href: "/mypage/collections", labelKey: "collections", icon: FolderOpen },
  { href: "/mypage/points", labelKey: "points", icon: Coins },
  { href: "/mypage/coupons", labelKey: "coupons", icon: Ticket },
  { href: "/mypage/notifications", labelKey: "notifications", icon: Bell },
];

export default function MypageLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const t = useTranslations("mypage");
  const { user, isAuthenticated, isLoading, fetchMe } = useAuthStore();

  useEffect(() => {
    fetchMe();
  }, [fetchMe]);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      sessionStorage.setItem("returnTo", pathname);
      router.replace("/login");
    }
  }, [isLoading, isAuthenticated, router, pathname]);

  if (isLoading || !isAuthenticated) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  const isActive = (href: string, exact?: boolean) =>
    exact ? pathname === href : pathname.startsWith(href);

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 rounded-full bg-[var(--color-secondary)] flex items-center justify-center">
          <User size={20} className="text-white" />
        </div>
        <div>
          <h1 className="text-lg font-bold text-[var(--color-secondary)]">{t("title")}</h1>
          <p className="text-sm text-gray-500">{user?.name}</p>
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-6">
        {/* Sidebar (desktop) */}
        <nav className="hidden lg:block w-56 shrink-0">
          <div className="bg-white rounded-xl shadow-sm p-3 sticky top-24 space-y-1">
            {NAV_ITEMS.map(({ href, labelKey, icon: Icon, exact }) => (
              <Link
                key={href}
                href={href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors ${
                  isActive(href, exact)
                    ? "bg-[var(--color-primary)] text-white font-medium"
                    : "text-gray-600 hover:bg-gray-50"
                }`}
              >
                <Icon size={18} />
                {t(`nav.${labelKey}`)}
              </Link>
            ))}
          </div>
        </nav>

        {/* Mobile tabs */}
        <nav className="lg:hidden overflow-x-auto -mx-4 px-4">
          <div className="flex gap-1 min-w-max pb-2">
            {NAV_ITEMS.map(({ href, labelKey, icon: Icon, exact }) => (
              <Link
                key={href}
                href={href}
                className={`flex items-center gap-1.5 px-3 py-2 rounded-full text-xs whitespace-nowrap transition-colors ${
                  isActive(href, exact)
                    ? "bg-[var(--color-primary)] text-white font-medium"
                    : "bg-gray-100 text-gray-600"
                }`}
              >
                <Icon size={14} />
                {t(`nav.${labelKey}`)}
              </Link>
            ))}
          </div>
        </nav>

        {/* Content */}
        <div className="flex-1 min-w-0">
          {children}
        </div>
      </div>
    </div>
  );
}
