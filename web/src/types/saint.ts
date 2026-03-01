export interface SaintDto {
  id: number;
  koreanName: string;
  latinName?: string;
  englishName?: string;
  description?: string;
  feastDay?: string;
  patronage?: string;
  isActive: boolean;
}

export interface SaintSummaryDto {
  id: number;
  koreanName: string;
  latinName?: string;
  feastDay?: string;
  patronage?: string;
}

export interface BaptismalNameInfo {
  baptismalName: string | null;
  patronSaintId: number | null;
  patronSaint: SaintSummaryDto | null;
}
