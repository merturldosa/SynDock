"use client";

import { useEffect, useState } from "react";
import {
  getCommissionSummary, getSettlements, createSettlement, processSettlement,
  type CommissionSummaryItem, type SettlementDto,
} from "@/lib/adminApi";
import { DollarSign, Building2, Clock, CheckCircle, AlertCircle, Plus, Send } from "lucide-react";

function formatPrice(n: number) {
  return n.toLocaleString("ko-KR") + "원";
}

function StatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    Pending: "bg-yellow-100 text-yellow-700",
    Ready: "bg-blue-100 text-blue-700",
    Processing: "bg-orange-100 text-orange-700",
    Completed: "bg-green-100 text-green-700",
    Failed: "bg-red-100 text-red-700",
  };
  return (
    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${styles[status] || "bg-gray-100 text-gray-600"}`}>
      {status}
    </span>
  );
}

export default function SettlementsPage() {
  const [summary, setSummary] = useState<CommissionSummaryItem[]>([]);
  const [selectedTenant, setSelectedTenant] = useState<string | null>(null);
  const [settlements, setSettlements] = useState<SettlementDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [settlementsLoading, setSettlementsLoading] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [periodStart, setPeriodStart] = useState("");
  const [periodEnd, setPeriodEnd] = useState("");
  const [processId, setProcessId] = useState<number | null>(null);
  const [transactionId, setTransactionId] = useState("");
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    getCommissionSummary()
      .then(setSummary)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const loadSettlements = async (tenantName: string) => {
    const tenant = summary.find((s) => s.tenantName === tenantName);
    if (!tenant) return;
    setSelectedTenant(tenantName);
    setSettlementsLoading(true);
    // Use tenant name as slug approximation (lowercase)
    try {
      const data = await getSettlements(tenantName.toLowerCase());
      setSettlements(data);
    } catch {
      setSettlements([]);
    }
    setSettlementsLoading(false);
  };

  const handleCreate = async () => {
    if (!selectedTenant || !periodStart || !periodEnd) return;
    try {
      await createSettlement(selectedTenant.toLowerCase(), periodStart, periodEnd);
      setMessage("정산이 생성되었습니다.");
      setShowCreate(false);
      loadSettlements(selectedTenant);
    } catch {
      setMessage("정산 생성에 실패했습니다.");
    }
  };

  const handleProcess = async () => {
    if (!processId || !transactionId) return;
    try {
      await processSettlement(processId, transactionId, "PlatformAdmin");
      setMessage("정산이 처리되었습니다.");
      setProcessId(null);
      setTransactionId("");
      if (selectedTenant) loadSettlements(selectedTenant);
    } catch {
      setMessage("정산 처리에 실패했습니다.");
    }
  };

  const totalPending = summary.reduce((s, t) => s + t.pendingSettlement, 0);
  const totalCommission = summary.reduce((s, t) => s + t.totalCommission, 0);
  const totalSettled = summary.reduce((s, t) => s + t.totalSettlementAmount, 0);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-blue-600 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">정산 관리</h1>
        <p className="text-sm text-gray-500 mt-1">테넌트별 수수료 및 정산 현황</p>
      </div>

      {/* Toast */}
      {message && (
        <div className="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-3 rounded-lg text-sm flex items-center justify-between">
          {message}
          <button onClick={() => setMessage(null)} className="text-blue-500 hover:text-blue-700">×</button>
        </div>
      )}

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-green-50 flex items-center justify-center">
              <DollarSign className="w-5 h-5 text-green-600" />
            </div>
            <span className="text-sm text-gray-500">총 수수료 수입</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(totalCommission)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center">
              <CheckCircle className="w-5 h-5 text-blue-600" />
            </div>
            <span className="text-sm text-gray-500">총 정산 완료</span>
          </div>
          <p className="text-2xl font-bold text-gray-900">{formatPrice(totalSettled)}</p>
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 rounded-lg bg-orange-50 flex items-center justify-center">
              <Clock className="w-5 h-5 text-orange-600" />
            </div>
            <span className="text-sm text-gray-500">미정산 잔액</span>
          </div>
          <p className="text-2xl font-bold text-orange-600">{formatPrice(totalPending)}</p>
        </div>
      </div>

      {/* Tenant Commission Summary */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="font-semibold text-gray-900">테넌트별 수수료 현황</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left font-medium text-gray-500">테넌트</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">주문 수</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">주문 금액</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">수수료</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">정산 금액</th>
                <th className="px-6 py-3 text-right font-medium text-gray-500">미정산</th>
                <th className="px-6 py-3 text-center font-medium text-gray-500">관리</th>
              </tr>
            </thead>
            <tbody>
              {summary.length === 0 ? (
                <tr><td colSpan={7} className="px-6 py-8 text-center text-gray-400">수수료 데이터가 없습니다.</td></tr>
              ) : (
                summary.map((t) => (
                  <tr key={t.tenantId} className={`border-t border-gray-100 hover:bg-gray-50 ${selectedTenant === t.tenantName ? "bg-blue-50" : ""}`}>
                    <td className="px-6 py-3">
                      <div className="flex items-center gap-2">
                        <Building2 size={16} className="text-gray-400" />
                        <span className="font-medium text-gray-900">{t.tenantName}</span>
                      </div>
                    </td>
                    <td className="px-6 py-3 text-right">{t.totalOrders}건</td>
                    <td className="px-6 py-3 text-right">{formatPrice(t.totalOrderAmount)}</td>
                    <td className="px-6 py-3 text-right text-green-600 font-medium">{formatPrice(t.totalCommission)}</td>
                    <td className="px-6 py-3 text-right">{formatPrice(t.totalSettlementAmount)}</td>
                    <td className="px-6 py-3 text-right">
                      {t.pendingSettlement > 0 ? (
                        <span className="text-orange-600 font-medium">{formatPrice(t.pendingSettlement)}</span>
                      ) : (
                        <span className="text-gray-400">-</span>
                      )}
                    </td>
                    <td className="px-6 py-3 text-center">
                      <button
                        onClick={() => loadSettlements(t.tenantName)}
                        className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                      >
                        정산 내역
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Selected Tenant Settlements */}
      {selectedTenant && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900">{selectedTenant} 정산 내역</h2>
            <button onClick={() => setShowCreate(true)}
              className="flex items-center gap-1 px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-medium hover:bg-blue-700">
              <Plus size={14} /> 정산 생성
            </button>
          </div>

          {/* Create Settlement Form */}
          {showCreate && (
            <div className="px-6 py-4 bg-blue-50 border-b border-blue-200">
              <div className="flex items-center gap-3 flex-wrap">
                <input type="date" value={periodStart} onChange={(e) => setPeriodStart(e.target.value)}
                  className="px-3 py-2 border rounded-lg text-sm" placeholder="시작일" />
                <span className="text-gray-400">~</span>
                <input type="date" value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value)}
                  className="px-3 py-2 border rounded-lg text-sm" placeholder="종료일" />
                <button onClick={handleCreate}
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700">생성</button>
                <button onClick={() => setShowCreate(false)}
                  className="px-4 py-2 bg-gray-200 text-gray-600 rounded-lg text-sm hover:bg-gray-300">취소</button>
              </div>
            </div>
          )}

          {settlementsLoading ? (
            <div className="flex items-center justify-center py-10">
              <div className="h-6 w-6 animate-spin rounded-full border-4 border-blue-600 border-t-transparent" />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left font-medium text-gray-500">기간</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">주문 수</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">주문 합계</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">수수료</th>
                    <th className="px-4 py-3 text-right font-medium text-gray-500">정산 금액</th>
                    <th className="px-4 py-3 text-center font-medium text-gray-500">상태</th>
                    <th className="px-4 py-3 text-left font-medium text-gray-500">은행 정보</th>
                    <th className="px-4 py-3 text-center font-medium text-gray-500">작업</th>
                  </tr>
                </thead>
                <tbody>
                  {settlements.length === 0 ? (
                    <tr><td colSpan={8} className="px-4 py-8 text-center text-gray-400">정산 내역이 없습니다.</td></tr>
                  ) : (
                    settlements.map((s) => (
                      <tr key={s.id} className="border-t border-gray-100 hover:bg-gray-50">
                        <td className="px-4 py-3 text-gray-700 whitespace-nowrap">
                          {s.periodStart.slice(0, 10)} ~ {s.periodEnd.slice(0, 10)}
                        </td>
                        <td className="px-4 py-3 text-right">{s.orderCount}건</td>
                        <td className="px-4 py-3 text-right">{formatPrice(s.totalOrderAmount)}</td>
                        <td className="px-4 py-3 text-right text-green-600">{formatPrice(s.totalCommission)}</td>
                        <td className="px-4 py-3 text-right font-medium">{formatPrice(s.totalSettlementAmount)}</td>
                        <td className="px-4 py-3 text-center"><StatusBadge status={s.status} /></td>
                        <td className="px-4 py-3 text-gray-500 text-xs">
                          {s.bankName ? `${s.bankName} ${s.bankAccount}` : "-"}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {(s.status === "Ready" || s.status === "Pending") && (
                            <button onClick={() => setProcessId(s.id)}
                              className="flex items-center gap-1 px-2 py-1 bg-green-600 text-white rounded text-xs hover:bg-green-700 mx-auto">
                              <Send size={12} /> 처리
                            </button>
                          )}
                          {s.status === "Completed" && s.transactionId && (
                            <span className="text-xs text-gray-400">TX: {s.transactionId}</span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Process Settlement Modal */}
      {processId && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl">
            <h3 className="text-lg font-semibold mb-4">정산 처리</h3>
            <p className="text-sm text-gray-500 mb-4">이체 완료 후 거래번호를 입력하세요.</p>
            <input
              type="text"
              value={transactionId}
              onChange={(e) => setTransactionId(e.target.value)}
              placeholder="거래번호 (Transaction ID)"
              className="w-full px-4 py-2 border rounded-lg text-sm mb-4"
            />
            <div className="flex gap-2 justify-end">
              <button onClick={() => { setProcessId(null); setTransactionId(""); }}
                className="px-4 py-2 bg-gray-200 text-gray-600 rounded-lg text-sm hover:bg-gray-300">취소</button>
              <button onClick={handleProcess} disabled={!transactionId}
                className="px-4 py-2 bg-green-600 text-white rounded-lg text-sm font-medium hover:bg-green-700 disabled:opacity-50">
                정산 완료
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
