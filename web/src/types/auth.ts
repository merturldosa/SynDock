export interface UserInfo {
  id: number;
  username: string;
  email: string;
  name: string;
  role: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserInfo;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  name: string;
  phone?: string;
}
