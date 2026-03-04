"use client";

import { ShoppingCart, ClipboardList, CreditCard, CheckCircle } from "lucide-react";
import { useTranslations } from "next-intl";

interface CheckoutStepsProps {
  currentStep: "cart" | "order" | "payment" | "complete";
}

const STEPS = [
  { key: "cart", icon: ShoppingCart },
  { key: "order", icon: ClipboardList },
  { key: "payment", icon: CreditCard },
  { key: "complete", icon: CheckCircle },
] as const;

export function CheckoutSteps({ currentStep }: CheckoutStepsProps) {
  const t = useTranslations("checkout");
  const currentIndex = STEPS.findIndex((s) => s.key === currentStep);

  return (
    <div className="max-w-2xl mx-auto mb-8">
      <div className="flex items-center justify-between">
        {STEPS.map((step, i) => {
          const Icon = step.icon;
          const isActive = i === currentIndex;
          const isDone = i < currentIndex;

          return (
            <div key={step.key} className="flex items-center flex-1 last:flex-none">
              <div className="flex flex-col items-center">
                <div
                  className={`w-10 h-10 rounded-full flex items-center justify-center transition-colors ${
                    isDone
                      ? "bg-[var(--color-primary)] text-white"
                      : isActive
                        ? "bg-[var(--color-primary)] text-white ring-4 ring-[var(--color-primary)]/20"
                        : "bg-gray-100 text-gray-400"
                  }`}
                >
                  <Icon size={18} />
                </div>
                <span
                  className={`text-xs mt-1.5 font-medium whitespace-nowrap ${
                    isDone || isActive ? "text-[var(--color-primary)]" : "text-gray-400"
                  }`}
                >
                  {t(`steps.${step.key}`)}
                </span>
              </div>
              {i < STEPS.length - 1 && (
                <div
                  className={`flex-1 h-0.5 mx-2 mt-[-18px] transition-colors ${
                    isDone ? "bg-[var(--color-primary)]" : "bg-gray-200"
                  }`}
                />
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
