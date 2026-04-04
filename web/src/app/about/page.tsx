"use client";

import { useEffect, useState } from "react";
import { MapPin, Phone, Mail, Building2, User, FileText } from "lucide-react";
import { useTenantStore } from "@/stores/tenantStore";

export default function AboutPage() {
  const { config, name: tenantName, theme } = useTenantStore();
  const [mounted, setMounted] = useState(false);

  useEffect(() => { setMounted(true); }, []);

  if (!mounted) return null;

  const companyName = (config as Record<string, unknown>)?.companyName as string || tenantName;
  const companyAddress = (config as Record<string, unknown>)?.companyAddress as string || "";
  const ceoName = (config as Record<string, unknown>)?.ceoName as string || "";
  const contactPhone = (config as Record<string, unknown>)?.contactPhone as string || "";
  const contactEmail = (config as Record<string, unknown>)?.contactEmail as string || "";
  const businessNumber = (config as Record<string, unknown>)?.businessNumber as string || "";
  const heroDescription = (config as Record<string, unknown>)?.heroDescription as string || "";

  return (
    <div className="max-w-3xl mx-auto px-4 py-12">
      {/* Hero */}
      <div className="text-center mb-12">
        <h1 className="text-3xl font-bold text-[var(--color-secondary)] mb-3">{companyName}</h1>
        {heroDescription && (
          <p className="text-gray-600 whitespace-pre-line leading-relaxed">{heroDescription}</p>
        )}
      </div>

      {/* Company Info Card */}
      <div className="bg-white rounded-2xl shadow-sm border p-8 space-y-6">
        <h2 className="text-xl font-semibold text-[var(--color-secondary)] border-b pb-3">회사 정보</h2>

        <div className="grid gap-4">
          {companyName && (
            <div className="flex items-start gap-3">
              <Building2 size={20} className="text-[var(--color-primary)] mt-0.5 flex-shrink-0" />
              <div>
                <p className="text-sm text-gray-500">회사명</p>
                <p className="font-medium text-[var(--color-secondary)]">{companyName}</p>
              </div>
            </div>
          )}

          {ceoName && (
            <div className="flex items-start gap-3">
              <User size={20} className="text-[var(--color-primary)] mt-0.5 flex-shrink-0" />
              <div>
                <p className="text-sm text-gray-500">대표자</p>
                <p className="font-medium text-[var(--color-secondary)]">{ceoName}</p>
              </div>
            </div>
          )}

          {businessNumber && (
            <div className="flex items-start gap-3">
              <FileText size={20} className="text-[var(--color-primary)] mt-0.5 flex-shrink-0" />
              <div>
                <p className="text-sm text-gray-500">사업자등록번호</p>
                <p className="font-medium text-[var(--color-secondary)]">{businessNumber}</p>
              </div>
            </div>
          )}

          {companyAddress && (
            <div className="flex items-start gap-3">
              <MapPin size={20} className="text-[var(--color-primary)] mt-0.5 flex-shrink-0" />
              <div>
                <p className="text-sm text-gray-500">주소</p>
                <p className="font-medium text-[var(--color-secondary)]">{companyAddress}</p>
              </div>
            </div>
          )}

          {contactPhone && (
            <div className="flex items-start gap-3">
              <Phone size={20} className="text-[var(--color-primary)] mt-0.5 flex-shrink-0" />
              <div>
                <p className="text-sm text-gray-500">전화번호</p>
                <a href={`tel:${contactPhone}`} className="font-medium text-[var(--color-primary)] hover:underline">
                  {contactPhone}
                </a>
              </div>
            </div>
          )}

          {contactEmail && (
            <div className="flex items-start gap-3">
              <Mail size={20} className="text-[var(--color-primary)] mt-0.5 flex-shrink-0" />
              <div>
                <p className="text-sm text-gray-500">이메일</p>
                <a href={`mailto:${contactEmail}`} className="font-medium text-[var(--color-primary)] hover:underline">
                  {contactEmail}
                </a>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Footer note */}
      <p className="text-center text-xs text-gray-400 mt-8">
        Powered by SynDock Platform
      </p>
    </div>
  );
}
