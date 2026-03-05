"use client";

import { useState } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useTenantStore } from "@/stores/tenantStore";
import api from "@/lib/api";

export default function ForgotPasswordPage() {
  const t = useTranslations();
  const { name: tenantName } = useTenantStore();
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;

    setError("");
    setIsLoading(true);
    try {
      await api.post("/auth/forgot-password", { email });
      setSuccess(true);
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
          <p className="text-gray-500 mt-2">{t("auth.forgotPassword.title")}</p>
        </div>

        {success ? (
          <div className="text-center">
            <div className="mb-4 p-4 bg-green-50 text-green-700 rounded-lg text-sm">
              {t("auth.forgotPassword.success")}
            </div>
            <Link
              href="/login"
              className="text-[var(--color-primary)] hover:underline font-medium text-sm"
            >
              {t("auth.forgotPassword.backToLogin")}
            </Link>
          </div>
        ) : (
          <>
            {error && (
              <div className="mb-4 p-3 bg-red-50 text-red-600 rounded-lg text-sm">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <Input
                id="email"
                type="email"
                label={t("auth.forgotPassword.email")}
                placeholder={t("auth.forgotPassword.email")}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
              <Button type="submit" isLoading={isLoading} className="mt-2">
                {t("auth.forgotPassword.submit")}
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
          </>
        )}
      </div>
    </div>
  );
}
