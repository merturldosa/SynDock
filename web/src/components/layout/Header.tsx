"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import { useTenantStore } from "@/stores/tenantStore";
import { useEffect, useState } from "react";
import { Menu, X, ShoppingCart, User, Search, Bell, MessageCircle } from "lucide-react";
import { getCategories } from "@/lib/productApi";
import { getUnreadCount } from "@/lib/notificationApi";
import type { CategoryInfo } from "@/types/product";

export function Header() {
  const router = useRouter();
  const { user, isAuthenticated, isLoading, logout, fetchMe } = useAuthStore();
  const { name: tenantName } = useTenantStore();
  const { cart, fetchCart } = useCartStore();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [categories, setCategories] = useState<CategoryInfo[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    fetchMe();
  }, [fetchMe]);

  useEffect(() => {
    if (isAuthenticated) {
      fetchCart();
      getUnreadCount().then(r => setUnreadCount(r.count)).catch(() => {});
    }
  }, [isAuthenticated, fetchCart]);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => {});
  }, []);

  const handleLogout = () => {
    logout();
    setMobileOpen(false);
    router.push("/");
  };

  // Show top 6 categories in desktop nav
  const navCategories = categories.slice(0, 6);

  return (
    <header className="bg-[var(--color-secondary)] text-white sticky top-0 z-50">
      {/* Top bar */}
      <div className="border-b border-white/10">
        <div className="max-w-7xl mx-auto px-4 h-8 flex items-center justify-end text-xs text-white/50 gap-4">
          {!isLoading && isAuthenticated ? (
            <>
              <span className="text-[var(--color-primary-light)]">{user?.name}님</span>
              <button onClick={handleLogout} className="hover:text-white transition-colors">
                로그아웃
              </button>
            </>
          ) : !isLoading ? (
            <>
              <Link href="/login" className="hover:text-white transition-colors">로그인</Link>
              <Link href="/register" className="hover:text-white transition-colors">회원가입</Link>
            </>
          ) : null}
        </div>
      </div>

      {/* Main header */}
      <div className="max-w-7xl mx-auto px-4 h-14 flex items-center justify-between">
        <Link href="/" className="text-xl font-bold text-[var(--color-primary)]">
          {tenantName || "Shop"}
        </Link>

        {/* Desktop Nav */}
        <nav className="hidden md:flex items-center gap-8">
          <Link href="/products" className="text-sm hover:text-[var(--color-primary)] transition-colors">
            전체상품
          </Link>
          {navCategories.map((cat) => (
            <Link
              key={cat.id}
              href={`/products?category=${cat.slug}`}
              className="text-sm hover:text-[var(--color-primary)] transition-colors"
            >
              {cat.name}
            </Link>
          ))}
        </nav>

        {/* Right icons */}
        <div className="flex items-center gap-3">
          <Link href="/feed" className="hidden md:block text-xs px-2 py-1 hover:text-[var(--color-primary)] transition-colors">
            커뮤니티
          </Link>
          <Link href="/liturgy" className="hidden md:block text-xs px-2 py-1 hover:text-[var(--color-primary)] transition-colors">
            전례
          </Link>
          <Link href="/search" className="hidden md:block p-2 hover:text-[var(--color-primary)] transition-colors">
            <Search size={20} />
          </Link>
          <Link href="/chat" className="hidden md:block p-2 hover:text-[var(--color-primary)] transition-colors" title="AI 채팅">
            <MessageCircle size={20} />
          </Link>
          {isAuthenticated && (
            <>
              <Link href="/mypage/notifications" className="hidden md:block relative p-2 hover:text-[var(--color-primary)] transition-colors">
                <Bell size={20} />
                {unreadCount > 0 && (
                  <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] flex items-center justify-center text-[10px] font-bold bg-red-500 text-white rounded-full px-1">
                    {unreadCount > 99 ? "99+" : unreadCount}
                  </span>
                )}
              </Link>
              <Link href="/mypage" className="hidden md:block p-2 hover:text-[var(--color-primary)] transition-colors">
                <User size={20} />
              </Link>
              {(user?.role === "Admin" || user?.role === "PlatformAdmin") && (
                <Link href="/admin" className="hidden md:block text-xs px-2 py-1 bg-[var(--color-primary)] rounded text-white hover:opacity-90">
                  관리자
                </Link>
              )}
              {user?.role === "PlatformAdmin" && (
                <Link href="/superadmin" className="hidden md:block text-xs px-2 py-1 bg-emerald-600 rounded text-white hover:bg-emerald-700">
                  플랫폼
                </Link>
              )}
            </>
          )}
          <Link href="/cart" className="relative p-2 hover:text-[var(--color-primary)] transition-colors">
            <ShoppingCart size={20} />
            {(cart?.totalQuantity ?? 0) > 0 && (
              <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] flex items-center justify-center text-[10px] font-bold bg-[var(--color-primary)] text-white rounded-full px-1">
                {cart!.totalQuantity > 99 ? "99+" : cart!.totalQuantity}
              </span>
            )}
          </Link>

          {/* Mobile menu */}
          <button className="md:hidden p-2" onClick={() => setMobileOpen(!mobileOpen)}>
            {mobileOpen ? <X size={22} /> : <Menu size={22} />}
          </button>
        </div>
      </div>

      {/* Mobile Nav */}
      {mobileOpen && (
        <nav className="md:hidden bg-[var(--color-secondary-light)] border-t border-white/10">
          <div className="px-4 py-4 flex flex-col gap-1">
            <Link
              href="/products"
              onClick={() => setMobileOpen(false)}
              className="py-2 px-2 rounded hover:bg-white/5 hover:text-[var(--color-primary)] transition-colors"
            >
              전체상품
            </Link>
            {categories.map((cat) => (
              <Link
                key={cat.id}
                href={`/products?category=${cat.slug}`}
                onClick={() => setMobileOpen(false)}
                className="py-2 px-2 rounded hover:bg-white/5 hover:text-[var(--color-primary)] transition-colors"
              >
                {cat.name}
              </Link>
            ))}
          </div>
        </nav>
      )}
    </header>
  );
}
