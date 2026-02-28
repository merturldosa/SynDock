export interface TenantTheme {
  primary: string;
  primaryLight: string;
  secondary: string;
  secondaryLight: string;
  background: string;
}

export interface HeroCta {
  label: string;
  href: string;
  variant?: "primary" | "outline";
}

export interface TenantConfig {
  theme?: TenantTheme;
  heroSubtitle?: string;
  heroTagline?: string;
  heroDescription?: string;
  heroPattern?: string;
  heroCta?: HeroCta[];
  contactPhone?: string;
  contactFax?: string;
  contactEmail?: string;
  companyName?: string;
  companyAddress?: string;
  businessNumber?: string;
  ceoName?: string;
  privacyOfficer?: string;
}

export interface TenantInfo {
  id: number;
  name: string;
  slug: string;
  domain: string | null;
  isActive: boolean;
  configJson: string | null;
}
