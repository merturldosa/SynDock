import api from "@/lib/api";
import type { LiturgyTodayDto, LiturgicalSeasonDto } from "@/types/liturgy";

export async function getTodayLiturgy(): Promise<LiturgyTodayDto> {
  const { data } = await api.get<LiturgyTodayDto>("/liturgy/today");
  return data;
}

export async function getLiturgicalSeasons(year?: number): Promise<LiturgicalSeasonDto[]> {
  const { data } = await api.get<LiturgicalSeasonDto[]>("/liturgy/seasons", {
    params: year ? { year } : undefined,
  });
  return data;
}
