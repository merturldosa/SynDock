"use client";

import { useState } from "react";
import { Sparkles, RefreshCw, Check } from "lucide-react";
import { generateProductContent, type GeneratedContent } from "@/lib/adminApi";

interface AIContentGeneratorProps {
  productId: number | null;
  onApply: (description: string) => void;
}

export function AIContentGenerator({ productId, onApply }: AIContentGeneratorProps) {
  const [loading, setLoading] = useState(false);
  const [content, setContent] = useState<GeneratedContent | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleGenerate = async () => {
    if (!productId) {
      setError("상품을 먼저 저장해주세요.");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const result = await generateProductContent(productId);
      setContent(result);
    } catch {
      setError("AI 콘텐츠 생성에 실패했습니다. 다시 시도해주세요.");
    }
    setLoading(false);
  };

  return (
    <div className="bg-gradient-to-r from-purple-50 to-blue-50 rounded-xl p-4 border border-purple-100">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <Sparkles size={16} className="text-purple-500" />
          <span className="text-sm font-semibold text-purple-700">AI 콘텐츠 생성</span>
        </div>
        <div className="flex gap-2">
          {content && (
            <button
              type="button"
              onClick={() => onApply(content.fullDescription)}
              className="flex items-center gap-1 px-3 py-1.5 bg-emerald-500 text-white rounded-lg text-xs font-medium hover:bg-emerald-600"
            >
              <Check size={12} /> 적용
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
                생성 중...
              </>
            ) : content ? (
              <>
                <RefreshCw size={12} /> 재생성
              </>
            ) : (
              <>
                <Sparkles size={12} /> AI 생성
              </>
            )}
          </button>
        </div>
      </div>

      {!productId && (
        <p className="text-xs text-gray-500">상품 저장 후 AI 콘텐츠를 생성할 수 있습니다.</p>
      )}

      {error && <p className="text-xs text-red-500 mt-2">{error}</p>}

      {content && (
        <div className="space-y-3 mt-3">
          <div className="bg-white rounded-lg p-3">
            <p className="text-xs font-semibold text-purple-600 mb-1">상단 (Hero)</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{content.heroSection}</p>
          </div>
          <div className="bg-white rounded-lg p-3">
            <p className="text-xs font-semibold text-blue-600 mb-1">중단 (Features)</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{content.featureSection}</p>
          </div>
          <div className="bg-white rounded-lg p-3">
            <p className="text-xs font-semibold text-emerald-600 mb-1">하단 (CTA)</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{content.closingSection}</p>
          </div>
        </div>
      )}
    </div>
  );
}
