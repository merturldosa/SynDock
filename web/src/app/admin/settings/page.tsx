"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useTranslations } from "next-intl";
import { getTenantSettings, updateTenantSettings, type TenantSettings } from "@/lib/adminApi";
import api from "@/lib/api";

export default function AdminSettingsPage() {
  const t = useTranslations();
  const [settings, setSettings] = useState<TenantSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [success, setSuccess] = useState("");
  const [error, setError] = useState("");

  const [form, setForm] = useState({
    companyName: "",
    companyAddress: "",
    businessNumber: "",
    ceoName: "",
    contactPhone: "",
    contactEmail: "",
    heroSubtitle: "",
    heroTagline: "",
    heroDescription: "",
    logoUrl: "",
    faviconUrl: "",
    themePrimary: "",
    themePrimaryLight: "",
    themeSecondary: "",
    themeSecondaryLight: "",
    themeBackground: "",
  });

  useEffect(() => {
    getTenantSettings()
      .then((s) => {
        setSettings(s);
        setForm({
          companyName: s.companyName || "",
          companyAddress: s.companyAddress || "",
          businessNumber: s.businessNumber || "",
          ceoName: s.ceoName || "",
          contactPhone: s.contactPhone || "",
          contactEmail: s.contactEmail || "",
          heroSubtitle: s.heroSubtitle || "",
          heroTagline: s.heroTagline || "",
          heroDescription: s.heroDescription || "",
          logoUrl: s.logoUrl || "",
          faviconUrl: s.faviconUrl || "",
          themePrimary: s.theme?.primary || "",
          themePrimaryLight: s.theme?.primaryLight || "",
          themeSecondary: s.theme?.secondary || "",
          themeSecondaryLight: s.theme?.secondaryLight || "",
          themeBackground: s.theme?.background || "",
        });
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const handleImageUpload = async (field: "logoUrl" | "faviconUrl", e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const formData = new FormData();
      formData.append("file", file);
      const { data } = await api.post("/upload/image?folder=settings", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      setForm((f) => ({ ...f, [field]: data.url }));
    } catch {
      alert(t("admin.settings.uploadFailed"));
    }
  };

  const handleSave = async () => {
    setError("");
    setSuccess("");
    setSaving(true);
    try {
      await updateTenantSettings({
        companyName: form.companyName || null,
        companyAddress: form.companyAddress || null,
        businessNumber: form.businessNumber || null,
        ceoName: form.ceoName || null,
        contactPhone: form.contactPhone || null,
        contactEmail: form.contactEmail || null,
        heroSubtitle: form.heroSubtitle || null,
        heroTagline: form.heroTagline || null,
        heroDescription: form.heroDescription || null,
        logoUrl: form.logoUrl || null,
        faviconUrl: form.faviconUrl || null,
        theme: {
          primary: form.themePrimary || null,
          primaryLight: form.themePrimaryLight || null,
          secondary: form.themeSecondary || null,
          secondaryLight: form.themeSecondaryLight || null,
          background: form.themeBackground || null,
        },
      });
      setSuccess(t("admin.settings.saveSuccess"));
    } catch {
      setError(t("admin.settings.saveFailed"));
    }
    setSaving(false);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[var(--color-primary)] border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">{t("admin.settings.title")}</h1>

      {error && <div className="p-3 mb-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">{error}</div>}
      {success && <div className="p-3 mb-4 bg-emerald-50 border border-emerald-200 rounded-lg text-sm text-emerald-600">{success}</div>}

      {/* Company Info */}
      <div className="bg-white rounded-xl shadow-sm p-6 mb-6 space-y-4">
        <h2 className="font-semibold text-[var(--color-secondary)]">{t("admin.settings.companyInfo")}</h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.companyName")}</label>
            <input type="text" value={form.companyName} onChange={(e) => setForm((f) => ({ ...f, companyName: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
          </div>
          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.ceoName")}</label>
            <input type="text" value={form.ceoName} onChange={(e) => setForm((f) => ({ ...f, ceoName: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
          </div>
        </div>
        <div>
          <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.businessNumber")}</label>
          <input type="text" value={form.businessNumber} onChange={(e) => setForm((f) => ({ ...f, businessNumber: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" placeholder="000-00-00000" />
        </div>
        <div>
          <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.companyAddress")}</label>
          <input type="text" value={form.companyAddress} onChange={(e) => setForm((f) => ({ ...f, companyAddress: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.contactPhone")}</label>
            <input type="text" value={form.contactPhone} onChange={(e) => setForm((f) => ({ ...f, contactPhone: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
          </div>
          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.contactEmail")}</label>
            <input type="email" value={form.contactEmail} onChange={(e) => setForm((f) => ({ ...f, contactEmail: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
          </div>
        </div>
      </div>

      {/* Theme */}
      <div className="bg-white rounded-xl shadow-sm p-6 mb-6 space-y-4">
        <h2 className="font-semibold text-[var(--color-secondary)]">{t("admin.settings.theme")}</h2>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          {([
            { key: "themePrimary" as const, label: "Primary" },
            { key: "themePrimaryLight" as const, label: "Primary Light" },
            { key: "themeSecondary" as const, label: "Secondary" },
            { key: "themeSecondaryLight" as const, label: "Secondary Light" },
            { key: "themeBackground" as const, label: "Background" },
          ]).map((item) => (
            <div key={item.key} className="text-center">
              <label className="block text-xs text-gray-500 mb-1">{item.label}</label>
              <input
                type="color"
                value={form[item.key] || "#ffffff"}
                onChange={(e) => setForm((f) => ({ ...f, [item.key]: e.target.value }))}
                className="w-full h-10 rounded-lg cursor-pointer border"
              />
              <input
                type="text"
                value={form[item.key]}
                onChange={(e) => setForm((f) => ({ ...f, [item.key]: e.target.value }))}
                className="w-full mt-1 px-2 py-1 border rounded text-xs text-center font-mono"
                placeholder="#000000"
              />
            </div>
          ))}
        </div>
        {/* Live Preview */}
        <div className="mt-4 p-4 rounded-xl border" style={{ backgroundColor: form.themeBackground || "#fff" }}>
          <p className="text-sm font-medium mb-2" style={{ color: form.themeSecondary || "#333" }}>{t("admin.settings.themePreview")}</p>
          <div className="flex gap-2">
            <span className="px-3 py-1.5 rounded-lg text-white text-xs" style={{ backgroundColor: form.themePrimary || "#ccc" }}>Primary</span>
            <span className="px-3 py-1.5 rounded-lg text-white text-xs" style={{ backgroundColor: form.themeSecondary || "#ccc" }}>Secondary</span>
          </div>
        </div>
      </div>

      {/* Hero Section */}
      <div className="bg-white rounded-xl shadow-sm p-6 mb-6 space-y-4">
        <h2 className="font-semibold text-[var(--color-secondary)]">{t("admin.settings.heroSection")}</h2>
        <div>
          <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.heroSubtitle")}</label>
          <input type="text" value={form.heroSubtitle} onChange={(e) => setForm((f) => ({ ...f, heroSubtitle: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
        </div>
        <div>
          <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.heroTagline")}</label>
          <input type="text" value={form.heroTagline} onChange={(e) => setForm((f) => ({ ...f, heroTagline: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm" />
        </div>
        <div>
          <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.heroDescription")}</label>
          <textarea value={form.heroDescription} onChange={(e) => setForm((f) => ({ ...f, heroDescription: e.target.value }))} rows={3} className="w-full px-3 py-2.5 border rounded-lg text-sm resize-none" />
        </div>
      </div>

      {/* Logo & Favicon */}
      <div className="bg-white rounded-xl shadow-sm p-6 mb-6 space-y-4">
        <h2 className="font-semibold text-[var(--color-secondary)]">{t("admin.settings.logoFavicon")}</h2>
        <div className="grid grid-cols-2 gap-6">
          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.logo")}</label>
            <input type="file" accept="image/*" onChange={(e) => handleImageUpload("logoUrl", e)} className="text-sm" />
            {form.logoUrl && (
              <div className="mt-2 w-32 h-16 bg-gray-50 rounded-lg overflow-hidden flex items-center justify-center">
                <Image src={form.logoUrl} alt="Logo" width={128} height={64} className="object-contain" unoptimized />
              </div>
            )}
          </div>
          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.favicon")}</label>
            <input type="file" accept="image/*" onChange={(e) => handleImageUpload("faviconUrl", e)} className="text-sm" />
            {form.faviconUrl && (
              <div className="mt-2 w-10 h-10 bg-gray-50 rounded-lg overflow-hidden flex items-center justify-center">
                <Image src={form.faviconUrl} alt="Favicon" width={40} height={40} className="object-contain" unoptimized />
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Save */}
      <button
        onClick={handleSave}
        disabled={saving}
        className="w-full py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
      >
        {saving ? t("admin.settings.saving") : t("admin.settings.settingsSave")}
      </button>
    </div>
  );
}
