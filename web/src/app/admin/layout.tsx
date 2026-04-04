"use client";

import { useEffect, useState } from "react";
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
  Menu,
  X,
  Image,
  ExternalLink,
  ChevronDown,
  MapPin,
  ClipboardList,
  PackageCheck,
  UserSearch,
  HeadphonesIcon,
  Footprints,
  Calculator,
  UserCog,
  Banknote,
} from "lucide-react";
import api from "@/lib/api";
import { useAuthStore } from "@/stores/authStore";

type NavItem = {
  href: string;
  icon: typeof LayoutDashboard;
  labelKey: string;
  adminOnly?: boolean;
};

type NavSection = {
  titleKey: string;
  emoji: string;
  items: NavItem[];
  defaultOpen?: boolean;
};

type FeatureKey = "wms" | "crm" | "erp";

const WMS_URL = process.env.NEXT_PUBLIC_WMS_URL || "http://localhost:3500";
const CRM_URL = process.env.NEXT_PUBLIC_CRM_URL || "http://localhost:3600";
const ERP_URL = process.env.NEXT_PUBLIC_ERP_URL || "http://localhost:3700";
const SCM_URL = process.env.NEXT_PUBLIC_SCM_URL || "http://localhost:3800";

const FEATURE_NAV_SECTIONS: Record<FeatureKey, NavSection> = {
  wms: {
    titleKey: "admin.nav.sectionWMS",
    emoji: "🏭",
    defaultOpen: false,
    items: [
      { href: WMS_URL, icon: MapPin, labelKey: "admin.nav.warehouseZones" },
    ],
  },
  crm: {
    titleKey: "admin.nav.sectionCRM",
    emoji: "🤝",
    defaultOpen: false,
    items: [
      { href: CRM_URL, icon: UserSearch, labelKey: "admin.nav.crmCustomers" },
    ],
  },
  erp: {
    titleKey: "admin.nav.sectionERP",
    emoji: "🏢",
    defaultOpen: false,
    items: [
      { href: ERP_URL, icon: Calculator, labelKey: "admin.nav.accounting" },
    ],
  },
};

const NAV_SECTIONS: NavSection[] = [
  {
    titleKey: "admin.nav.sectionOperations",
    emoji: "📊",
    defaultOpen: true,
    items: [
      { href: "/admin", icon: LayoutDashboard, labelKey: "admin.nav.dashboard" },
      { href: "/admin/orders", icon: ShoppingCart, labelKey: "admin.nav.orders" },
      { href: "/admin/inventory", icon: Warehouse, labelKey: "admin.nav.inventory" },
    ],
  },
  {
    titleKey: "admin.nav.sectionProducts",
    emoji: "🛍️",
    defaultOpen: true,
    items: [
      { href: "/admin/products", icon: Package, labelKey: "admin.nav.products" },
      { href: "/admin/categories", icon: FolderTree, labelKey: "admin.nav.categories" },
      { href: "/admin/banners", icon: Image, labelKey: "admin.nav.banners" },
    ],
  },
  {
    titleKey: "admin.nav.sectionCustomers",
    emoji: "👥",
    defaultOpen: false,
    items: [
      { href: "/admin/users", icon: Users, labelKey: "admin.nav.users" },
      { href: "/admin/coupons", icon: Ticket, labelKey: "admin.nav.coupons" },
      { href: "/admin/notifications", icon: Bell, labelKey: "admin.nav.notifications" },
      { href: "/admin/email", icon: Mail, labelKey: "admin.nav.email" },
    ],
  },
  {
    titleKey: "admin.nav.sectionAnalytics",
    emoji: "📈",
    defaultOpen: false,
    items: [
      { href: "/admin/analytics", icon: BarChart3, labelKey: "admin.nav.analytics" },
      { href: "/admin/forecast", icon: TrendingUp, labelKey: "admin.nav.forecast" },
      { href: "/admin/production-plan", icon: Factory, labelKey: "admin.nav.productionPlan", adminOnly: true },
      { href: "/admin/auto-reorder", icon: RefreshCcw, labelKey: "admin.nav.autoReorder", adminOnly: true },
    ],
  },
  {
    titleKey: "admin.nav.sectionFinance",
    emoji: "💰",
    defaultOpen: false,
    items: [
      { href: "/admin/settlements", icon: Landmark, labelKey: "admin.nav.settlements", adminOnly: true },
    ],
  },
  {
    titleKey: "admin.nav.sectionSettings",
    emoji: "⚙️",
    defaultOpen: false,
    items: [
      { href: "/admin/settings", icon: Settings, labelKey: "admin.nav.settings" },
    ],
  },
];

function useSectionState(sections: NavSection[]) {
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({});

  useEffect(() => {
    setOpenSections((prev) => {
      const next: Record<string, boolean> = { ...prev };
      sections.forEach((s) => {
        if (!(s.titleKey in next)) next[s.titleKey] = s.defaultOpen ?? false;
      });
      return next;
    });
  }, [sections]);

  const toggle = (key: string) => setOpenSections((prev) => ({ ...prev, [key]: !prev[key] }));
  return { openSections, toggle };
}

