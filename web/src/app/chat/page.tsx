"use client";

import { useState, useRef, useEffect } from "react";
import { Send, Bot, User, Loader2 } from "lucide-react";
import { sendChatMessage, type ChatMessageDto } from "@/lib/chatApi";
import { useAuthStore } from "@/stores/authStore";

interface Message {
  role: "user" | "assistant";
  content: string;
}

export default function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([
    {
      role: "assistant",
      content:
        "안녕하세요! 쇼핑에 관한 궁금한 점이 있으시면 무엇이든 물어보세요. 상품 추천, 주문 문의, 배송 관련 질문 등 도움을 드리겠습니다.",
    },
  ]);
  const [input, setInput] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const user = useAuthStore((s) => s.user);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const handleSend = async () => {
    const trimmed = input.trim();
    if (!trimmed || isLoading) return;

    const userMessage: Message = { role: "user", content: trimmed };
    const newMessages = [...messages, userMessage];
    setMessages(newMessages);
    setInput("");
    setIsLoading(true);

    try {
      const apiMessages: ChatMessageDto[] = newMessages
        .filter((m) => m.role === "user" || m.role === "assistant")
        .slice(-10)
        .map((m) => ({ role: m.role, content: m.content }));

      const response = await sendChatMessage(apiMessages);
      setMessages((prev) => [
        ...prev,
        { role: "assistant", content: response },
      ]);
    } catch {
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          content: "죄송합니다. 일시적인 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.",
        },
      ]);
    } finally {
      setIsLoading(false);
      inputRef.current?.focus();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="min-h-screen bg-[var(--color-background)] pt-20">
      <div className="max-w-3xl mx-auto px-4 py-8 flex flex-col" style={{ height: "calc(100vh - 80px)" }}>
        {/* Header */}
        <div className="text-center mb-6">
          <div className="inline-flex items-center gap-2 bg-white rounded-full px-4 py-2 shadow-sm border border-gray-200">
            <Bot className="w-5 h-5 text-[var(--color-primary)]" />
            <span className="font-medium text-gray-800">AI 쇼핑 어시스턴트</span>
          </div>
        </div>

        {/* Messages */}
        <div className="flex-1 overflow-y-auto space-y-4 pb-4">
          {messages.map((msg, i) => (
            <div
              key={i}
              className={`flex items-start gap-3 ${
                msg.role === "user" ? "flex-row-reverse" : ""
              }`}
            >
              {/* Avatar */}
              <div
                className={`flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center ${
                  msg.role === "user"
                    ? "bg-[var(--color-primary)] text-white"
                    : "bg-gray-200 text-gray-600"
                }`}
              >
                {msg.role === "user" ? (
                  <User className="w-4 h-4" />
                ) : (
                  <Bot className="w-4 h-4" />
                )}
              </div>

              {/* Bubble */}
              <div
                className={`max-w-[75%] rounded-2xl px-4 py-3 ${
                  msg.role === "user"
                    ? "bg-[var(--color-primary)] text-white"
                    : "bg-white text-gray-800 border border-gray-200 shadow-sm"
                }`}
              >
                <p className="text-sm whitespace-pre-wrap leading-relaxed">
                  {msg.content}
                </p>
              </div>
            </div>
          ))}

          {isLoading && (
            <div className="flex items-start gap-3">
              <div className="flex-shrink-0 w-8 h-8 rounded-full bg-gray-200 text-gray-600 flex items-center justify-center">
                <Bot className="w-4 h-4" />
              </div>
              <div className="bg-white text-gray-500 rounded-2xl px-4 py-3 border border-gray-200 shadow-sm">
                <div className="flex items-center gap-2">
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span className="text-sm">답변을 작성하고 있습니다...</span>
                </div>
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Input */}
        <div className="border-t border-gray-200 pt-4">
          {!user ? (
            <div className="text-center py-3 text-sm text-gray-500">
              AI 채팅을 이용하려면{" "}
              <a href="/login" className="text-[var(--color-primary)] hover:underline font-medium">
                로그인
              </a>
              이 필요합니다.
            </div>
          ) : (
            <div className="flex items-center gap-3">
              <input
                ref={inputRef}
                type="text"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="메시지를 입력하세요..."
                disabled={isLoading}
                className="flex-1 rounded-full border border-gray-300 px-5 py-3 text-sm focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)] disabled:opacity-50"
              />
              <button
                onClick={handleSend}
                disabled={!input.trim() || isLoading}
                className="flex-shrink-0 w-11 h-11 rounded-full bg-[var(--color-primary)] text-white flex items-center justify-center hover:opacity-90 transition-opacity disabled:opacity-40"
              >
                <Send className="w-4 h-4" />
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
