"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import {
  User, Package, MapPin, Heart, Star, MessageCircleQuestion,
  FolderOpen, Coins, Ticket, Bell,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";

const NAV_ITEMS = [
  { href: "/mypage", label: "내 정보", icon: User, exact: true },
  { href: "/mypage/orders", label: "주문내역", icon: Package },
  { href: "/mypage/addresses", label: "배송지 관리", icon: MapPin },
  { href: "/mypage/wishlist", label: "찜 목록", icon: Heart },
  { href: "/mypage/reviews", label: "내 리뷰", icon: Star },
  { href: "/mypage/qna", label: "내 QnA", icon: MessageCircleQuestion },
  { href: "/mypage/collections", label: "컬렉션", icon: FolderOpen },
  { href: "/mypage/points", label: "포인트", icon: Coins },
  { href: "/mypage/coupons", label: "쿠폰", icon: Ticket },
  { href: "/mypage/notifications", label: "알림", icon: Bell },
];

export default function MypageLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
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
          <h1 className="text-lg font-bold text-[var(--color-secondary)]">마이페이지</h1>
          <p className="text-sm text-gray-500">{user?.name}</p>
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-6">
        {/* Sidebar (desktop) */}
        <nav className="hidden lg:block w-56 shrink-0">
          <div className="bg-white rounded-xl shadow-sm p-3 sticky top-24 space-y-1">
            {NAV_ITEMS.map(({ href, label, icon: Icon, exact }) => (
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
                {label}
              </Link>
            ))}
          </div>
        </nav>

        {/* Mobile tabs */}
        <nav className="lg:hidden overflow-x-auto -mx-4 px-4">
          <div className="flex gap-1 min-w-max pb-2">
            {NAV_ITEMS.map(({ href, label, icon: Icon, exact }) => (
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
                {label}
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
