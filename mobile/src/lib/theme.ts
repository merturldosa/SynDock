export interface Theme {
  primary: string;
  primaryLight: string;
  secondary: string;
  secondaryLight: string;
  background: string;
  surface: string;
  text: string;
  textSecondary: string;
  border: string;
  error: string;
  success: string;
  warning: string;
}

export const defaultTheme: Theme = {
  primary: "#D4A574",
  primaryLight: "#E8CDB0",
  secondary: "#2C1810",
  secondaryLight: "#4A3728",
  background: "#FAFAFA",
  surface: "#FFFFFF",
  text: "#1A1A1A",
  textSecondary: "#666666",
  border: "#E5E5E5",
  error: "#DC2626",
  success: "#16A34A",
  warning: "#F59E0B",
};

export const formatPrice = (price: number) =>
  price.toLocaleString("ko-KR") + "원";
