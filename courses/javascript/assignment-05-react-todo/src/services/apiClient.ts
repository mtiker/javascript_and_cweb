import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { tokenStore } from "./tokenStore";
import { BASE_URL } from "./config";
import type { IJWTResponse } from "@/domain";

const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { "Content-Type": "application/json" },
});

type TokenRefreshedCallback = (token: string, refresh: string) => void;
type AuthFailureCallback = () => void;
let onTokenRefreshed: TokenRefreshedCallback | null = null;
let onAuthFailure: AuthFailureCallback | null = null;

// Shared promise for an in-flight refresh. When several requests 401
// concurrently, only the first one POSTs /RefreshToken; the rest await this
// promise and then retry with the rotated Bearer.
let refreshInFlight: Promise<string> | null = null;

export function setOnTokenRefreshed(cb: TokenRefreshedCallback | null) {
  onTokenRefreshed = cb;
}

export function setOnAuthFailure(cb: AuthFailureCallback | null) {
  onAuthFailure = cb;
}

async function performRefresh(): Promise<string> {
  const token = tokenStore.getToken();
  const refreshToken = tokenStore.getRefreshToken();
  if (!refreshToken) throw new Error("No refresh token available");

  const { data } = await axios.post<IJWTResponse>(
    `${BASE_URL}/api/v1/Account/RefreshToken`,
    { jwt: token, refreshToken },
    { headers: { "Content-Type": "application/json" } },
  );

  if (!data.token || !data.refreshToken) {
    throw new Error("Refresh response missing token values");
  }

  tokenStore.setTokens(data.token, data.refreshToken);
  onTokenRefreshed?.(data.token, data.refreshToken);
  return data.token;
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

    if (!tokenStore.getRefreshToken()) {
      tokenStore.clearTokens();
      onAuthFailure?.();
      return Promise.reject(error);
    }

    original._retry = true;

    try {
      if (!refreshInFlight) {
        refreshInFlight = performRefresh().finally(() => {
          refreshInFlight = null;
        });
      }
      const newToken = await refreshInFlight;
      original.headers.set?.("Authorization", `Bearer ${newToken}`);
      return apiClient(original);
    } catch (refreshErr) {
      tokenStore.clearTokens();
      onAuthFailure?.();
      return Promise.reject(refreshErr);
    }
  },
);

export default apiClient;
