"use client";

import { useEffect, useState, Suspense, use } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/stores/authStore";
import { oauthLogin } from "@/lib/oauthApi";

function OAuthCallbackContent({ provider }: { provider: string }) {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { login } = useAuthStore();
  const [error, setError] = useState("");
  const [processing, setProcessing] = useState(true);

  useEffect(() => {
    const code = searchParams.get("code");
    if (!code) {
      setError("인증 코드가 없습니다.");
      setProcessing(false);
      return;
    }

    const redirectUri = `${window.location.origin}/auth/callback/${provider}`;

    oauthLogin(provider, code, redirectUri)
      .then((res) => {
        login(res.accessToken, res.refreshToken, res.user);
        router.replace("/");
      })
      .catch((err) => {
        const axiosErr = err as { response?: { data?: { error?: string } } };
        setError(axiosErr.response?.data?.error || "소셜 로그인에 실패했습니다.");
        setProcessing(false);
      });
  }, [searchParams, provider, login, router]);

  if (processing) {
    return (
      <div className="min-h-[60vh] flex flex-col items-center justify-center">
        <div className="h-10 w-10 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent mb-4" />
        <p className="text-gray-500">로그인 처리 중...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-[60vh] flex flex-col items-center justify-center px-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-lg p-8 text-center">
          <div className="text-red-500 text-4xl mb-4">!</div>
          <h2 className="text-xl font-bold text-gray-900 mb-2">로그인 실패</h2>
          <p className="text-gray-500 mb-6">{error}</p>
          <Link
            href="/login"
            className="inline-block px-6 py-2 bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90"
          >
            로그인 페이지로 돌아가기
          </Link>
        </div>
      </div>
    );
  }

  return null;
}

export default function OAuthCallbackPage({ params }: { params: Promise<{ provider: string }> }) {
  const { provider } = use(params);
  return (
    <Suspense
      fallback={
        <div className="min-h-[60vh] flex items-center justify-center">
          <div className="h-10 w-10 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
        </div>
      }
    >
      <OAuthCallbackContent provider={provider} />
    </Suspense>
  );
}
