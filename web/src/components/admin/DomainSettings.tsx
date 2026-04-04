"use client";

import { useEffect, useState } from "react";
import { Globe, ExternalLink, Copy, CheckCircle } from "lucide-react";
import api from "@/lib/api";
import toast from "react-hot-toast";

interface DomainInfo {
  customDomain: string | null;
  subdomain: string | null;
  shopUrl: string;
  customDomainUrl: string | null;
  dnsInstructions: { type: string; host: string; target: string }[];
}

export function DomainSettings() {
  const [domain, setDomain] = useState<DomainInfo | null>(null);
  const [newDomain, setNewDomain] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    api.get("/tenant-settings/domain")
      .then((res) => {
        setDomain(res.data);
        setNewDomain(res.data.customDomain || "");
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    setSaving(true);
    try {
      const res = await api.put("/tenant-settings/domain", { customDomain: newDomain || null });
      toast.success("도메인 설정이 저장되었습니다");
      // Refresh
      const updated = await api.get("/tenant-settings/domain");
      setDomain(updated.data);
    } catch (err: any) {
      toast.error(err.response?.data?.error || "저장 실패");
    } finally {
      setSaving(false);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast.success("클립보드에 복사되었습니다");
  };

  if (loading) return <div className="animate-pulse h-40 bg-gray-100 rounded-xl" />;

  return (
    <div className="bg-white rounded-xl shadow-sm p-6 space-y-5">
      <div className="flex items-center gap-3">
        <Globe className="w-5 h-5 text-[var(--color-primary)]" />
        <h2 className="font-semibold text-[var(--color-secondary)]">도메인 설정</h2>
      </div>

      {/* Current URLs */}
      <div className="space-y-2">
        <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
          <div>
            <p className="text-xs text-gray-500">기본 URL</p>
            <p className="text-sm font-medium text-blue-600">{domain?.shopUrl}</p>
          </div>
          <button onClick={() => copyToClipboard(domain?.shopUrl || "")} className="text-gray-400 hover:text-gray-600">
            <Copy size={16} />
          </button>
        </div>
        {domain?.customDomainUrl && (
          <div className="flex items-center justify-between p-3 bg-emerald-50 rounded-lg">
            <div>
              <p className="text-xs text-gray-500">커스텀 도메인</p>
              <p className="text-sm font-medium text-emerald-600">{domain.customDomainUrl}</p>
            </div>
            <a href={domain.customDomainUrl} target="_blank" rel="noopener noreferrer" className="text-emerald-500 hover:text-emerald-700">
              <ExternalLink size={16} />
            </a>
          </div>
        )}
      </div>

      {/* Set Custom Domain */}
      <div>
        <label className="block text-sm text-gray-600 mb-1">커스텀 도메인 (선택)</label>
        <div className="flex gap-2">
          <input
            type="text"
            value={newDomain}
            onChange={(e) => setNewDomain(e.target.value.toLowerCase().trim())}
            className="flex-1 px-3 py-2.5 border rounded-lg text-sm"
            placeholder="www.myshop.com"
          />
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-4 py-2.5 bg-[var(--color-primary)] text-white rounded-lg text-sm hover:opacity-90 disabled:opacity-50"
          >
            {saving ? "저장 중..." : "저장"}
          </button>
        </div>
      </div>

      {/* DNS Instructions */}
      {domain?.dnsInstructions && domain.dnsInstructions.length > 0 && (
        <div className="border-t pt-4">
          <h3 className="text-sm font-medium text-gray-700 mb-2">DNS 설정 안내</h3>
          <p className="text-xs text-gray-500 mb-3">
            도메인 관리 업체 (가비아, 카페24 등)에서 아래 DNS 레코드를 추가하세요.
          </p>
          <div className="overflow-hidden rounded-lg border">
            <table className="w-full text-xs">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">타입</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">호스트</th>
                  <th className="px-3 py-2 text-left font-medium text-gray-600">값</th>
                </tr>
              </thead>
              <tbody>
                {domain.dnsInstructions.map((dns, i) => (
                  <tr key={i} className="border-t">
                    <td className="px-3 py-2 font-mono font-medium">{dns.type}</td>
                    <td className="px-3 py-2 font-mono text-gray-600">{dns.host}</td>
                    <td className="px-3 py-2 font-mono text-blue-600">{dns.target}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="text-xs text-gray-400 mt-2">DNS 변경 후 반영까지 최대 48시간이 소요될 수 있습니다.</p>
        </div>
      )}
    </div>
  );
}
