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
