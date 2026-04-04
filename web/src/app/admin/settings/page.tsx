"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useTranslations } from "next-intl";
import { getTenantSettings, updateTenantSettings, type TenantSettings } from "@/lib/adminApi";
import api from "@/lib/api";
import toast from "react-hot-toast";
import { HelpTooltip, HELP_CONFIGS } from "@/components/ui/HelpTooltip";

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
    aiOpenAiApiKey: "",
    aiOpenAiModel: "gpt-4o",
    aiDalleModel: "dall-e-3",
    aiClaudeApiKey: "",
    aiClaudeModel: "claude-sonnet-4-20250514",
    aiContentEnabled: false,
    aiImageEnabled: false,
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
          aiOpenAiApiKey: s.aiIntegration?.openAiApiKey || "",
          aiOpenAiModel: s.aiIntegration?.openAiModel || "gpt-4o",
          aiDalleModel: s.aiIntegration?.dalleModel || "dall-e-3",
          aiClaudeApiKey: s.aiIntegration?.claudeApiKey || "",
          aiClaudeModel: s.aiIntegration?.claudeModel || "claude-sonnet-4-20250514",
          aiContentEnabled: s.aiIntegration?.aiContentEnabled || false,
          aiImageEnabled: s.aiIntegration?.aiImageEnabled || false,
        });
      })
      .catch(() => { toast.error(t("common.fetchError")); })
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
      toast.error(t("admin.settings.uploadFailed"));
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
        aiIntegration: {
          openAiApiKey: form.aiOpenAiApiKey || null,
          openAiModel: form.aiOpenAiModel || null,
          dalleModel: form.aiDalleModel || null,
          claudeApiKey: form.aiClaudeApiKey || null,
          claudeModel: form.aiClaudeModel || null,
          aiContentEnabled: form.aiContentEnabled,
          aiImageEnabled: form.aiImageEnabled,
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
                <Image src={form.logoUrl} alt="Logo" width={128} height={64} className="object-contain" />
              </div>
            )}
          </div>
          <div>
            <label className="block text-sm text-gray-500 mb-1">{t("admin.settings.favicon")}</label>
            <input type="file" accept="image/*" onChange={(e) => handleImageUpload("faviconUrl", e)} className="text-sm" />
            {form.faviconUrl && (
              <div className="mt-2 w-10 h-10 bg-gray-50 rounded-lg overflow-hidden flex items-center justify-center">
                <Image src={form.faviconUrl} alt="Favicon" width={40} height={40} className="object-contain" />
              </div>
            )}
          </div>
        </div>
      </div>

      {/* AI Integration */}
      <div className="bg-white rounded-xl shadow-sm p-6 mb-6 space-y-4">
        <h2 className="font-semibold text-[var(--color-secondary)]">AI 연동 설정</h2>
        <p className="text-xs text-gray-400">AI 콘텐츠 생성 및 이미지 생성에 사용할 API 키를 입력하세요.</p>

        <div className="flex items-center gap-4">
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={form.aiContentEnabled} onChange={(e) => setForm((f) => ({ ...f, aiContentEnabled: e.target.checked }))} className="rounded" />
            AI 콘텐츠 생성 활성화
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={form.aiImageEnabled} onChange={(e) => setForm((f) => ({ ...f, aiImageEnabled: e.target.checked }))} className="rounded" />
            AI 이미지 생성 활성화
          </label>
        </div>

        <div className="border-t pt-4 space-y-3">
          <h3 className="text-sm font-medium text-gray-700">OpenAI (GPT / DALL-E)</h3>
          <div>
            <label className="flex items-center text-sm text-gray-500 mb-1">
              OpenAI API Key
              <HelpTooltip {...HELP_CONFIGS.openAiApiKey} />
            </label>
            <input
              type="password"
              value={form.aiOpenAiApiKey}
              onChange={(e) => setForm((f) => ({ ...f, aiOpenAiApiKey: e.target.value }))}
              className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
              placeholder="sk-proj-xxxx... (platform.openai.com 에서 발급)"
            />
            <p className="text-xs text-gray-400 mt-1">GPT 챗봇 + DALL-E 이미지 생성에 사용 (종량제 과금)</p>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm text-gray-500 mb-1">GPT 모델</label>
              <select value={form.aiOpenAiModel} onChange={(e) => setForm((f) => ({ ...f, aiOpenAiModel: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm">
                <option value="gpt-4o">GPT-4o</option>
                <option value="gpt-4o-mini">GPT-4o Mini</option>
                <option value="gpt-4-turbo">GPT-4 Turbo</option>
              </select>
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">DALL-E 모델</label>
              <select value={form.aiDalleModel} onChange={(e) => setForm((f) => ({ ...f, aiDalleModel: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm">
                <option value="dall-e-3">DALL-E 3</option>
                <option value="dall-e-2">DALL-E 2</option>
              </select>
            </div>
          </div>
        </div>

        <div className="border-t pt-4 space-y-3">
          <h3 className="text-sm font-medium text-gray-700">Claude (Anthropic)</h3>
          <div>
            <label className="flex items-center text-sm text-gray-500 mb-1">
              Claude API Key
              <HelpTooltip {...HELP_CONFIGS.claudeApiKey} />
            </label>
            <input
              type="password"
              value={form.aiClaudeApiKey}
              onChange={(e) => setForm((f) => ({ ...f, aiClaudeApiKey: e.target.value }))}
              className="w-full px-3 py-2.5 border rounded-lg text-sm font-mono"
              placeholder="sk-ant-api03-xxxx... (console.anthropic.com 에서 발급)"
            />
            <p className="text-xs text-gray-400 mt-1">한국어 챗봇 대화에 사용 (Claude가 더 자연스러운 한국어 제공)</p>
          </div>
          <div>
            <label className="block text-sm text-gray-500 mb-1">Claude 모델</label>
            <select value={form.aiClaudeModel} onChange={(e) => setForm((f) => ({ ...f, aiClaudeModel: e.target.value }))} className="w-full px-3 py-2.5 border rounded-lg text-sm">
              <option value="claude-sonnet-4-20250514">Claude Sonnet 4</option>
              <option value="claude-haiku-4-5-20251001">Claude Haiku 4.5</option>
            </select>
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
