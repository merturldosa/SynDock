import axios from "axios";

const api = axios.create({
  baseURL: "/api",
  headers: { "Content-Type": "application/json" },
  timeout: 15000,
});

// Singleton refresh promise to prevent concurrent token refresh
let refreshPromise: Promise<any> | null = null;

// Locale-to-Accept-Language mapping
const langMap: Record<string, string> = {
  ko: "ko-KR,ko;q=0.9",
  en: "en-US,en;q=0.9",
  ja: "ja-JP,ja;q=0.9",
  "zh-CN": "zh-CN,zh;q=0.9",
  vi: "vi-VN,vi;q=0.9",
};

// Request interceptor: attach JWT token + X-Tenant-Id + Accept-Language
api.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("accessToken");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    const locale = localStorage.getItem("locale") || "ko";
    config.headers["Accept-Language"] = langMap[locale] || langMap.ko;
  }
  const tenantSlug = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";
  config.headers["X-Tenant-Id"] = tenantSlug;
  return config;
});

// Response interceptor: handle 401 with concurrent refresh protection
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      const refreshToken = localStorage.getItem("refreshToken");
      if (refreshToken && !error.config._retry) {
        error.config._retry = true;
        try {
          // Reuse existing refresh request if one is in-flight
          if (!refreshPromise) {
            refreshPromise = axios
              .post("/api/auth/refresh", { refreshToken })
              .finally(() => {
                refreshPromise = null;
              });
          }
          const { data } = await refreshPromise;
          localStorage.setItem("accessToken", data.accessToken);
          localStorage.setItem("refreshToken", data.refreshToken);
          error.config.headers.Authorization = `Bearer ${data.accessToken}`;
          return api(error.config);
        } catch {
          localStorage.removeItem("accessToken");
          localStorage.removeItem("refreshToken");
          window.location.href = "/login";
        }
      }
    }
    return Promise.reject(error);
  }
);

export default api;
