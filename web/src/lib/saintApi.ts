import api from "@/lib/api";
import type { SaintSummaryDto, SaintDto, BaptismalNameInfo } from "@/types/saint";
import type { PagedResponse } from "@/types/product";

export async function getSaints(params?: {
  search?: string;
  page?: number;
  pageSize?: number;
}): Promise<PagedResponse<SaintSummaryDto>> {
  const { data } = await api.get<PagedResponse<SaintSummaryDto>>("/saints", { params });
  return data;
}

export async function getSaintById(id: number): Promise<SaintDto> {
  const { data } = await api.get<SaintDto>(`/saints/${id}`);
  return data;
}

export async function getTodaySaints(): Promise<SaintSummaryDto[]> {
  const { data } = await api.get<SaintSummaryDto[]>("/saints/today");
  return data;
}

export async function searchSaints(search: string): Promise<PagedResponse<SaintSummaryDto>> {
  const { data } = await api.get<PagedResponse<SaintSummaryDto>>("/saints", {
    params: { search, pageSize: 10 },
  });
  return data;
}

export async function updateBaptismalName(baptismalName: string): Promise<BaptismalNameInfo> {
  const { data } = await api.put<BaptismalNameInfo>("/auth/baptismal-name", { baptismalName });
  return data;
}
