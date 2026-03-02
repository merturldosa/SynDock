"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { useTranslations } from "next-intl";
import { getTenantUsage, type TenantUsage } from "@/lib/platformApi";

function formatBytes(bytes: number): string {
  if (bytes === 0) return "0 B";
  const units = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${units[i]}`;
}

// Note: formatLimit is called inside UsageBar which doesn't have access to t
// We'll pass the unlimited label as a prop
function formatLimit(value: number, unlimitedLabel: string = "Unlimited"): string {
  if (value >= Number.MAX_SAFE_INTEGER || value >= 2_147_483_647) return unlimitedLabel;
  return value.toLocaleString("ko-KR");
}

function UsageBar({ current, max, label, formatValue, unlimitedLabel, usedLabel }: {
  current: number;
  max: number;
  label: string;
  formatValue?: (v: number) => string;
  unlimitedLabel: string;
  usedLabel: string;
}) {
  const isUnlimited = max >= Number.MAX_SAFE_INTEGER || max >= 2_147_483_647;
  const percentage = isUnlimited ? 0 : Math.min((current / max) * 100, 100);
  const fmt = formatValue || ((v: number) => v.toLocaleString("ko-KR"));

  let barColor = "bg-emerald-500";
  if (percentage >= 90) barColor = "bg-red-500";
  else if (percentage >= 70) barColor = "bg-yellow-500";

  return (
    <div className="bg-white rounded-xl shadow-sm p-5">
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-sm font-medium text-gray-700">{label}</h3>
        <span className="text-sm text-gray-500">
          {fmt(current)} / {isUnlimited ? unlimitedLabel : fmt(max)}
        </span>
      </div>
      <div className="w-full bg-gray-200 rounded-full h-3">
        <div
          className={`h-3 rounded-full transition-all ${barColor}`}
          style={{ width: `${isUnlimited ? 0 : percentage}%` }}
        />
      </div>
      {!isUnlimited && (
        <p className="text-xs text-gray-400 mt-1 text-right">
          {usedLabel}
        </p>
      )}
    </div>
  );
}

export default function TenantUsagePage() {
  const t = useTranslations();
  const params = useParams();
  const router = useRouter();
  const slug = params.slug as string;

  const [usage, setUsage] = useState<TenantUsage | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getTenantUsage(slug)
      .then(setUsage)
      .catch(() => router.push(`/superadmin/tenants/${slug}`))
      .finally(() => setLoading(false));
  }, [slug, router]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
      </div>
    );
  }

  if (!usage) return null;

  const planColors: Record<string, string> = {
    Free: "bg-gray-100 text-gray-700",
    Basic: "bg-blue-100 text-blue-700",
    Pro: "bg-purple-100 text-purple-700",
    Enterprise: "bg-emerald-100 text-emerald-700",
  };

  return (
    <div className="max-w-3xl">
      <Link
        href={`/superadmin/tenants/${slug}`}
        className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ArrowLeft size={14} /> {t("superadmin.usage.backToDetail")}
      </Link>

      <div className="flex items-center gap-3 mb-6">
        <h1 className="text-2xl font-bold text-gray-900">
          {t("superadmin.usage.title")}: {usage.tenantName}
        </h1>
        <span className={`text-xs px-2.5 py-1 rounded-full font-semibold ${planColors[usage.planType] || planColors.Free}`}>
          {usage.planType}
        </span>
      </div>

      <p className="text-sm text-gray-500 mb-6">
        {t("superadmin.usage.billingPeriod")}: {usage.currentPeriod}
      </p>

      <div className="grid gap-4">
        <UsageBar
          label={t("superadmin.usage.products")}
          current={usage.productCount}
          max={usage.maxProducts}
          unlimitedLabel={t("superadmin.usage.unlimited")}
          usedLabel={t("superadmin.usage.used", { percent: (Math.min((usage.productCount / usage.maxProducts) * 100, 100)).toFixed(1) })}
        />
        <UsageBar
          label={t("superadmin.usage.users")}
          current={usage.userCount}
          max={usage.maxUsers}
          unlimitedLabel={t("superadmin.usage.unlimited")}
          usedLabel={t("superadmin.usage.used", { percent: (Math.min((usage.userCount / usage.maxUsers) * 100, 100)).toFixed(1) })}
        />
        <UsageBar
          label={t("superadmin.usage.orders")}
          current={usage.monthlyOrderCount}
          max={usage.maxMonthlyOrders}
          unlimitedLabel={t("superadmin.usage.unlimited")}
          usedLabel={t("superadmin.usage.used", { percent: (Math.min((usage.monthlyOrderCount / usage.maxMonthlyOrders) * 100, 100)).toFixed(1) })}
        />
        <UsageBar
          label={t("superadmin.usage.storage")}
          current={usage.storageUsedBytes}
          max={usage.maxStorageBytes}
          formatValue={formatBytes}
          unlimitedLabel={t("superadmin.usage.unlimited")}
          usedLabel={t("superadmin.usage.used", { percent: (Math.min((usage.storageUsedBytes / usage.maxStorageBytes) * 100, 100)).toFixed(1) })}
        />
      </div>

      {/* Plan Comparison Table */}
      <div className="mt-8 bg-white rounded-xl shadow-sm p-5">
        <h2 className="font-semibold text-gray-900 mb-4">{t("superadmin.usage.planComparison")}</h2>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left text-gray-500">
              <th className="pb-2 font-medium">{t("superadmin.usage.item")}</th>
              <th className="pb-2 font-medium">Free</th>
              <th className="pb-2 font-medium">Basic</th>
              <th className="pb-2 font-medium">Pro</th>
              <th className="pb-2 font-medium">Enterprise</th>
            </tr>
          </thead>
          <tbody className="text-gray-700">
            <tr className="border-b">
              <td className="py-2">{t("superadmin.usage.products")}</td>
              <td>20</td>
              <td>200</td>
              <td>2,000</td>
              <td>{t("superadmin.usage.unlimited")}</td>
            </tr>
            <tr className="border-b">
              <td className="py-2">{t("superadmin.usage.users")}</td>
              <td>50</td>
              <td>500</td>
              <td>5,000</td>
              <td>{t("superadmin.usage.unlimited")}</td>
            </tr>
            <tr className="border-b">
              <td className="py-2">{t("superadmin.usage.monthlyOrders")}</td>
              <td>100</td>
              <td>1,000</td>
              <td>10,000</td>
              <td>{t("superadmin.usage.unlimited")}</td>
            </tr>
            <tr>
              <td className="py-2">{t("superadmin.usage.storage")}</td>
              <td>1 GB</td>
              <td>5 GB</td>
              <td>20 GB</td>
              <td>{t("superadmin.usage.unlimited")}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}
