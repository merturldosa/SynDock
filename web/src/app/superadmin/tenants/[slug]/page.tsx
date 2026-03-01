"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  getPlatformTenant,
  updatePlatformTenant,
  type TenantDetail,
} from "@/lib/platformApi";
import type { TenantConfig } from "@/types/tenant";

export default function TenantDetailPage() {
  const params = useParams();
  const router = useRouter();
  const slug = params.slug as string;

  const [tenant, setTenant] = useState<TenantDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [name, setName] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [configText, setConfigText] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  useEffect(() => {
    getPlatformTenant(slug)
      .then((t) => {
        setTenant(t);
        setName(t.name);
        setIsActive(t.isActive);
        if (t.configJson) {
          try {
            const parsed = JSON.parse(t.configJson);
            setConfigText(JSON.stringify(parsed, null, 2));
          } catch {
            setConfigText(t.configJson);
          }
        }
      })
      .catch(() => router.push("/superadmin/tenants"))
      .finally(() => setLoading(false));
  }, [slug, router]);

  const handleSave = async () => {
    setError("");
    setSuccess("");

    // Validate JSON
    if (configText.trim()) {
      try {
        JSON.parse(configText);
      } catch {
        setError("ConfigJson이 유효한 JSON 형식이 아닙니다.");
        return;
      }
    }

    setSaving(true);
    try {
      await updatePlatformTenant(slug, {
        name,
        isActive,
        configJson: configText.trim() || undefined,
      });
      setSuccess("저장되었습니다.");
    } catch {
      setError("저장에 실패했습니다.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
      </div>
    );
  }

  if (!tenant) return null;

  // Parse config for theme preview
  let config: TenantConfig | null = null;
  try {
    if (configText) config = JSON.parse(configText) as TenantConfig;
  } catch {
    /* ignore */
  }

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">
        쇼핑몰 상세: {tenant.name}
      </h1>

      {error && (
        <div className="p-3 mb-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
          {error}
        </div>
      )}
      {success && (
        <div className="p-3 mb-4 bg-emerald-50 border border-emerald-200 rounded-lg text-sm text-emerald-600">
          {success}
        </div>
      )}

      {/* Basic Info */}
      <div className="bg-white rounded-xl shadow-sm p-5 mb-6 space-y-4">
        <h2 className="font-semibold text-gray-900">기본 정보</h2>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              쇼핑몰 이름
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Slug
            </label>
            <input
              type="text"
              value={slug}
              disabled
              className="w-full px-3 py-2.5 border rounded-lg text-sm bg-gray-50 text-gray-500"
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              도메인
            </label>
            <p className="text-sm text-gray-600">
              {tenant.customDomain || tenant.subdomain || "-"}
            </p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              상태
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
                className="w-4 h-4 rounded text-emerald-600"
              />
              <span className="text-sm text-gray-700">운영 중</span>
            </label>
          </div>
        </div>
      </div>

      {/* Theme Preview */}
      {config?.theme && (
        <div className="bg-white rounded-xl shadow-sm p-5 mb-6">
          <h2 className="font-semibold text-gray-900 mb-3">테마 미리보기</h2>
          <div className="flex gap-3">
            {Object.entries(config.theme).map(([key, value]) => (
              <div key={key} className="text-center">
                <div
                  className="w-10 h-10 rounded-full border-2 border-gray-200 mx-auto mb-1"
                  style={{ backgroundColor: value }}
                />
                <p className="text-xs text-gray-500">{key}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Payment Settings */}
      <div className="bg-white rounded-xl shadow-sm p-5 mb-6">
        <h2 className="font-semibold text-gray-900 mb-3">결제 설정</h2>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">PG사</label>
            <select
              value={config?.paymentConfig?.provider || "Mock"}
              onChange={(e) => {
                try {
                  const parsed = configText ? JSON.parse(configText) : {};
                  parsed.paymentConfig = { ...parsed.paymentConfig, provider: e.target.value };
                  setConfigText(JSON.stringify(parsed, null, 2));
                } catch { /* ignore */ }
              }}
              className="w-full px-3 py-2.5 border rounded-lg text-sm"
            >
              <option value="Mock">Mock (테스트)</option>
              <option value="TossPayments">TossPayments</option>
            </select>
          </div>

          {(config?.paymentConfig?.provider === "TossPayments") && (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Client Key</label>
                <input
                  type="text"
                  value={config?.paymentConfig?.clientKey || ""}
                  onChange={(e) => {
                    try {
                      const parsed = configText ? JSON.parse(configText) : {};
                      parsed.paymentConfig = { ...parsed.paymentConfig, clientKey: e.target.value };
                      setConfigText(JSON.stringify(parsed, null, 2));
                    } catch { /* ignore */ }
                  }}
                  placeholder="test_ck_..."
                  className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Secret Key</label>
                <input
                  type="password"
                  value={config?.paymentConfig?.secretKey || ""}
                  onChange={(e) => {
                    try {
                      const parsed = configText ? JSON.parse(configText) : {};
                      parsed.paymentConfig = { ...parsed.paymentConfig, secretKey: e.target.value };
                      setConfigText(JSON.stringify(parsed, null, 2));
                    } catch { /* ignore */ }
                  }}
                  placeholder="test_sk_..."
                  className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
                />
              </div>
            </>
          )}
        </div>
      </div>

      {/* ConfigJson Editor */}
      <div className="bg-white rounded-xl shadow-sm p-5 mb-6">
        <h2 className="font-semibold text-gray-900 mb-3">ConfigJson</h2>
        <textarea
          value={configText}
          onChange={(e) => setConfigText(e.target.value)}
          rows={16}
          className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
          placeholder='{"theme": {"primary": "#3B82F6", ...}}'
        />
      </div>

      <div className="flex gap-3">
        <button
          onClick={handleSave}
          disabled={saving}
          className="px-6 py-2.5 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700 disabled:opacity-50"
        >
          {saving ? "저장 중..." : "저장"}
        </button>
        <button
          onClick={() => router.push("/superadmin/tenants")}
          className="px-6 py-2.5 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
        >
          목록으로
        </button>
      </div>
    </div>
  );
}
