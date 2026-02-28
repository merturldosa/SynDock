import api from "./api";

export interface PointBalanceDto {
  balance: number;
}

export interface PointHistoryDto {
  id: number;
  amount: number;
  transactionType: string;
  description: string | null;
  orderId: number | null;
  createdAt: string;
}

export interface PagedPointHistory {
  items: PointHistoryDto[];
  totalCount: number;
}

export async function getPointBalance(): Promise<PointBalanceDto> {
  const { data } = await api.get("/points/balance");
  return data;
}

export async function getPointHistory(
  page = 1,
  pageSize = 20
): Promise<PagedPointHistory> {
  const { data } = await api.get("/points/history", {
    params: { page, pageSize },
  });
  return data;
}
