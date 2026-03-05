"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Plus, Edit2, Store } from "lucide-react";
import { useTranslations } from "next-intl";
import { getPlatformTenants, type TenantDetail } from "@/lib/platformApi";
import { formatDateShort } from "@/lib/format";

export default function SuperAdminTenantsPage() {
  const t = useTranslations();
  const [tenants, setTenants] = useState<TenantDetail[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getPlatformTenants()
      .then(setTenants)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">{t("superadmin.tenants.title")}</h1>
        <Link
          href="/superadmin/tenants/new"
          className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700"
        >
          <Plus size={16} /> {t("superadmin.tenants.addNew")}
        </Link>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
        </div>
      ) : tenants.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <Store size={48} className="mx-auto mb-3 opacity-30" />
          <p>{t("superadmin.tenants.noTenants")}</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3 font-medium text-gray-500">
                  {t("superadmin.tenants.name")}
                </th>
                <th className="text-left p-3 font-medium text-gray-500">
                  Slug
                </th>
                <th className="text-left p-3 font-medium text-gray-500">
                  {t("superadmin.tenants.domain")}
                </th>
                <th className="text-center p-3 font-medium text-gray-500">
                  {t("superadmin.tenants.status")}
                </th>
                <th className="text-left p-3 font-medium text-gray-500">
                  {t("superadmin.tenants.createdAt")}
                </th>
                <th className="text-center p-3 font-medium text-gray-500">
                  {t("superadmin.tenants.manage")}
                </th>
              </tr>
            </thead>
            <tbody>
              {tenants.map((tenant) => (
                <tr
                  key={tenant.id}
                  className="border-b last:border-0 hover:bg-gray-50"
                >
                  <td className="p-3 font-medium text-gray-900">{tenant.name}</td>
                  <td className="p-3 text-gray-500">{tenant.slug}</td>
                  <td className="p-3 text-gray-500">
                    {tenant.customDomain || tenant.subdomain || "-"}
                  </td>
                  <td className="p-3 text-center">
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full ${
                        tenant.isActive
                          ? "bg-emerald-100 text-emerald-700"
                          : "bg-gray-100 text-gray-500"
                      }`}
                    >
                      {tenant.isActive ? t("superadmin.tenants.active") : t("superadmin.tenants.inactive")}
                    </span>
                  </td>
                  <td className="p-3 text-gray-500">
                    {formatDateShort(tenant.createdAt)}
                  </td>
                  <td className="p-3 text-center">
                    <Link
                      href={`/superadmin/tenants/${tenant.slug}`}
                      className="inline-flex items-center gap-1 text-gray-400 hover:text-emerald-600"
                    >
                      <Edit2 size={16} />
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
