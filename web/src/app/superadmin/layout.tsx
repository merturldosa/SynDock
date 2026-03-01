"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import {
  LayoutDashboard,
  Store,
  Plus,
  CreditCard,
  ChevronLeft,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";

const NAV_ITEMS = [
  { href: "/superadmin", icon: LayoutDashboard, label: "대시보드" },
  { href: "/superadmin/tenants", icon: Store, label: "쇼핑몰 관리" },
  { href: "/superadmin/tenants/new", icon: Plus, label: "쇼핑몰 분양" },
  { href: "/superadmin/billing", icon: CreditCard, label: "빌링" },
];

export default function SuperAdminLayout({
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
    if (!isLoading && user && user.role !== "PlatformAdmin") {
      router.replace("/");
    }
  }, [isLoading, isAuthenticated, user, router, pathname]);

  if (isLoading || !user) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  if (user.role !== "PlatformAdmin") return null;

  return (
    <div className="flex min-h-[calc(100vh-64px)]">
      <aside className="w-60 bg-gray-900 text-white flex-shrink-0">
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
          <p className="text-xs text-emerald-400">Platform Admin</p>
          <p className="font-medium truncate">{user.name}</p>
        </div>

        <nav className="p-2">
          {NAV_ITEMS.map((item) => {
            const isActive =
              pathname === item.href ||
              (item.href !== "/superadmin" &&
                item.href !== "/superadmin/tenants/new" &&
                pathname.startsWith(item.href));
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

      <main className="flex-1 bg-gray-50 p-6 overflow-auto">{children}</main>
    </div>
  );
}
