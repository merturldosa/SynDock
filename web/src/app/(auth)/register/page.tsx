"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useTenantStore } from "@/stores/tenantStore";
import api from "@/lib/api";
import { getKakaoLoginUrl, getGoogleLoginUrl } from "@/lib/oauthApi";

const registerSchema = z
  .object({
    username: z.string().min(4, "아이디는 4자 이상이어야 합니다."),
    email: z.string().email("올바른 이메일 형식이 아닙니다."),
    password: z.string().min(8, "비밀번호는 8자 이상이어야 합니다."),
    confirmPassword: z.string().min(1, "비밀번호를 다시 입력해주세요."),
    name: z.string().min(1, "이름을 입력해주세요."),
    phone: z.string().optional(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "비밀번호가 일치하지 않습니다.",
    path: ["confirmPassword"],
  });

type RegisterForm = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const router = useRouter();
  const { name: tenantName } = useTenantStore();
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterForm) => {
    setError("");
    setIsLoading(true);
    try {
      const { confirmPassword: _, ...payload } = data;
      await api.post("/auth/register", payload);
      router.push("/login?registered=true");
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { error?: string } } };
      setError(axiosErr.response?.data?.error || "회원가입에 실패했습니다.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-[70vh] flex items-center justify-center px-4 py-8">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-lg p-8">
        <h1 className="text-2xl font-bold text-[var(--color-secondary)] text-center mb-2">
          회원가입
        </h1>
        <p className="text-gray-500 text-center mb-8">
          {tenantName || "Shop"} 회원이 되어보세요
        </p>

        {error && (
          <div className="mb-4 p-3 bg-red-50 text-red-600 rounded-lg text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <Input
            id="username"
            label="아이디"
            placeholder="4자 이상"
            error={errors.username?.message}
            {...register("username")}
          />
          <Input
            id="email"
            type="email"
            label="이메일"
            placeholder="example@email.com"
            error={errors.email?.message}
            {...register("email")}
          />
          <Input
            id="password"
            type="password"
            label="비밀번호"
            placeholder="8자 이상"
            error={errors.password?.message}
            {...register("password")}
          />
          <Input
            id="confirmPassword"
            type="password"
            label="비밀번호 확인"
            placeholder="비밀번호를 다시 입력하세요"
            error={errors.confirmPassword?.message}
            {...register("confirmPassword")}
          />
          <Input
            id="name"
            label="이름"
            placeholder="홍길동"
            error={errors.name?.message}
            {...register("name")}
          />
          <Input
            id="phone"
            label="전화번호 (선택)"
            placeholder="010-0000-0000"
            {...register("phone")}
          />

          <Button type="submit" variant="secondary" isLoading={isLoading} className="mt-2">
            회원가입
          </Button>
        </form>

        <div className="mt-6 flex items-center gap-3">
          <div className="flex-1 h-px bg-gray-200" />
          <span className="text-xs text-gray-400">또는</span>
          <div className="flex-1 h-px bg-gray-200" />
        </div>

        <div className="mt-4 flex flex-col gap-3">
          <button
            type="button"
            onClick={() => { window.location.href = getKakaoLoginUrl(); }}
            className="w-full flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm transition-colors"
            style={{ backgroundColor: "#FEE500", color: "#191919" }}
          >
            <svg width="18" height="18" viewBox="0 0 18 18"><path fill="#191919" d="M9 1C4.58 1 1 3.8 1 7.19c0 2.18 1.44 4.1 3.62 5.2l-.92 3.42c-.08.29.25.52.5.35L8 13.46c.33.03.66.05 1 .05 4.42 0 8-2.8 8-6.32C17 3.8 13.42 1 9 1"/></svg>
            카카오로 시작하기
          </button>
          <button
            type="button"
            onClick={() => { window.location.href = getGoogleLoginUrl(); }}
            className="w-full flex items-center justify-center gap-2 py-2.5 rounded-lg font-medium text-sm border border-gray-300 bg-white text-gray-700 hover:bg-gray-50 transition-colors"
          >
            <svg width="18" height="18" viewBox="0 0 18 18"><path fill="#4285F4" d="M17.64 9.2c0-.64-.06-1.25-.16-1.84H9v3.48h4.84a4.14 4.14 0 0 1-1.8 2.72v2.26h2.92a8.78 8.78 0 0 0 2.68-6.62z"/><path fill="#34A853" d="M9 18c2.43 0 4.47-.8 5.96-2.18l-2.92-2.26c-.8.54-1.83.86-3.04.86-2.34 0-4.33-1.58-5.04-3.71H.96v2.33A9 9 0 0 0 9 18z"/><path fill="#FBBC05" d="M3.96 10.71A5.41 5.41 0 0 1 3.68 9c0-.59.1-1.17.28-1.71V4.96H.96A9 9 0 0 0 0 9c0 1.45.35 2.82.96 4.04l3-2.33z"/><path fill="#EA4335" d="M9 3.58a4.86 4.86 0 0 1 3.44 1.35l2.58-2.59A8.65 8.65 0 0 0 9 0 9 9 0 0 0 .96 4.96l3 2.33C4.67 5.16 6.66 3.58 9 3.58z"/></svg>
            Google로 시작하기
          </button>
        </div>

        <p className="mt-6 text-center text-sm text-gray-500">
          이미 계정이 있으신가요?{" "}
          <Link href="/login" className="text-[var(--color-primary)] hover:underline font-medium">
            로그인
          </Link>
        </p>
      </div>
    </div>
  );
}
