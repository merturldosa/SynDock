"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useAuthStore } from "@/stores/authStore";
import { useTenantStore } from "@/stores/tenantStore";
import api from "@/lib/api";
import type { LoginResponse } from "@/types/auth";

const loginSchema = z.object({
  username: z.string().min(1, "아이디를 입력해주세요."),
  password: z.string().min(1, "비밀번호를 입력해주세요."),
});

type LoginForm = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const router = useRouter();
  const { login, isAuthenticated, fetchMe } = useAuthStore();
  const { name: tenantName } = useTenantStore();
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    fetchMe();
  }, [fetchMe]);

  useEffect(() => {
    if (isAuthenticated) {
      const returnTo = sessionStorage.getItem("returnTo") || "/";
      sessionStorage.removeItem("returnTo");
      router.replace(returnTo);
    }
  }, [isAuthenticated, router]);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginForm) => {
    setError("");
    setIsLoading(true);
    try {
      const { data: res } = await api.post<LoginResponse>("/auth/login", data);
      login(res.accessToken, res.refreshToken, res.user);
      const returnTo = sessionStorage.getItem("returnTo") || "/";
      sessionStorage.removeItem("returnTo");
      router.push(returnTo);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { error?: string } } };
      setError(axiosErr.response?.data?.error || "로그인에 실패했습니다.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-[70vh] flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-lg p-8">
        <div className="text-center mb-8">
          <Link href="/" className="text-2xl font-bold text-[var(--color-primary)]">
            {tenantName || "Shop"}
          </Link>
          <p className="text-gray-500 mt-2">로그인하고 더 많은 혜택을 받으세요</p>
        </div>

        {error && (
          <div className="mb-4 p-3 bg-red-50 text-red-600 rounded-lg text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <Input
            id="username"
            label="아이디"
            placeholder="아이디를 입력하세요"
            error={errors.username?.message}
            {...register("username")}
          />
          <Input
            id="password"
            type="password"
            label="비밀번호"
            placeholder="비밀번호를 입력하세요"
            error={errors.password?.message}
            {...register("password")}
          />
          <Button type="submit" isLoading={isLoading} className="mt-2">
            로그인
          </Button>
        </form>

        <p className="mt-6 text-center text-sm text-gray-500">
          계정이 없으신가요?{" "}
          <Link href="/register" className="text-[var(--color-primary)] hover:underline font-medium">
            회원가입
          </Link>
        </p>
      </div>
    </div>
  );
}
