import api from "./api";
import type { LoginResponse } from "@/types/auth";

const KAKAO_CLIENT_ID = process.env.NEXT_PUBLIC_KAKAO_CLIENT_ID || "";
const GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID || "";

export function getKakaoLoginUrl(): string {
  const redirectUri = `${window.location.origin}/auth/callback/kakao`;
  return `https://kauth.kakao.com/oauth/authorize?client_id=${KAKAO_CLIENT_ID}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code`;
}

export function getGoogleLoginUrl(): string {
  const redirectUri = `${window.location.origin}/auth/callback/google`;
  return `https://accounts.google.com/o/oauth2/v2/auth?client_id=${GOOGLE_CLIENT_ID}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code&scope=openid%20email%20profile`;
}

export async function oauthLogin(
  provider: string,
  code: string,
  redirectUri: string
): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>(`/auth/oauth/${provider}`, {
    code,
    redirectUri,
  });
  return data;
}
