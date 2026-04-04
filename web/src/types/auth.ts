export interface UserInfo {
  id: number;
  username: string;
  email: string;
  name: string;
  role: string;
  customFieldsJson?: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  user: UserInfo;
}

export interface LoginResponse {
  requiresTwoFactor: boolean;
  twoFactorToken: string | null;
  auth: AuthTokens | null;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  name: string;
  phone?: string;
}
