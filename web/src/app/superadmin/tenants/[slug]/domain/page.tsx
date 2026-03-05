"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { ChevronLeft, Globe, Shield, Copy, Check, RefreshCw } from "lucide-react";
import { useTranslations } from "next-intl";
import { formatDateShort } from "@/lib/format";
import {
  getTenantDomainConfig,
  updateTenantDomain,
  verifyTenantDomain,
  type DomainConfig,
} from "@/lib/platformApi";

const STATUS_COLORS: Record<string, string> = {
  Verified: "bg-emerald-100 text-emerald-700",
  Active: "bg-emerald-100 text-emerald-700",
  Pending: "bg-yellow-100 text-yellow-700",
  Failed: "bg-red-100 text-red-700",
  None: "bg-gray-100 text-gray-500",
};

export default function DomainManagementPage() {
  const t = useTranslations();
  const params = useParams();
  const router = useRouter();
  const slug = params.slug as string;

  const [config, setConfig] = useState<DomainConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [customDomain, setCustomDomain] = useState("");
  const [subdomain, setSubdomain] = useState("");
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [copiedIndex, setCopiedIndex] = useState<number | null>(null);

  useEffect(() => {
    getTenantDomainConfig(slug)
      .then((c) => {
        setConfig(c);
        setCustomDomain(c.customDomain || "");
        setSubdomain(c.subdomain || "");
      })
      .catch(() => router.push(`/superadmin/tenants/${slug}`))
      .finally(() => setLoading(false));
  }, [slug, router]);

  const handleSave = async () => {
    setSaving(true);
    setMessage(null);
    try {
      const result = await updateTenantDomain(slug, {
        customDomain: customDomain || undefined,
        subdomain: subdomain || undefined,
      });
      setConfig(result);
      setMessage({ type: "success", text: t("superadmin.domain.saveSuccess") });
    } catch {
      setMessage({ type: "error", text: t("superadmin.domain.saveFailed") });
    }
    setSaving(false);
  };

  const handleVerify = async () => {
    setVerifying(true);
    setMessage(null);
    try {
      const result = await verifyTenantDomain(slug);
      setMessage({
        type: result.isVerified ? "success" : "error",
        text: result.message,
      });
      // Reload config
      const updated = await getTenantDomainConfig(slug);
      setConfig(updated);
    } catch {
      setMessage({ type: "error", text: t("superadmin.domain.verifyFailed") });
    }
    setVerifying(false);
  };

  const copyToClipboard = (text: string, index: number) => {
    navigator.clipboard.writeText(text).then(() => {
      setCopiedIndex(index);
      setTimeout(() => setCopiedIndex(null), 2000);
    });
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-emerald-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="max-w-3xl">
      <Link
        href={`/superadmin/tenants/${slug}`}
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ChevronLeft size={16} /> {t("superadmin.domain.backToDetail")}
      </Link>

      <h1 className="text-2xl font-bold text-gray-900 mb-6">
        <Globe className="inline mr-2" size={24} />
        {t("superadmin.domain.title")}
      </h1>

      {message && (
        <div className={`p-3 mb-4 rounded-lg text-sm border ${
          message.type === "success"
            ? "bg-emerald-50 border-emerald-200 text-emerald-600"
            : "bg-red-50 border-red-200 text-red-600"
        }`}>
          {message.text}
        </div>
      )}

      {/* Current Status */}
      {config && (
        <div className="bg-white rounded-xl shadow-sm p-5 mb-6">
          <h2 className="font-semibold text-gray-900 mb-4">{t("superadmin.domain.currentStatus")}</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-gray-500">{t("superadmin.domain.customDomain")}</p>
              <p className="font-medium">{config.customDomain || "-"}</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">{t("superadmin.domain.subdomain")}</p>
              <p className="font-medium">{config.subdomain || "-"}</p>
            </div>
            <div>
              <p className="text-sm text-gray-500">{t("superadmin.domain.verificationStatus")}</p>
              <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[config.verificationStatus] || STATUS_COLORS.None}`}>
                {config.verificationStatus}
              </span>
              {config.verifiedAt && (
                <span className="text-xs text-gray-400 ml-2">
                  ({formatDateShort(config.verifiedAt)})
                </span>
              )}
            </div>
            <div>
              <p className="text-sm text-gray-500">{t("superadmin.domain.sslStatus")}</p>
              <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[config.sslStatus] || STATUS_COLORS.None}`}>
                <Shield size={12} />
                {config.sslStatus}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Domain Settings */}
      <div className="bg-white rounded-xl shadow-sm p-5 mb-6 space-y-4">
        <h2 className="font-semibold text-gray-900">{t("superadmin.domain.domainSettings")}</h2>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">{t("superadmin.domain.customDomain")}</label>
          <input
            type="text"
            value={customDomain}
            onChange={(e) => setCustomDomain(e.target.value)}
            className="w-full px-3 py-2.5 border rounded-lg text-sm"
            placeholder="shop.example.com"
          />
          <p className="text-xs text-gray-400 mt-1">{t("superadmin.domain.customDomainHint")}</p>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">{t("superadmin.domain.subdomain")}</label>
          <div className="flex items-center gap-2">
            <input
              type="text"
              value={subdomain}
              onChange={(e) => setSubdomain(e.target.value)}
              className="flex-1 px-3 py-2.5 border rounded-lg text-sm"
              placeholder="myshop"
            />
            <span className="text-sm text-gray-500">.syndock.com</span>
          </div>
        </div>
        <button
          onClick={handleSave}
          disabled={saving}
          className="px-6 py-2.5 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700 disabled:opacity-50"
        >
          {saving ? t("superadmin.domain.saving") : t("superadmin.domain.save")}
        </button>
      </div>

      {/* DNS Instructions */}
      {config && config.dnsInstructions.length > 0 && (
        <div className="bg-white rounded-xl shadow-sm p-5 mb-6">
          <h2 className="font-semibold text-gray-900 mb-4">{t("superadmin.domain.dnsInstructions")}</h2>
          <p className="text-sm text-gray-500 mb-4">
            {t("superadmin.domain.dnsInstructionsDesc")}
          </p>
          <div className="space-y-3">
            {config.dnsInstructions.map((dns, i) => (
              <div key={i} className="bg-gray-50 rounded-lg p-4">
                <div className="grid grid-cols-3 gap-4 text-sm">
                  <div>
                    <p className="text-xs text-gray-500 mb-1">{t("superadmin.domain.recordType")}</p>
                    <p className="font-mono font-medium">{dns.type}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500 mb-1">Host</p>
                    <p className="font-mono text-xs break-all">{dns.host}</p>
                  </div>
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="text-xs text-gray-500 mb-1">{t("superadmin.domain.targetValue")}</p>
                      <p className="font-mono text-xs break-all">{dns.target}</p>
                    </div>
                    <button
                      onClick={() => copyToClipboard(dns.target, i)}
                      className="ml-2 p-1.5 text-gray-400 hover:text-gray-600 flex-shrink-0"
                      title={t("superadmin.domain.copy")}
                      aria-label="Copy to clipboard"
                    >
                      {copiedIndex === i ? <Check size={14} className="text-emerald-500" /> : <Copy size={14} />}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Verify */}
      {config?.customDomain && (
        <div className="bg-white rounded-xl shadow-sm p-5 mb-6">
          <h2 className="font-semibold text-gray-900 mb-3">{t("superadmin.domain.verifyDomain")}</h2>
          <p className="text-sm text-gray-500 mb-4">
            {t("superadmin.domain.verifyDomainDesc")}
          </p>
          <button
            onClick={handleVerify}
            disabled={verifying}
            className="flex items-center gap-2 px-6 py-2.5 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
          >
            <RefreshCw size={16} className={verifying ? "animate-spin" : ""} />
            {verifying ? t("superadmin.domain.verifying") : t("superadmin.domain.verifyDomain")}
          </button>
        </div>
      )}

      <button
        onClick={() => router.push(`/superadmin/tenants/${slug}`)}
        className="px-6 py-2.5 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
      >
        {t("superadmin.domain.backToList")}
      </button>
    </div>
  );
}