function useFeatureFlags() {
  const [enabledFeatures, setEnabledFeatures] = useState<FeatureKey[]>([]);

  useEffect(() => {
    api.get("/tenant-settings/features")
      .then((res) => {
        const flags = res.data as Record<string, boolean>;
        const enabled: FeatureKey[] = [];
        if (flags.wms) enabled.push("wms");
        if (flags.crm) enabled.push("crm");
        if (flags.erp) enabled.push("erp");
        setEnabledFeatures(enabled);
      })
      .catch(() => {});
  }, []);

  return enabledFeatures;
}

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, isAuthenticated, isLoading, fetchMe } = useAuthStore();
  const t = useTranslations();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const enabledFeatures = useFeatureFlags();

  // Build combined nav sections: static + feature-gated
  const allSections = [
    ...NAV_SECTIONS,
    ...enabledFeatures.map((f) => FEATURE_NAV_SECTIONS[f]),
  ];
  const { openSections, toggle } = useSectionState(allSections);

  useEffect(() => { fetchMe(); }, [fetchMe]);

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

  const isAdminPlus = user.role === "Admin" || user.role === "PlatformAdmin";

  // Fallback for missing translation keys
  const safeT = (key: string, fallback?: string) => {
    try { return t(key); } catch { return fallback ?? key.split(".").pop() ?? key; }
  };

  return (
    <div className="flex min-h-[calc(100vh-64px)]">
      {/* Mobile hamburger */}
      <button
        onClick={() => setSidebarOpen(true)}
        className="md:hidden fixed top-[72px] left-3 z-40 p-2 bg-[var(--color-secondary)] text-white rounded-lg shadow-lg"
        aria-label="Open menu"
      >
        <Menu size={20} />
      </button>

      {/* Mobile overlay */}
      {sidebarOpen && (
        <div className="md:hidden fixed inset-0 bg-black/50 z-40" onClick={() => setSidebarOpen(false)} />
      )}

      {/* Sidebar */}
      <aside
        className={`w-60 bg-[var(--color-secondary)] text-white flex-shrink-0 fixed md:static inset-y-0 left-0 z-50 transform transition-transform duration-200 ease-in-out ${
          sidebarOpen ? "translate-x-0" : "-translate-x-full"
        } md:translate-x-0 flex flex-col`}
      >
        {/* Header */}
        <div className="p-4 border-b border-white/10 flex items-center justify-between">
          <Link href="/" className="flex items-center gap-2 text-sm text-white/60 hover:text-white">
            <ChevronLeft size={14} />
            {safeT("admin.nav.backToShop", "쇼핑몰로")}
          </Link>
          <button onClick={() => setSidebarOpen(false)} className="md:hidden text-white/60 hover:text-white" aria-label="Close menu">
            <X size={18} />
          </button>
        </div>

        {/* User info */}
        <div className="p-4 border-b border-white/10">
          <p className="text-xs text-white/40">{safeT("admin.nav.admin", "관리자")}</p>
          <p className="font-medium truncate">{user.name}</p>
          <p className="text-xs text-white/30 mt-0.5">{user.role}</p>
        </div>

        {/* Navigation sections */}
        <nav className="flex-1 overflow-y-auto p-2">
          {allSections.map((section) => {
            const visibleItems = section.items.filter((item) => !item.adminOnly || isAdminPlus);
            if (visibleItems.length === 0) return null;

            const isOpen = openSections[section.titleKey];
            const hasActive = visibleItems.some(
              (item) => pathname === item.href || (item.href !== "/admin" && pathname.startsWith(item.href))
            );

            return (
              <div key={section.titleKey} className="mb-1">
                <button
                  onClick={() => toggle(section.titleKey)}
                  className={`w-full flex items-center justify-between px-3 py-2 text-xs font-medium rounded-lg transition-colors ${
                    hasActive ? "text-white bg-white/5" : "text-white/40 hover:text-white/60"
                  }`}
                >
                  <span>{section.emoji} {safeT(section.titleKey, section.titleKey.split(".").pop())}</span>
                  <ChevronDown size={12} className={`transition-transform ${isOpen ? "rotate-180" : ""}`} />
                </button>

                {isOpen && (
                  <div className="ml-1 mt-0.5 space-y-0.5">
                    {visibleItems.map((item) => {
                      const isActive = pathname === item.href || (item.href !== "/admin" && pathname.startsWith(item.href));
                      return (
                        <Link
                          key={item.href}
                          href={item.href}
                          onClick={() => setSidebarOpen(false)}
                          className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                            isActive ? "bg-white/10 text-white" : "text-white/60 hover:bg-white/5 hover:text-white"
                          }`}
                        >
                          <item.icon size={16} />
                          {safeT(item.labelKey, item.labelKey.split(".").pop())}
                        </Link>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </nav>

        {/* Shop preview button */}
        <div className="p-3 border-t border-white/10">
          <Link
            href="/"
            target="_blank"
            className="flex items-center justify-center gap-2 w-full py-2.5 bg-white/10 hover:bg-white/20 rounded-lg text-sm font-medium transition-colors"
          >
            <ExternalLink size={14} />
            {safeT("admin.nav.viewShop", "쇼핑몰 보기")}
          </Link>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 bg-gray-50 p-6 overflow-auto">{children}</main>
    </div>
  );
}
