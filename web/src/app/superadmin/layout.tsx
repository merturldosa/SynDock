"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import {
  LayoutDashboard,
  Store,
  Plus,
  CreditCard,
  Banknote,
  ChevronLeft,
  Menu,
  X,
} from "lucide-react";
import { useTranslations } from "next-intl";
import { useAuthStore } from "@/stores/authStore";

const NAV_ITEMS = [
  { href: "/superadmin", icon: LayoutDashboard, labelKey: "dashboard" },
  { href: "/superadmin/tenants", icon: Store, labelKey: "tenants" },
  { href: "/superadmin/tenants/new", icon: Plus, labelKey: "newTenant" },
  { href: "/superadmin/billing", icon: CreditCard, labelKey: "billing" },
  { href: "/superadmin/settlements", icon: Banknote, labelKey: "settlements" },
];

export default function SuperAdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const t = useTranslations();
  const router = useRouter();
  const pathname = usePathname();
  const { user, isAuthenticated, isLoading, fetchMe } = useAuthStore();
  const [sidebarOpen, setSidebarOpen] = useState(false);

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
      {/* Mobile hamburger button */}
      <button
        onClick={() => setSidebarOpen(true)}
        className="md:hidden fixed top-[72px] left-3 z-40 p-2 bg-gray-900 text-white rounded-lg shadow-lg"
        aria-label="Open menu"
      >
        <Menu size={20} />
      </button>

      {/* Mobile overlay */}
      {sidebarOpen && (
        <div
          className="md:hidden fixed inset-0 bg-black/50 z-40"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside
        className={`w-60 bg-gray-900 text-white flex-shrink-0 fixed md:static inset-y-0 left-0 z-50 transform transition-transform duration-200 ease-in-out ${
          sidebarOpen ? "translate-x-0" : "-translate-x-full"
        } md:translate-x-0`}
      >
        <div className="p-4 border-b border-white/10 flex items-center justify-between">
          <Link
            href="/"
            className="flex items-center gap-2 text-sm text-white/60 hover:text-white"
          >
            <ChevronLeft size={14} />
            {t("superadmin.backToShop")}
          </Link>
          <button
            onClick={() => setSidebarOpen(false)}
            className="md:hidden text-white/60 hover:text-white"
            aria-label="Close menu"
          >
            <X size={18} />
          </button>
        </div>

        <div className="p-4 border-b border-white/10">
          <p className="text-xs text-emerald-400">Platform Admin</p>
          <p className="font-medium truncate">{user.name}</p>
        </div>

        <nav className="p-2 overflow-y-auto max-h-[calc(100vh-140px)]">
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
                onClick={() => setSidebarOpen(false)}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors ${
                  isActive
                    ? "bg-white/10 text-white"
                    : "text-white/60 hover:bg-white/5 hover:text-white"
                }`}
              >
                <item.icon size={18} />
                {t(`superadmin.nav.${item.labelKey}`)}
              </Link>
            );
          })}
        </nav>
      </aside>

      <main className="flex-1 bg-gray-50 p-6 overflow-auto md:ml-0 ml-0">{children}</main>
    </div>
  );
}
