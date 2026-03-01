"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import {
  LayoutDashboard,
  Package,
  FolderTree,
  ShoppingCart,
  Ticket,
  Users,
  BarChart3,
  Warehouse,
  ChevronLeft,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";

const NAV_ITEMS = [
  { href: "/admin", icon: LayoutDashboard, label: "대시보드" },
  { href: "/admin/products", icon: Package, label: "상품 관리" },
  { href: "/admin/categories", icon: FolderTree, label: "카테고리 관리" },
  { href: "/admin/orders", icon: ShoppingCart, label: "주문 관리" },
  { href: "/admin/inventory", icon: Warehouse, label: "재고 관리" },
  { href: "/admin/coupons", icon: Ticket, label: "쿠폰 관리" },
  { href: "/admin/users", icon: Users, label: "회원 관리" },
  { href: "/admin/analytics", icon: BarChart3, label: "매출 분석" },
];

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
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

  if (isLoading || !user) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="flex min-h-[calc(100vh-64px)]">
      {/* Sidebar */}
      <aside className="w-60 bg-[var(--color-secondary)] text-white flex-shrink-0">
        <div className="p-4 border-b border-white/10">
          <Link
            href="/"
            className="flex items-center gap-2 text-sm text-white/60 hover:text-white"
          >
            <ChevronLeft size={14} />
            쇼핑몰로 돌아가기
          </Link>
        </div>

        <div className="p-4 border-b border-white/10">
          <p className="text-xs text-white/40">관리자</p>
          <p className="font-medium truncate">{user.name}</p>
        </div>

        <nav className="p-2">
          {NAV_ITEMS.map((item) => {
            const isActive =
              pathname === item.href ||
              (item.href !== "/admin" && pathname.startsWith(item.href));
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors ${
                  isActive
                    ? "bg-white/10 text-white"
                    : "text-white/60 hover:bg-white/5 hover:text-white"
                }`}
              >
                <item.icon size={18} />
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>

      {/* Main content */}
      <main className="flex-1 bg-gray-50 p-6 overflow-auto">{children}</main>
    </div>
  );
}
