import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { tokenStore } from "./tokenStore";
import type { IJWTResponse } from "@/domain";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://taltech.akaver.com";

const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { "Content-Type": "application/json" },
});

type TokenRefreshedCallback = (token: string, refresh: string) => void;
let onTokenRefreshed: TokenRefreshedCallback | null = null;

export function setOnTokenRefreshed(cb: TokenRefreshedCallback | null) {
  onTokenRefreshed = cb;
}

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenStore.getToken();
  if (token) {
    config.headers.set?.("Authorization", `Bearer ${token}`);
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as
      | (InternalAxiosRequestConfig & { _retry?: boolean })
      | undefined;

    if (!original || error.response?.status !== 401 || original._retry) {
      return Promise.reject(error);
    }

    original._retry = true;
    const token = tokenStore.getToken();
    const refreshToken = tokenStore.getRefreshToken();

    if (!refreshToken) {
      tokenStore.clearTokens();
      if (typeof window !== "undefined") window.location.href = "/login";
      return Promise.reject(error);
    }

    try {
      const { data } = await axios.post<IJWTResponse>(
        `${BASE_URL}/api/v1/Account/RefreshToken`,
        { jwt: token, refreshToken },
        { headers: { "Content-Type": "application/json" } },
      );

      if (!data.token || !data.refreshToken) {
        throw new Error("Refresh response missing token values");
      }

      tokenStore.setTokens(data.token, data.refreshToken, data.firstName, data.lastName);
      onTokenRefreshed?.(data.token, data.refreshToken);
      original.headers.set?.("Authorization", `Bearer ${data.token}`);
      return apiClient(original);
    } catch (refreshErr) {
      tokenStore.clearTokens();
      if (typeof window !== "undefined") window.location.href = "/login";
      return Promise.reject(refreshErr);
    }
  },
);

export default apiClient;
