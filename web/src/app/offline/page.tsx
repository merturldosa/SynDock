"use client";

import { WifiOff } from "lucide-react";

export default function OfflinePage() {
  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center px-4">
      <WifiOff size={64} className="text-gray-300 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-2">
        오프라인 상태
      </h1>
      <p className="text-gray-500 text-center max-w-md">
        인터넷 연결이 끊어졌습니다. 네트워크를 확인하고 다시 시도해주세요.
      </p>
      <button
        onClick={() => window.location.reload()}
        className="mt-6 px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
      >
        다시 시도
      </button>
    </div>
  );
}
