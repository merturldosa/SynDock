import axios from "axios";

const api = axios.create({
  baseURL: "/api",
  headers: { "Content-Type": "application/json" },
});

// Request interceptor: attach JWT token + X-Tenant-Id
api.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("accessToken");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  const tenantSlug = process.env.NEXT_PUBLIC_TENANT_SLUG || "catholia";
  config.headers["X-Tenant-Id"] = tenantSlug;
  return config;
});

// Response interceptor: handle 401
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      const refreshToken = localStorage.getItem("refreshToken");
      if (refreshToken && !error.config._retry) {
        error.config._retry = true;
        try {
          const { data } = await axios.post("/api/auth/refresh", {
            refreshToken,
          });
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
