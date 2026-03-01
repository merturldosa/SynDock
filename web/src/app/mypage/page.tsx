"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Package, Coins, Ticket, Bell, Cross } from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { Button } from "@/components/ui/Button";
import { updateBaptismalName } from "@/lib/saintApi";
import { getOrders } from "@/lib/orderApi";
import { getPointBalance } from "@/lib/pointApi";
import { getMyCoupons } from "@/lib/couponApi";
import { getUnreadCount } from "@/lib/notificationApi";
import type { BaptismalNameInfo } from "@/types/saint";

export default function MyPage() {
  const router = useRouter();
  const { user, logout } = useAuthStore();
  const [baptismalName, setBaptismalName] = useState("");
  const [baptismalInfo, setBaptismalInfo] = useState<BaptismalNameInfo | null>(null);
  const [savingBaptismal, setSavingBaptismal] = useState(false);
  const [recentOrderCount, setRecentOrderCount] = useState(0);
  const [pointBalance, setPointBalance] = useState<number | null>(null);
  const [couponCount, setCouponCount] = useState<number | null>(null);
  const [unreadNotifications, setUnreadNotifications] = useState<number | null>(null);

  useEffect(() => {
    if (user?.customFieldsJson) {
      try {
        const fields = JSON.parse(user.customFieldsJson);
        if (fields.baptismalName) {
          setBaptismalName(fields.baptismalName);
          setBaptismalInfo({
            baptismalName: fields.baptismalName,
            patronSaintId: fields.patronSaintId || null,
            patronSaint: null,
          });
        }
      } catch {}
    }
  }, [user]);

  useEffect(() => {
    getOrders({ page: 1, pageSize: 1 })
      .then((res) => setRecentOrderCount(res.totalCount))
      .catch(() => {});
    getPointBalance()
      .then((res) => setPointBalance(res.balance))
      .catch(() => {});
    getMyCoupons()
      .then((coupons) => setCouponCount(coupons.filter((c) => !c.isUsed).length))
      .catch(() => {});
    getUnreadCount()
      .then((res) => setUnreadNotifications(res.count))
      .catch(() => {});
  }, []);

  const handleSaveBaptismalName = async () => {
    if (!baptismalName.trim()) return;
    setSavingBaptismal(true);
    try {
      const result = await updateBaptismalName(baptismalName.trim());
      setBaptismalInfo(result);
    } catch {}
    setSavingBaptismal(false);
  };

  if (!user) return null;

  return (
    <div className="space-y-6">
      {/* Summary cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <Package size={24} className="mx-auto text-[var(--color-primary)] mb-2" />
          <p className="text-2xl font-bold text-[var(--color-secondary)]">{recentOrderCount}</p>
          <p className="text-xs text-gray-500">주문</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <Coins size={24} className="mx-auto text-[var(--color-primary)] mb-2" />
          <p className="text-2xl font-bold text-[var(--color-secondary)]">
            {pointBalance !== null ? pointBalance.toLocaleString("ko-KR") : "-"}
          </p>
          <p className="text-xs text-gray-500">포인트</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <Ticket size={24} className="mx-auto text-[var(--color-primary)] mb-2" />
          <p className="text-2xl font-bold text-[var(--color-secondary)]">
            {couponCount !== null ? couponCount.toLocaleString("ko-KR") : "-"}
          </p>
          <p className="text-xs text-gray-500">쿠폰</p>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4 text-center">
          <Bell size={24} className="mx-auto text-[var(--color-primary)] mb-2" />
          <p className="text-2xl font-bold text-[var(--color-secondary)]">
            {unreadNotifications !== null ? unreadNotifications.toLocaleString("ko-KR") : "-"}
          </p>
          <p className="text-xs text-gray-500">알림</p>
        </div>
      </div>

      {/* User info */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <h2 className="font-semibold text-[var(--color-secondary)] mb-4">내 정보</h2>
        <div className="space-y-3">
          <div className="flex justify-between py-2 border-b border-gray-100">
            <span className="text-gray-500 text-sm">아이디</span>
            <span className="font-medium text-sm text-[var(--color-secondary)]">{user.username}</span>
          </div>
          <div className="flex justify-between py-2 border-b border-gray-100">
            <span className="text-gray-500 text-sm">이름</span>
            <span className="font-medium text-sm text-[var(--color-secondary)]">{user.name}</span>
          </div>
          <div className="flex justify-between py-2 border-b border-gray-100">
            <span className="text-gray-500 text-sm">이메일</span>
            <span className="font-medium text-sm text-[var(--color-secondary)]">{user.email}</span>
          </div>
        </div>
      </div>

      {/* Baptismal Name Section */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <h2 className="font-semibold text-[var(--color-secondary)] mb-3 flex items-center gap-2">
          <Cross size={18} className="text-[var(--color-primary)]" />
          세례명 / 수호성인
        </h2>
        {baptismalInfo?.baptismalName ? (
          <div className="space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-500 text-sm">세례명</span>
              <span className="font-medium text-[var(--color-secondary)]">{baptismalInfo.baptismalName}</span>
            </div>
            {baptismalInfo.patronSaint && (
              <>
                <div className="flex justify-between">
                  <span className="text-gray-500 text-sm">수호성인</span>
                  <span className="font-medium text-[var(--color-secondary)]">{baptismalInfo.patronSaint.koreanName}</span>
                </div>
                {baptismalInfo.patronSaint.feastDay && (
                  <div className="flex justify-between">
                    <span className="text-gray-500 text-sm">축일</span>
                    <span className="text-sm text-gray-600">
                      {new Date(baptismalInfo.patronSaint.feastDay).toLocaleDateString("ko-KR", { month: "long", day: "numeric" })}
                    </span>
                  </div>
                )}
                {baptismalInfo.patronSaint.patronage && (
                  <div className="flex justify-between">
                    <span className="text-gray-500 text-sm">수호 분야</span>
                    <span className="text-sm text-gray-600">{baptismalInfo.patronSaint.patronage}</span>
                  </div>
                )}
              </>
            )}
            <button
              onClick={() => setBaptismalInfo(null)}
              className="text-xs text-[var(--color-primary)] hover:underline mt-1"
            >
              변경하기
            </button>
          </div>
        ) : (
          <div className="flex gap-2">
            <input
              type="text"
              value={baptismalName}
              onChange={(e) => setBaptismalName(e.target.value)}
              placeholder="세례명을 입력하세요 (예: 베드로)"
              className="flex-1 px-3 py-2 border rounded-lg text-sm"
            />
            <button
              onClick={handleSaveBaptismalName}
              disabled={savingBaptismal || !baptismalName.trim()}
              className="px-4 py-2 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-50"
            >
              {savingBaptismal ? "저장 중..." : "저장"}
            </button>
          </div>
        )}
      </div>

      <Button
        variant="outline"
        onClick={() => {
          logout();
          router.push("/");
        }}
        className="w-full"
      >
        로그아웃
      </Button>
    </div>
  );
}
