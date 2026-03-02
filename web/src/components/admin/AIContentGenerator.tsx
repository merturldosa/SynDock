"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Sparkles, RefreshCw, Check } from "lucide-react";
import { generateProductContent, type GeneratedContent } from "@/lib/adminApi";

interface AIContentGeneratorProps {
  productId: number | null;
  onApply: (description: string) => void;
}

export function AIContentGenerator({ productId, onApply }: AIContentGeneratorProps) {
  const t = useTranslations();
  const [loading, setLoading] = useState(false);
  const [content, setContent] = useState<GeneratedContent | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleGenerate = async () => {
    if (!productId) {
      setError(t("admin.aiContent.saveFirst"));
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const result = await generateProductContent(productId);
      setContent(result);
    } catch {
      setError(t("admin.aiContent.generateFailed"));
    }
    setLoading(false);
  };

  return (
    <div className="bg-gradient-to-r from-purple-50 to-blue-50 rounded-xl p-4 border border-purple-100">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <Sparkles size={16} className="text-purple-500" />
          <span className="text-sm font-semibold text-purple-700">{t("admin.aiContent.title")}</span>
        </div>
        <div className="flex gap-2">
          {content && (
            <button
              type="button"
              onClick={() => onApply(content.fullDescription)}
              className="flex items-center gap-1 px-3 py-1.5 bg-emerald-500 text-white rounded-lg text-xs font-medium hover:bg-emerald-600"
            >
              <Check size={12} /> {t("admin.aiContent.apply")}
            </button>
          )}
          <button
            type="button"
            onClick={handleGenerate}
            disabled={loading || !productId}
            className="flex items-center gap-1 px-3 py-1.5 bg-purple-500 text-white rounded-lg text-xs font-medium hover:bg-purple-600 disabled:opacity-50"
          >
            {loading ? (
              <>
                <div className="h-3 w-3 animate-spin rounded-full border-2 border-white border-t-transparent" />
                {t("admin.aiContent.generating")}
              </>
            ) : content ? (
              <>
                <RefreshCw size={12} /> {t("admin.aiContent.regenerate")}
              </>
            ) : (
              <>
                <Sparkles size={12} /> {t("admin.aiContent.generate")}
              </>
            )}
          </button>
        </div>
      </div>

      {!productId && (
        <p className="text-xs text-gray-500">{t("admin.aiContent.afterSave")}</p>
      )}

      {error && <p className="text-xs text-red-500 mt-2">{error}</p>}

      {content && (
        <div className="space-y-3 mt-3">
          <div className="bg-white rounded-lg p-3">
            <p className="text-xs font-semibold text-purple-600 mb-1">{t("admin.aiContent.heroSection")}</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{content.heroSection}</p>
          </div>
          <div className="bg-white rounded-lg p-3">
            <p className="text-xs font-semibold text-blue-600 mb-1">{t("admin.aiContent.featureSection")}</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{content.featureSection}</p>
          </div>
          <div className="bg-white rounded-lg p-3">
            <p className="text-xs font-semibold text-emerald-600 mb-1">{t("admin.aiContent.closingSection")}</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{content.closingSection}</p>
          </div>
        </div>
      )}
    </div>
  );
}
