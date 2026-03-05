"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useTenantStore } from "@/stores/tenantStore";
import api from "@/lib/api";

export default function ResetPasswordPage() {
  const t = useTranslations();
  const router = useRouter();
  const searchParams = useSearchParams();
  const { name: tenantName } = useTenantStore();

  const email = searchParams.get("email") || "";
  const token = searchParams.get("token") || "";

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (newPassword !== confirmPassword) {
      setError(t("auth.resetPassword.mismatch"));
      return;
    }

    setError("");
    setIsLoading(true);
    try {
      await api.post("/auth/reset-password", { email, token, newPassword });
      sessionStorage.setItem("resetPasswordSuccess", "true");
      router.push("/login");
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { error?: string } } };
      setError(axiosErr.response?.data?.error || t("error.generic"));
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
          <p className="text-gray-500 mt-2">{t("auth.resetPassword.title")}</p>
        </div>

        {error && (
          <div className="mb-4 p-3 bg-red-50 text-red-600 rounded-lg text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <Input
            id="newPassword"
            type="password"
            label={t("auth.resetPassword.newPassword")}
            placeholder={t("auth.resetPassword.newPassword")}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
          <Input
            id="confirmPassword"
            type="password"
            label={t("auth.resetPassword.confirmPassword")}
            placeholder={t("auth.resetPassword.confirmPassword")}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
          />
          <Button type="submit" isLoading={isLoading} className="mt-2">
            {t("auth.resetPassword.submit")}
          </Button>
        </form>

        <p className="mt-6 text-center text-sm text-gray-500">
          <Link
            href="/login"
            className="text-[var(--color-primary)] hover:underline font-medium"
          >
            {t("auth.forgotPassword.backToLogin")}
          </Link>
        </p>
      </div>
    </div>
  );
}
