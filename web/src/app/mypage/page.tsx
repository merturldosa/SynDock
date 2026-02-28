"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { User, Package, ShoppingCart, Heart, Coins, Ticket, Bell } from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { Button } from "@/components/ui/Button";

export default function MyPage() {
  const router = useRouter();
  const { user, isAuthenticated, isLoading, fetchMe, logout } = useAuthStore();

  useEffect(() => {
    fetchMe();
  }, [fetchMe]);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      sessionStorage.setItem("returnTo", "/mypage");
      router.replace("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading || !user) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-12">
      <div className="bg-white rounded-2xl shadow-lg p-8">
        <div className="flex items-center gap-4 mb-8">
          <div className="w-16 h-16 rounded-full bg-[var(--color-secondary)] flex items-center justify-center">
            <User size={32} className="text-white" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-[var(--color-secondary)]">{user.name}</h1>
            <p className="text-gray-500">{user.email}</p>
          </div>
        </div>

        {/* Quick links */}
        <div className="grid grid-cols-2 gap-3 mb-8">
          <Link
            href="/mypage/orders"
            className="flex items-center gap-3 p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
          >
            <Package size={24} className="text-[var(--color-primary)]" />
            <span className="font-medium text-[var(--color-secondary)]">주문내역</span>
          </Link>
          <Link
            href="/cart"
            className="flex items-center gap-3 p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
          >
            <ShoppingCart size={24} className="text-[var(--color-primary)]" />
            <span className="font-medium text-[var(--color-secondary)]">장바구니</span>
          </Link>
          <Link
            href="/mypage/wishlist"
            className="flex items-center gap-3 p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
          >
            <Heart size={24} className="text-[var(--color-primary)]" />
            <span className="font-medium text-[var(--color-secondary)]">찜 목록</span>
          </Link>
          <Link
            href="/mypage/points"
            className="flex items-center gap-3 p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
          >
            <Coins size={24} className="text-[var(--color-primary)]" />
            <span className="font-medium text-[var(--color-secondary)]">포인트</span>
          </Link>
          <Link
            href="/mypage/coupons"
            className="flex items-center gap-3 p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
          >
            <Ticket size={24} className="text-[var(--color-primary)]" />
            <span className="font-medium text-[var(--color-secondary)]">쿠폰</span>
          </Link>
          <Link
            href="/mypage/notifications"
            className="flex items-center gap-3 p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
          >
            <Bell size={24} className="text-[var(--color-primary)]" />
            <span className="font-medium text-[var(--color-secondary)]">알림</span>
          </Link>
        </div>

        <div className="space-y-4 mb-8">
          <div className="flex justify-between py-3 border-b border-gray-100">
            <span className="text-gray-500">아이디</span>
            <span className="font-medium text-[var(--color-secondary)]">{user.username}</span>
          </div>
          <div className="flex justify-between py-3 border-b border-gray-100">
            <span className="text-gray-500">이름</span>
            <span className="font-medium text-[var(--color-secondary)]">{user.name}</span>
          </div>
          <div className="flex justify-between py-3 border-b border-gray-100">
            <span className="text-gray-500">이메일</span>
            <span className="font-medium text-[var(--color-secondary)]">{user.email}</span>
          </div>
        </div>

        <Button
          variant="outline"
          onClick={() => {
            logout();
            router.push("/");
          }}
          className="w-full"
        >
          로그아웃
        </Button>
      </div>
    </div>
  );
}
