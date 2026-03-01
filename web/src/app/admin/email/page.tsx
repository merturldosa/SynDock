"use client";

import { useState } from "react";
import { Send, Eye } from "lucide-react";
import { sendMarketingEmail } from "@/lib/adminApi";

export default function AdminEmailPage() {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [target, setTarget] = useState("all");
  const [sending, setSending] = useState(false);
  const [showPreview, setShowPreview] = useState(false);

  const handleSend = async () => {
    if (!title.trim() || !content.trim()) {
      alert("제목과 내용을 입력해주세요.");
      return;
    }
    if (!confirm(`${target === "all" ? "전체" : "VIP"} 회원에게 이메일을 발송하시겠습니까?`))
      return;

    setSending(true);
    try {
      const { sentCount } = await sendMarketingEmail(title, content, target);
      alert(`${sentCount}명에게 이메일을 발송했습니다.`);
      setTitle("");
      setContent("");
    } catch {
      alert("이메일 발송에 실패했습니다.");
    }
    setSending(false);
  };

  return (
    <div className="max-w-3xl">
      <h1 className="text-2xl font-bold text-[var(--color-secondary)] mb-6">
        마케팅 이메일
      </h1>

      <div className="space-y-6">
        <div className="bg-white rounded-xl shadow-sm p-6">
          <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
            이메일 작성
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm text-gray-500 mb-1">대상</label>
              <select
                value={target}
                onChange={(e) => setTarget(e.target.value)}
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              >
                <option value="all">전체 회원</option>
                <option value="vip">VIP 회원</option>
              </select>
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">제목</label>
              <input
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="이메일 제목을 입력하세요"
                className="w-full px-3 py-2.5 border rounded-lg text-sm"
              />
            </div>
            <div>
              <label className="block text-sm text-gray-500 mb-1">
                내용 (HTML 지원)
              </label>
              <textarea
                value={content}
                onChange={(e) => setContent(e.target.value)}
                rows={10}
                placeholder="이메일 본문을 작성하세요. HTML 태그를 사용할 수 있습니다."
                className="w-full px-3 py-2.5 border rounded-lg text-sm resize-none font-mono"
              />
            </div>
          </div>
        </div>

        <div className="flex gap-3">
          <button
            onClick={() => setShowPreview(!showPreview)}
            className="flex-1 flex items-center justify-center gap-2 py-3 border rounded-lg text-sm font-medium text-gray-500 hover:bg-gray-50"
          >
            <Eye size={16} />
            {showPreview ? "미리보기 닫기" : "미리보기"}
          </button>
          <button
            onClick={handleSend}
            disabled={sending || !title.trim() || !content.trim()}
            className="flex-1 flex items-center justify-center gap-2 py-3 bg-[var(--color-primary)] text-white rounded-lg text-sm font-medium hover:opacity-90 disabled:opacity-60"
          >
            <Send size={16} />
            {sending ? "발송 중..." : "이메일 발송"}
          </button>
        </div>

        {showPreview && (
          <div className="bg-white rounded-xl shadow-sm p-6">
            <h2 className="font-semibold text-[var(--color-secondary)] mb-4">
              미리보기
            </h2>
            <div className="border rounded-lg p-4">
              <div className="bg-[var(--color-primary)] text-white p-4 rounded-t-lg -m-4 mb-4">
                <h3 className="font-bold">{title || "(제목 없음)"}</h3>
              </div>
              <div
                className="prose prose-sm max-w-none mt-4"
                dangerouslySetInnerHTML={{
                  __html: content || "<p style='color:#999'>내용이 없습니다.</p>",
                }}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
