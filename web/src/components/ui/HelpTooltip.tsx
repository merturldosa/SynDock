"use client";

import { useState, useRef, useEffect } from "react";
import { HelpCircle, ExternalLink, X } from "lucide-react";

interface HelpStep {
  title: string;
  description: string;
}

interface HelpTooltipProps {
  title: string;
  description: string;
  steps?: HelpStep[];
  externalLink?: { label: string; url: string };
  placeholder?: string;
}

export function HelpTooltip({
  title,
  description,
  steps,
  externalLink,
}: HelpTooltipProps) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    if (open) document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [open]);

  return (
    <div className="relative inline-block" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="ml-1.5 text-gray-400 hover:text-blue-500 transition inline-flex items-center"
        aria-label={`${title} 도움말`}
      >
        <HelpCircle size={15} />
      </button>

      {open && (
        <div className="absolute z-50 left-0 top-6 w-80 bg-white rounded-xl shadow-xl border p-4 text-left animate-in fade-in slide-in-from-top-1 duration-200">
          <div className="flex items-start justify-between mb-2">
            <h4 className="text-sm font-semibold text-gray-900">{title}</h4>
            <button
              onClick={() => setOpen(false)}
              className="text-gray-400 hover:text-gray-600"
            >
              <X size={14} />
            </button>
          </div>

          <p className="text-xs text-gray-600 leading-relaxed mb-3">
            {description}
          </p>

          {steps && steps.length > 0 && (
            <div className="space-y-2 mb-3">
              {steps.map((step, i) => (
                <div key={i} className="flex gap-2">
                  <span className="flex-shrink-0 w-5 h-5 rounded-full bg-blue-100 text-blue-600 text-xs flex items-center justify-center font-medium">
                    {i + 1}
                  </span>
                  <div>
                    <p className="text-xs font-medium text-gray-800">
                      {step.title}
                    </p>
                    <p className="text-xs text-gray-500">{step.description}</p>
                  </div>
                </div>
              ))}
            </div>
          )}

          {externalLink && (
            <a
              href={externalLink.url}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-700 font-medium"
            >
              <ExternalLink size={12} />
              {externalLink.label}
            </a>
          )}
        </div>
      )}
    </div>
  );
}

// Pre-defined help configs for common fields
export const HELP_CONFIGS = {
  openAiApiKey: {
    title: "OpenAI API Key 발급 방법",
    description:
      "OpenAI API Key는 GPT 챗봇과 DALL-E 이미지 생성에 사용됩니다. 키가 없으면 AI 기능이 비활성화됩니다.",
    steps: [
      {
        title: "OpenAI 계정 생성",
        description: "platform.openai.com 에서 회원가입",
      },
      {
        title: "API Keys 메뉴 이동",
        description: "좌측 메뉴 > API Keys 클릭",
      },
      {
        title: "새 키 생성",
        description: '"Create new secret key" 클릭 후 키 복사',
      },
      {
        title: "결제 설정",
        description: "Billing > Payment methods에서 카드 등록 (종량제)",
      },
    ],
    externalLink: {
      label: "OpenAI API Key 발급 페이지",
      url: "https://platform.openai.com/api-keys",
    },
  },
  claudeApiKey: {
    title: "Claude API Key 발급 방법",
    description:
      "Anthropic Claude API Key는 AI 챗봇 대화에 사용됩니다. Claude가 더 자연스러운 한국어 대화를 제공합니다.",
    steps: [
      {
        title: "Anthropic Console 접속",
        description: "console.anthropic.com 에서 회원가입",
      },
      {
        title: "API Keys 메뉴",
        description: "Settings > API Keys 클릭",
      },
      {
        title: "키 생성",
        description: '"Create Key" 클릭, 이름 입력 후 키 복사',
      },
      {
        title: "크레딧 충전",
        description: "Plans & Billing에서 크레딧 구매 (최소 $5)",
      },
    ],
    externalLink: {
      label: "Anthropic Console",
      url: "https://console.anthropic.com/settings/keys",
    },
  },
  tossPaymentsKey: {
    title: "TossPayments 키 발급 방법",
    description:
      "TossPayments 연동으로 카드결제, 계좌이체, 가상계좌를 지원합니다. 테스트 키로 먼저 연동 후 실 키로 전환하세요.",
    steps: [
      {
        title: "TossPayments 가입",
        description: "developers.tosspayments.com 에서 가맹점 등록",
      },
      {
        title: "개발 정보 확인",
        description: "대시보드 > 개발 정보 메뉴",
      },
      {
        title: "테스트 키 복사",
        description: "Client Key (test_ck_...), Secret Key (test_sk_...) 복사",
      },
      {
        title: "실서비스 전환",
        description: "심사 완료 후 라이브 키로 교체",
      },
    ],
    externalLink: {
      label: "TossPayments 개발자 센터",
      url: "https://developers.tosspayments.com",
    },
  },
  kakaoAlimtalk: {
    title: "카카오 알림톡 설정 방법",
    description:
      "주문확인, 배송시작, 배송완료 알림을 카카오톡으로 자동 발송합니다.",
    steps: [
      {
        title: "카카오 비즈니스 가입",
        description: "business.kakao.com 에서 채널 생성",
      },
      {
        title: "알림톡 API 신청",
        description: "카카오 디벨로퍼스 > 알림톡 API 사용 신청",
      },
      {
        title: "템플릿 등록",
        description: "주문확인/배송 템플릿 등록 및 검수",
      },
      {
        title: "API Key 발급",
        description: "앱 키 > REST API 키 복사",
      },
    ],
    externalLink: {
      label: "카카오 비즈니스",
      url: "https://business.kakao.com",
    },
  },
} as const;
