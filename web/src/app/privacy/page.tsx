"use client";

import { useTranslations } from "next-intl";
import { useTenantStore } from "@/stores/tenantStore";

export default function PrivacyPage() {
  const t = useTranslations();
  const { name } = useTenantStore();

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        {t("legal.privacyPolicy")}
      </h1>

      <div className="space-y-8 text-sm text-gray-700 leading-relaxed">
        {/* 1. 개인정보의 처리 목적 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section1Title")}
          </h2>
          <p>
            {name}(이하 &quot;회사&quot;)는 다음의 목적을 위하여 개인정보를
            처리합니다. 처리하고 있는 개인정보는 다음의 목적 이외의 용도로는
            이용되지 않으며, 이용 목적이 변경되는 경우에는 별도의 동의를 받는 등
            필요한 조치를 이행할 예정입니다.
          </p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>회원가입 및 관리: 회원제 서비스 이용에 따른 본인확인, 개인식별, 가입의사 확인</li>
            <li>재화 또는 서비스 제공: 물품배송, 서비스 제공, 콘텐츠 제공, 맞춤서비스 제공</li>
            <li>마케팅 및 광고에의 활용: 이벤트 및 광고성 정보 제공, 접속빈도 파악</li>
          </ul>
        </section>

        {/* 2. 개인정보의 처리 및 보유기간 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section2Title")}
          </h2>
          <p>
            회사는 법령에 따른 개인정보 보유·이용기간 또는 정보주체로부터
            개인정보를 수집 시에 동의 받은 개인정보 보유·이용기간 내에서
            개인정보를 처리·보유합니다.
          </p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>회원가입 정보: 회원 탈퇴 시까지</li>
            <li>전자상거래 거래기록: 5년 (전자상거래등에서의 소비자보호에 관한 법률)</li>
            <li>소비자 불만 또는 분쟁처리 기록: 3년</li>
            <li>접속 기록: 3개월 이상 (통신비밀보호법)</li>
          </ul>
        </section>

        {/* 3. 개인정보의 제3자 제공 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section3Title")}
          </h2>
          <p>
            회사는 정보주체의 개인정보를 제1조에서 명시한 범위 내에서만
            처리하며, 정보주체의 동의, 법률의 특별한 규정 등에 해당하는
            경우에만 개인정보를 제3자에게 제공합니다.
          </p>
        </section>

        {/* 4. 정보주체의 권리·의무 및 행사방법 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section4Title")}
          </h2>
          <p>정보주체는 회사에 대해 언제든지 다음 각 호의 개인정보 보호 관련 권리를 행사할 수 있습니다.</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>개인정보 열람 요구</li>
            <li>오류 등이 있을 경우 정정 요구</li>
            <li>삭제 요구</li>
            <li>처리정지 요구</li>
          </ul>
        </section>

        {/* 5. 처리하는 개인정보 항목 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section5Title")}
          </h2>
          <p>회사는 다음의 개인정보 항목을 처리하고 있습니다.</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>필수항목: 이름, 이메일, 비밀번호, 연락처</li>
            <li>선택항목: 주소, 생년월일</li>
            <li>자동수집항목: IP주소, 쿠키, 서비스 이용기록, 방문기록</li>
          </ul>
        </section>

        {/* 6. 개인정보의 안전성 확보조치 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section6Title")}
          </h2>
          <p>회사는 개인정보의 안전성 확보를 위해 다음과 같은 조치를 취하고 있습니다.</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>관리적 조치: 내부관리계획 수립·시행, 정기적 직원 교육</li>
            <li>기술적 조치: 개인정보처리시스템 등의 접근권한 관리, 암호화 기술 적용</li>
            <li>물리적 조치: 전산실, 자료보관실 등의 접근통제</li>
          </ul>
        </section>

        {/* 7. 개인정보 보호책임자 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            {t("legal.privacy.section7Title")}
          </h2>
          <p>
            회사는 개인정보 처리에 관한 업무를 총괄해서 책임지고, 개인정보
            처리와 관련한 정보주체의 불만처리 및 피해구제 등을 위하여
            아래와 같이 개인정보 보호책임자를 지정하고 있습니다.
          </p>
          <div className="mt-2 p-4 bg-gray-50 rounded-lg">
            <p>개인정보 보호책임자: {name} 관리자</p>
            <p>문의: 마이페이지 &gt; 1:1 문의를 이용해 주세요.</p>
          </div>
        </section>

        <p className="text-gray-400 text-xs pt-4 border-t border-gray-200">
          본 개인정보처리방침은 시행일로부터 적용되며, 변경사항이 있는 경우
          공지사항을 통하여 고지할 것입니다.
        </p>
      </div>
    </div>
  );
}
