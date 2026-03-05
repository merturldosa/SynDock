"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import { useTenantStore } from "@/stores/tenantStore";
import { useEffect, useState } from "react";
import { Menu, X, ShoppingCart, User, Search, Bell, MessageCircle } from "lucide-react";
import toast from "react-hot-toast";
import { getCategories } from "@/lib/productApi";
import { getUnreadCount } from "@/lib/notificationApi";
import { useNotificationStore } from "@/stores/notificationStore";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { useTranslations } from "next-intl";
import type { CategoryInfo } from "@/types/product";

export function Header() {
  const t = useTranslations();
  const router = useRouter();
  const { user, isAuthenticated, isLoading, logout, fetchMe } = useAuthStore();
  const { name: tenantName } = useTenantStore();
  const { cart, fetchCart } = useCartStore();
  const { unreadCount, setUnreadCount, connect, disconnect } = useNotificationStore();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [categories, setCategories] = useState<CategoryInfo[]>([]);

  useEffect(() => {
    fetchMe();
  }, [fetchMe]);

  useEffect(() => {
    if (isAuthenticated) {
      fetchCart();
      getUnreadCount().then(r => setUnreadCount(r.count)).catch(() => toast.error(t("common.fetchError")));
      const token = localStorage.getItem("accessToken");
      if (token) connect(token);
    } else {
      disconnect();
    }
  }, [isAuthenticated, fetchCart, connect, disconnect, setUnreadCount]);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => toast.error(t("common.fetchError")));
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
          <LanguageSwitcher />
          {!isLoading && isAuthenticated ? (
            <>
              <span className="text-[var(--color-primary-light)]">{t("header.greeting", { name: user?.name ?? "" })}</span>
              <button onClick={handleLogout} className="hover:text-white transition-colors">
                {t("header.logout")}
              </button>
            </>
          ) : !isLoading ? (
            <>
              <Link href="/login" className="hover:text-white transition-colors">{t("header.login")}</Link>
              <Link href="/register" className="hover:text-white transition-colors">{t("header.register")}</Link>
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
            {t("common.allProducts")}
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
            {t("common.community")}
          </Link>
          <Link href="/liturgy" className="hidden md:block text-xs px-2 py-1 hover:text-[var(--color-primary)] transition-colors">
            {t("common.liturgy")}
          </Link>
          <Link href="/search" className="hidden md:block p-2 hover:text-[var(--color-primary)] transition-colors">
            <Search size={20} />
          </Link>
          <Link href="/chat" className="hidden md:block p-2 hover:text-[var(--color-primary)] transition-colors" title={t("common.aiChat")}>
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
                  {t("header.admin")}
                </Link>
              )}
              {user?.role === "PlatformAdmin" && (
                <Link href="/superadmin" className="hidden md:block text-xs px-2 py-1 bg-emerald-600 rounded text-white hover:bg-emerald-700">
                  {t("header.platform")}
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
          <button className="md:hidden p-2" onClick={() => setMobileOpen(!mobileOpen)} aria-label="Toggle menu" aria-expanded={mobileOpen}>
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
              {t("common.allProducts")}
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
