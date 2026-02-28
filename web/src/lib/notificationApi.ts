import api from "./api";

export interface NotificationDto {
  id: number;
  type: string;
  title: string;
  message: string | null;
  isRead: boolean;
  readAt: string | null;
  referenceId: number | null;
  referenceType: string | null;
  createdAt: string;
}

export interface PagedNotifications {
  items: NotificationDto[];
  totalCount: number;
}

export interface UnreadCountDto {
  count: number;
}

export async function getNotifications(
  page = 1,
  pageSize = 20
): Promise<PagedNotifications> {
  const { data } = await api.get("/notifications", {
    params: { page, pageSize },
  });
  return data;
}

export async function getUnreadCount(): Promise<UnreadCountDto> {
  const { data } = await api.get("/notifications/unread-count");
  return data;
}

export async function markAsRead(id: number): Promise<void> {
  await api.put(`/notifications/${id}/read`);
}

export async function markAllAsRead(): Promise<{ markedCount: number }> {
  const { data } = await api.put("/notifications/read-all");
  return data;
}

export async function deleteNotification(id: number): Promise<void> {
  await api.delete(`/notifications/${id}`);
}
