"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { useTranslations } from "next-intl";
import {
  LayoutDashboard,
  Package,
  FolderTree,
  ShoppingCart,
  Ticket,
  Users,
  BarChart3,
  Warehouse,
  Mail,
  Bell,
  TrendingUp,
  Settings,
  ChevronLeft,
  Landmark,
  Factory,
  RefreshCcw,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";

const NAV_ITEMS = [
  { href: "/admin", icon: LayoutDashboard, labelKey: "admin.nav.dashboard" },
  { href: "/admin/products", icon: Package, labelKey: "admin.nav.products" },
  { href: "/admin/categories", icon: FolderTree, labelKey: "admin.nav.categories" },
  { href: "/admin/orders", icon: ShoppingCart, labelKey: "admin.nav.orders" },
  { href: "/admin/inventory", icon: Warehouse, labelKey: "admin.nav.inventory" },
  { href: "/admin/coupons", icon: Ticket, labelKey: "admin.nav.coupons" },
  { href: "/admin/users", icon: Users, labelKey: "admin.nav.users" },
  { href: "/admin/analytics", icon: BarChart3, labelKey: "admin.nav.analytics" },
  { href: "/admin/settlements", icon: Landmark, labelKey: "admin.nav.settlements" },
  { href: "/admin/forecast", icon: TrendingUp, labelKey: "admin.nav.forecast" },
  { href: "/admin/production-plan", icon: Factory, labelKey: "admin.nav.productionPlan" },
  { href: "/admin/auto-reorder", icon: RefreshCcw, labelKey: "admin.nav.autoReorder" },
  { href: "/admin/email", icon: Mail, labelKey: "admin.nav.email" },
  { href: "/admin/notifications", icon: Bell, labelKey: "admin.nav.notifications" },
  { href: "/admin/settings", icon: Settings, labelKey: "admin.nav.settings" },
] as const;

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, isAuthenticated, isLoading, fetchMe } = useAuthStore();
  const t = useTranslations();

  useEffect(() => {
    fetchMe();
  }, [fetchMe]);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      sessionStorage.setItem("returnTo", pathname);
      router.replace("/login");
    }
    if (!isLoading && isAuthenticated && user && !["Admin", "TenantAdmin", "PlatformAdmin"].includes(user.role)) {
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
            {t("admin.nav.backToShop")}
          </Link>
        </div>

        <div className="p-4 border-b border-white/10">
          <p className="text-xs text-white/40">{t("admin.nav.admin")}</p>
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
                {t(item.labelKey)}
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
