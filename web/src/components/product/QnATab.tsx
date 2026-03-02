"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { MessageCircle, Lock, Trash2 } from "lucide-react";
import { getProductQnAs, createQnA, deleteQnA } from "@/lib/reviewApi";
import { useAuthStore } from "@/stores/authStore";
import type { PagedQnA } from "@/types/review";

interface QnATabProps {
  productId: number;
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString("ko-KR", { year: "numeric", month: "short", day: "numeric" });
}

export function QnATab({ productId }: QnATabProps) {
  const t = useTranslations();
  const { isAuthenticated, user } = useAuthStore();
  const [data, setData] = useState<PagedQnA | null>(null);
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [isSecret, setIsSecret] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const load = () => {
    getProductQnAs(productId, page).then(setData).catch(() => {});
  };

  useEffect(() => { load(); }, [productId, page]);

  const handleSubmit = async () => {
    if (!title.trim() || !content.trim()) return;
    setSubmitting(true);
    try {
      await createQnA(productId, title, content, isSecret);
      setShowForm(false);
      setTitle("");
      setContent("");
      setIsSecret(false);
      load();
    } catch {
      alert(t("qna.writeFailed"));
    }
    setSubmitting(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm(t("qna.deleteConfirm"))) return;
    try {
      await deleteQnA(id);
      load();
    } catch {
      alert(t("qna.deleteFailed"));
    }
  };

  if (!data) return <div className="py-8 text-center text-gray-400">{t("common.loading")}</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <p className="text-sm text-gray-500">{t("qna.totalCount", { count: data.totalCount })}</p>
        {isAuthenticated && (
          <button
            onClick={() => setShowForm(!showForm)}
            className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90"
          >
            {t("qna.writeQuestion")}
          </button>
        )}
      </div>

      {showForm && (
        <div className="bg-gray-50 rounded-xl p-4 mb-6 space-y-3">
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t("qna.questionTitle")}
            className="w-full px-3 py-2 border rounded-lg text-sm"
          />
          <textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder={t("qna.questionContent")}
            className="w-full px-3 py-2 border rounded-lg text-sm resize-none h-24"
          />
          <div className="flex items-center justify-between">
            <label className="flex items-center gap-2 text-sm text-gray-500">
              <input type="checkbox" checked={isSecret} onChange={(e) => setIsSecret(e.target.checked)} />
              <Lock size={14} /> {t("qna.secret")}
            </label>
            <div className="flex gap-2">
              <button onClick={() => setShowForm(false)} className="px-4 py-2 text-sm text-gray-500 hover:text-gray-700">{t("common.cancel")}</button>
              <button
                onClick={handleSubmit}
                disabled={submitting || !title.trim() || !content.trim()}
                className="px-4 py-2 text-sm bg-[var(--color-primary)] text-white rounded-lg hover:opacity-90 disabled:opacity-60"
              >
                {submitting ? t("common.submitting") : t("common.submit")}
              </button>
            </div>
          </div>
        </div>
      )}

      {data.items.length === 0 ? (
        <p className="text-center text-gray-400 py-8">{t("qna.noQuestions")}</p>
      ) : (
        <div className="space-y-4">
          {data.items.map((qna) => (
            <div key={qna.id} className="border-b border-gray-100 pb-4">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    {qna.isSecret && <Lock size={12} className="text-gray-400" />}
                    <span className={`text-xs px-2 py-0.5 rounded-full ${qna.isAnswered ? "bg-emerald-100 text-emerald-700" : "bg-yellow-100 text-yellow-700"}`}>
                      {qna.isAnswered ? t("qna.answered") : t("qna.waiting")}
                    </span>
                    <span className="text-sm font-medium text-[var(--color-secondary)]">{qna.userName}</span>
                    <span className="text-xs text-gray-400">{formatDate(qna.createdAt)}</span>
                  </div>
                  <p className="text-sm font-medium text-[var(--color-secondary)]">{qna.title}</p>
                  {qna.content && <p className="text-sm text-gray-500 mt-1">{qna.content}</p>}

                  {qna.reply && (
                    <div className="mt-3 ml-4 p-3 bg-gray-50 rounded-lg border-l-2 border-[var(--color-primary)]">
                      <div className="flex items-center gap-2 mb-1">
                        <MessageCircle size={12} className="text-[var(--color-primary)]" />
                        <span className="text-xs font-medium text-[var(--color-primary)]">{qna.reply.userName}</span>
                        <span className="text-xs text-gray-400">{formatDate(qna.reply.createdAt)}</span>
                      </div>
                      <p className="text-sm text-gray-600">{qna.reply.content}</p>
                    </div>
                  )}
                </div>
                {user?.id === qna.userId && (
                  <button onClick={() => handleDelete(qna.id)} className="text-gray-400 hover:text-red-500 ml-2">
                    <Trash2 size={14} />
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
