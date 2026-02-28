"use client";

import { useEffect, useState } from "react";
import { getAdminUsers, type UserSummary } from "@/lib/adminApi";

export default function AdminUsersPage() {
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getAdminUsers()
      .then(setUsers)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <div>
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        회원 관리
      </h1>

      {users.length > 0 && (
        <p className="text-sm text-gray-500 mb-3">
          총 {users.length}명
        </p>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      ) : users.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p>등록된 회원이 없습니다.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left p-3 font-medium text-gray-500">이름</th>
                <th className="text-left p-3 font-medium text-gray-500">아이디</th>
                <th className="text-left p-3 font-medium text-gray-500">이메일</th>
                <th className="text-center p-3 font-medium text-gray-500">역할</th>
                <th className="text-center p-3 font-medium text-gray-500">상태</th>
                <th className="text-left p-3 font-medium text-gray-500">가입일</th>
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
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full ${
                        user.role === "PlatformAdmin"
                          ? "bg-emerald-100 text-emerald-700"
                          : user.role === "Admin"
                          ? "bg-blue-100 text-blue-700"
                          : "bg-gray-100 text-gray-600"
                      }`}
                    >
                      {user.role}
                    </span>
                  </td>
                  <td className="p-3 text-center">
                    <span
                      className={`px-2 py-0.5 text-xs rounded-full ${
                        user.isActive
                          ? "bg-emerald-100 text-emerald-700"
                          : "bg-red-100 text-red-700"
                      }`}
                    >
                      {user.isActive ? "활성" : "비활성"}
                    </span>
                  </td>
                  <td className="p-3 text-gray-500 text-xs">
                    {new Date(user.createdAt).toLocaleDateString("ko-KR")}
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
