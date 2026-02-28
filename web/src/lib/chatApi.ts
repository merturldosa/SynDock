import api from "./api";

export interface ChatMessageDto {
  role: "user" | "assistant";
  content: string;
}

export interface ChatResponse {
  content: string;
}

export async function sendChatMessage(
  messages: ChatMessageDto[]
): Promise<string> {
  const { data } = await api.post<ChatResponse>("/chat", {
    messages,
  });
  return data.content;
}
