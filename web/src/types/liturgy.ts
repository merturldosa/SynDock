import type { SaintSummaryDto } from "./saint";

export interface LiturgicalSeasonDto {
  seasonName: string;
  startDate: string;
  endDate: string;
  liturgicalColor: string;
}

export interface LiturgyTodayDto {
  currentSeason: LiturgicalSeasonDto;
  todaySaints: SaintSummaryDto[];
}
