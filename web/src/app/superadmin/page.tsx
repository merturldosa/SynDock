"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Store, ShoppingCart, Plus } from "lucide-react";
import { getPlatformTenants, type TenantDetail } from "@/lib/platformApi";

export default function SuperAdminDashboardPage() {
  const [tenants, setTenants] = useState<TenantDetail[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getPlatformTenants()
      .then(setTenants)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const activeTenants = tenants.filter((t) => t.isActive).length;

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">
        플랫폼 대시보드
      </h1>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <div className="bg-white rounded-xl shadow-sm p-5">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-emerald-100 rounded-lg">
              <Store size={20} className="text-emerald-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">전체 쇼핑몰</p>
              <p className="text-2xl font-bold text-gray-900">
                {tenants.length}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm p-5">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <ShoppingCart size={20} className="text-blue-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">운영 중</p>
              <p className="text-2xl font-bold text-gray-900">
                {activeTenants}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm p-5">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-orange-100 rounded-lg">
              <Store size={20} className="text-orange-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">비활성</p>
              <p className="text-2xl font-bold text-gray-900">
                {tenants.length - activeTenants}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-xl shadow-sm p-5 mb-8">
        <h2 className="font-semibold text-gray-900 mb-3">빠른 작업</h2>
        <div className="flex gap-3">
          <Link
            href="/superadmin/tenants/new"
            className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700"
          >
            <Plus size={16} /> 새 쇼핑몰 분양
          </Link>
          <Link
            href="/superadmin/tenants"
            className="flex items-center gap-2 px-4 py-2.5 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
          >
            <Store size={16} /> 쇼핑몰 목록
          </Link>
        </div>
      </div>

      {/* Recent Tenants */}
      <div className="bg-white rounded-xl shadow-sm overflow-hidden">
        <div className="p-5 border-b">
          <h2 className="font-semibold text-gray-900">최근 등록 쇼핑몰</h2>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="text-left p-3 font-medium text-gray-500">
                쇼핑몰명
              </th>
              <th className="text-left p-3 font-medium text-gray-500">Slug</th>
              <th className="text-center p-3 font-medium text-gray-500">
                상태
              </th>
              <th className="text-left p-3 font-medium text-gray-500">
                등록일
              </th>
            </tr>
          </thead>
          <tbody>
            {tenants.slice(0, 5).map((t) => (
              <tr
                key={t.id}
                className="border-b last:border-0 hover:bg-gray-50"
              >
                <td className="p-3">
                  <Link
                    href={`/superadmin/tenants/${t.slug}`}
                    className="font-medium text-gray-900 hover:text-emerald-600"
                  >
                    {t.name}
                  </Link>
                </td>
                <td className="p-3 text-gray-500">{t.slug}</td>
                <td className="p-3 text-center">
                  <span
                    className={`px-2 py-0.5 text-xs rounded-full ${
                      t.isActive
                        ? "bg-emerald-100 text-emerald-700"
                        : "bg-gray-100 text-gray-500"
                    }`}
                  >
                    {t.isActive ? "운영 중" : "비활성"}
                  </span>
                </td>
                <td className="p-3 text-gray-500">
                  {new Date(t.createdAt).toLocaleDateString("ko-KR")}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
