"use client";

import { useTenantStore } from "@/stores/tenantStore";

export default function TermsPage() {
  const { name } = useTenantStore();

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        이용약관
      </h1>

      <div className="space-y-8 text-sm text-gray-700 leading-relaxed">
        {/* 1. 목적 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제1조 (목적)
          </h2>
          <p>
            이 약관은 {name}(이하 &quot;회사&quot;)가 운영하는 온라인
            쇼핑몰에서 제공하는 인터넷 관련 서비스를 이용함에 있어 회사와
            이용자의 권리·의무 및 책임사항을 규정함을 목적으로 합니다.
          </p>
        </section>

        {/* 2. 정의 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제2조 (정의)
          </h2>
          <ul className="list-disc pl-5 space-y-1">
            <li>&quot;쇼핑몰&quot;이란 회사가 재화 또는 용역을 이용자에게 제공하기 위하여 설정한 가상의 영업장을 말합니다.</li>
            <li>&quot;이용자&quot;란 쇼핑몰에 접속하여 이 약관에 따라 회사가 제공하는 서비스를 받는 회원 및 비회원을 말합니다.</li>
            <li>&quot;회원&quot;이라 함은 쇼핑몰에 회원등록을 한 자로서, 계속적으로 쇼핑몰이 제공하는 서비스를 이용할 수 있는 자를 말합니다.</li>
          </ul>
        </section>

        {/* 3. 약관의 게시와 개정 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제3조 (약관의 게시와 개정)
          </h2>
          <ul className="list-decimal pl-5 space-y-1">
            <li>회사는 이 약관의 내용을 이용자가 쉽게 알 수 있도록 서비스 초기 화면에 게시합니다.</li>
            <li>회사는 관련 법령을 위배하지 않는 범위에서 이 약관을 개정할 수 있습니다.</li>
            <li>개정된 약관은 적용일자 7일 이전부터 공지합니다.</li>
          </ul>
        </section>

        {/* 4. 서비스의 제공 및 변경 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제4조 (서비스의 제공 및 변경)
          </h2>
          <p>회사는 다음과 같은 업무를 수행합니다.</p>
          <ul className="list-disc pl-5 mt-2 space-y-1">
            <li>재화 또는 용역에 대한 정보 제공 및 구매계약의 체결</li>
            <li>구매계약이 체결된 재화 또는 용역의 배송</li>
            <li>기타 회사가 정하는 업무</li>
          </ul>
        </section>

        {/* 5. 구매신청 및 결제 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제5조 (구매신청 및 결제)
          </h2>
          <p>
            이용자는 쇼핑몰에서 다음의 방법으로 구매를 신청하며, 회사는
            이용자가 구매신청을 함에 있어서 각 내용을 알기 쉽게
            제공하여야 합니다.
          </p>
          <ul className="list-decimal pl-5 mt-2 space-y-1">
            <li>재화 등의 검색 및 선택</li>
            <li>받는 사람의 성명, 주소, 전화번호, 이메일주소 등의 입력</li>
            <li>약관내용, 청약철회권이 제한되는 서비스 등의 비용부담에 관한 확인</li>
            <li>이 약관에 동의하고 위 사항을 확인하거나 거부하는 표시</li>
            <li>결제방법의 선택</li>
          </ul>
        </section>

        {/* 6. 청약철회 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제6조 (청약철회 등)
          </h2>
          <ul className="list-decimal pl-5 space-y-1">
            <li>
              회사와 재화 등의 구매에 관한 계약을 체결한 이용자는 수신확인의
              통지를 받은 날부터 7일 이내에 청약의 철회를 할 수 있습니다.
            </li>
            <li>
              이용자는 재화 등을 배송 받은 경우 다음 각 호에 해당하는
              경우에는 반품 및 교환을 할 수 없습니다.
              <ul className="list-disc pl-5 mt-1 space-y-1">
                <li>이용자에게 책임 있는 사유로 재화 등이 멸실 또는 훼손된 경우</li>
                <li>이용자의 사용 또는 일부 소비에 의하여 재화 등의 가치가 현저히 감소한 경우</li>
                <li>복제가 가능한 재화 등의 포장을 훼손한 경우</li>
              </ul>
            </li>
          </ul>
        </section>

        {/* 7. 개인정보보호 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제7조 (개인정보보호)
          </h2>
          <p>
            회사는 이용자의 개인정보 수집 시 서비스제공을 위하여 필요한
            범위에서 최소한의 개인정보를 수집합니다. 자세한 사항은{" "}
            <a href="/privacy" className="text-[var(--color-primary)] hover:underline">
              개인정보처리방침
            </a>
            을 참조하시기 바랍니다.
          </p>
        </section>

        {/* 8. 분쟁해결 */}
        <section>
          <h2 className="text-lg font-semibold text-[var(--color-secondary)] mb-3">
            제8조 (분쟁해결)
          </h2>
          <ul className="list-decimal pl-5 space-y-1">
            <li>
              회사는 이용자가 제기하는 정당한 의견이나 불만을 반영하고 그
              피해를 보상처리하기 위하여 피해보상처리기구를 설치·운영합니다.
            </li>
            <li>
              회사와 이용자 간에 발생한 전자상거래 분쟁에 관하여는
              이용자의 피해구제신청에 따라 공정거래위원회 또는
              시·도지사가 의뢰하는 분쟁조정기관의 조정에 따를 수 있습니다.
            </li>
          </ul>
        </section>

        <p className="text-gray-400 text-xs pt-4 border-t border-gray-200">
          본 약관은 시행일로부터 적용되며, 변경사항이 있는 경우 공지사항을
          통하여 고지할 것입니다.
        </p>
      </div>
    </div>
  );
}
