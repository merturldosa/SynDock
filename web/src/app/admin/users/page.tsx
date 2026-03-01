"use client";

import { useEffect, useState } from "react";
import { getAdminUsers, updateUser, type UserSummary } from "@/lib/adminApi";

export default function AdminUsersPage() {
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [updatingId, setUpdatingId] = useState<number | null>(null);

  const load = () => {
    setLoading(true);
    getAdminUsers()
      .then(setUsers)
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const handleRoleChange = async (user: UserSummary, newRole: string) => {
    if (!confirm(`${user.name}님의 역할을 ${newRole}(으)로 변경하시겠습니까?`)) return;
    setUpdatingId(user.id);
    try {
      await updateUser(user.id, newRole, user.isActive);
      load();
    } catch {
      alert("역할 변경에 실패했습니다.");
    }
    setUpdatingId(null);
  };

  const handleActiveToggle = async (user: UserSummary) => {
    const action = user.isActive ? "비활성화" : "활성화";
    if (!confirm(`${user.name}님을 ${action}하시겠습니까?`)) return;
    setUpdatingId(user.id);
    try {
      await updateUser(user.id, user.role, !user.isActive);
      load();
    } catch {
      alert("상태 변경에 실패했습니다.");
    }
    setUpdatingId(null);
  };

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
                        활성
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
