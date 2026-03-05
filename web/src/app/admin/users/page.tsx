"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { getAdminUsers, updateUser, type UserSummary } from "@/lib/adminApi";
import toast from "react-hot-toast";
import { formatDateShort } from "@/lib/format";

export default function AdminUsersPage() {
  const t = useTranslations();
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [updatingId, setUpdatingId] = useState<number | null>(null);

  const load = () => {
    setLoading(true);
    getAdminUsers()
      .then(setUsers)
      .catch(() => toast.error(t("common.fetchError")))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const handleRoleChange = async (user: UserSummary, newRole: string) => {
    if (!confirm(t("admin.users.roleChangeConfirm", { name: user.name, role: newRole }))) return;
    setUpdatingId(user.id);
    try {
      await updateUser(user.id, newRole, user.isActive);
      load();
    } catch {
      alert(t("admin.users.roleChangeFailed"));
    }
    setUpdatingId(null);
  };

  const handleActiveToggle = async (user: UserSummary) => {
    const msg = user.isActive
      ? t("admin.users.deactivateConfirm", { name: user.name })
      : t("admin.users.activateConfirm", { name: user.name });
    if (!confirm(msg)) return;
    setUpdatingId(user.id);
    try {
      await updateUser(user.id, user.role, !user.isActive);
      load();
    } catch {
      alert(t("admin.users.statusChangeFailed"));
    }
    setUpdatingId(null);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("admin.users.title")}
      </h1>

      {users.length > 0 && (
        <p className="text-sm text-gray-500 mb-3">
          {t("admin.users.totalCount", { count: users.length })}
        </p>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : users.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>{t("admin.users.noUsersRegistered")}</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.users.name")}</th>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.users.username")}</th>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.users.email")}</th>
                <th className="text-center p-3 font-medium text-gray-500">{t("admin.users.role")}</th>
                <th className="text-center p-3 font-medium text-gray-500">{t("admin.users.status")}</th>
                <th className="text-left p-3 font-medium text-gray-500">{t("admin.users.joinDate")}</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id} className="border-b last:border-0 hover:bg-gray-50">
                  <td className="p-3 font-medium text-[var(--color-secondary)]">
                    {user.name}
                  </td>
                  <td className="p-3 text-gray-500">{user.username}</td>
                  <td className="p-3 text-gray-500">{user.email}</td>
                  <td className="p-3 text-center">
                    {user.role === "PlatformAdmin" ? (
                      <span className="px-2 py-0.5 text-xs rounded-full bg-emerald-100 text-emerald-700">
                        PlatformAdmin
                      </span>
                    ) : (
                      <select
                        value={user.role}
                        onChange={(e) => handleRoleChange(user, e.target.value)}
                        disabled={updatingId === user.id}
                        className="text-xs border rounded-lg px-2 py-1 bg-white disabled:opacity-50"
                      >
                        <option value="Member">Member</option>
                        <option value="Admin">Admin</option>
                      </select>
                    )}
                  </td>
                  <td className="p-3 text-center">
                    {user.role === "PlatformAdmin" ? (
                      <span className="px-2 py-0.5 text-xs rounded-full bg-emerald-100 text-emerald-700">
                        {t("admin.users.active")}
                      </span>
                    ) : (
                      <button
                        onClick={() => handleActiveToggle(user)}
                        disabled={updatingId === user.id}
                        className={`relative inline-flex h-5 w-9 items-center rounded-full transition-colors disabled:opacity-50 ${
                          user.isActive ? "bg-emerald-500" : "bg-gray-300"
                        }`}
                      >
                        <span
                          className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white transition-transform ${
                            user.isActive ? "translate-x-4.5" : "translate-x-0.5"
                          }`}
                        />
                      </button>
                    )}
                  </td>
                  <td className="p-3 text-gray-500 text-xs">
                    {formatDateShort(user.createdAt)}
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
