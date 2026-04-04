"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  CheckCircle, Circle, Building2, ImageIcon, FolderTree,
  Package, CreditCard, ShoppingCart, ChevronRight, X, Rocket,
} from "lucide-react";
import api from "@/lib/api";

interface OnboardingStep {
  step: string;
  label: string;
  completed: boolean;
}

interface OnboardingData {
  steps: OnboardingStep[];
  completionRate: number;
}

const STEP_ICONS: Record<string, React.ElementType> = {
  profile: Building2,
  logo: ImageIcon,
  categories: FolderTree,
  products: Package,
  payment: CreditCard,
  firstOrder: ShoppingCart,
};

const STEP_LINKS: Record<string, string> = {
  profile: "/admin/settings",
  logo: "/admin/settings",
  categories: "/admin/categories",
  products: "/admin/products",
  payment: "/admin/settings",
  firstOrder: "/admin/orders",
};

const STEP_DESCRIPTIONS: Record<string, string> = {
  profile: "회사명, 연락처, 사업자번호 등 기본 정보를 입력하세요.",
  logo: "쇼핑몰 로고와 파비콘을 업로드하세요. (권장: 200x60px PNG)",
  categories: "판매할 상품의 카테고리를 설정하세요.",
  products: "첫 번째 상품을 등록해보세요. 이미지와 가격을 입력하면 바로 판매 시작!",
  payment: "TossPayments 키를 연동하면 카드/계좌이체 결제를 받을 수 있습니다.",
  firstOrder: "모든 설정이 완료되면 고객의 첫 주문을 기다리세요!",
};

export function OnboardingWizard() {
  const [data, setData] = useState<OnboardingData | null>(null);
  const [dismissed, setDismissed] = useState(false);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const wasDismissed = localStorage.getItem("onboarding_dismissed");
    if (wasDismissed === "true") {
      setDismissed(true);
      setLoading(false);
      return;
    }

    api.get("/tenant-settings/onboarding")
      .then((res) => setData(res.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading || dismissed || !data || data.completionRate === 100) return null;

  const nextStep = data.steps.find((s) => !s.completed);
  const completedCount = data.steps.filter((s) => s.completed).length;

  return (
    <div className="bg-white rounded-2xl shadow-sm border p-6 mb-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-100 flex items-center justify-center">
            <Rocket className="w-5 h-5 text-blue-600" />
          </div>
          <div>
            <h2 className="font-bold text-gray-900">쇼핑몰 설정 가이드</h2>
            <p className="text-sm text-gray-500">
              {completedCount}/{data.steps.length} 완료 ({data.completionRate}%)
            </p>
          </div>
        </div>
        <button
          onClick={() => {
            setDismissed(true);
            localStorage.setItem("onboarding_dismissed", "true");
          }}
          className="text-gray-400 hover:text-gray-600"
          title="닫기"
        >
          <X size={18} />
        </button>
      </div>

      {/* Progress Bar */}
      <div className="w-full h-2 bg-gray-100 rounded-full mb-5 overflow-hidden">
        <div
          className="h-full bg-blue-600 rounded-full transition-all duration-500"
          style={{ width: `${data.completionRate}%` }}
        />
      </div>

      {/* Steps */}
      <div className="space-y-2">
        {data.steps.map((step) => {
          const Icon = STEP_ICONS[step.step] || Circle;
          const link = STEP_LINKS[step.step] || "/admin";
          const desc = STEP_DESCRIPTIONS[step.step] || "";
          const isNext = nextStep?.step === step.step;

          return (
            <button
              key={step.step}
              onClick={() => !step.completed && router.push(link)}
              disabled={step.completed}
              className={`w-full flex items-center gap-3 p-3 rounded-xl text-left transition ${
                step.completed
                  ? "bg-gray-50 opacity-60"
                  : isNext
                  ? "bg-blue-50 border border-blue-200 hover:bg-blue-100"
                  : "hover:bg-gray-50"
              }`}
            >
              {step.completed ? (
                <CheckCircle className="w-5 h-5 text-green-500 flex-shrink-0" />
              ) : (
                <Icon className={`w-5 h-5 flex-shrink-0 ${isNext ? "text-blue-600" : "text-gray-400"}`} />
              )}
              <div className="flex-1 min-w-0">
                <p className={`text-sm font-medium ${step.completed ? "line-through text-gray-400" : "text-gray-900"}`}>
                  {step.label}
                </p>
                {isNext && (
                  <p className="text-xs text-gray-500 mt-0.5">{desc}</p>
                )}
              </div>
              {!step.completed && (
                <ChevronRight className={`w-4 h-4 flex-shrink-0 ${isNext ? "text-blue-600" : "text-gray-300"}`} />
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}
