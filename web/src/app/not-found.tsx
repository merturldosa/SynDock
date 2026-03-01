import Link from "next/link";
import { SearchX } from "lucide-react";

export default function NotFoundPage() {
  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center px-4">
      <SearchX size={64} className="text-gray-300 mb-6" />
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-2">
        페이지를 찾을 수 없습니다
      </h1>
      <p className="text-gray-500 text-center max-w-md">
        요청하신 페이지가 존재하지 않거나 이동되었을 수 있습니다.
      </p>
      <Link
        href="/"
        className="mt-6 px-6 py-2.5 bg-[var(--color-primary)] text-white rounded-lg font-medium hover:opacity-90"
      >
        홈으로 돌아가기
      </Link>
    </div>
  );
}
