import axios, { type AxiosRequestConfig, type InternalAxiosRequestConfig } from "axios";
import { env } from "@/config/env";
import { createSingleFlight } from "@/lib/single-flight";
import { tokenStorage } from "@/lib/token-storage";
import type { AuthResponseDto, AuthTokens } from "@/types/auth";

declare module "axios" {
  export interface AxiosRequestConfig {
    skipAuth?: boolean;
    skipAuthRefresh?: boolean;
  }

  export interface InternalAxiosRequestConfig {
    skipAuth?: boolean;
    skipAuthRefresh?: boolean;
    _retry?: boolean;
  }
}

export function normalizeAuthTokens(response: AuthResponseDto): AuthTokens {
  if (!response.token || !response.refreshToken) {
    throw new Error("Authentication response did not include both tokens.");
  }

  return {
    accessToken: response.token,
    refreshToken: response.refreshToken,
  };
}

function isAuthEndpoint(url?: string) {
  return Boolean(url && /\/Account\/(Login|Register|RefreshToken)/i.test(url));
}

export function createApiClient(baseURL = env.apiBaseUrl) {
  const client = axios.create({
    baseURL,
    headers: {
      "Content-Type": "application/json",
    },
  });

  const singleFlight = createSingleFlight<AuthTokens | null>();

  const refreshAccessToken = async () => {
    const tokens = tokenStorage.get();

    if (!tokens) {
      tokenStorage.clear();
      return null;
    }

    try {
      const response = await client.post<AuthResponseDto>(
        "/Account/RefreshToken?expiresInSeconds=900",
        {
          jwt: tokens.accessToken,
          refreshToken: tokens.refreshToken,
        },
        {
          skipAuth: true,
          skipAuthRefresh: true,
        },
      );

      const nextTokens = normalizeAuthTokens(response.data);
      tokenStorage.set(nextTokens);
      return nextTokens;
    } catch {
      tokenStorage.clear();
      return null;
    }
  };

  client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    if (!config.skipAuth) {
      const tokens = tokenStorage.get();

      if (tokens?.accessToken) {
        config.headers.set("Authorization", `Bearer ${tokens.accessToken}`);
      }
    }

    return config;
  });

  client.interceptors.response.use(
    (response) => response,
    async (error) => {
      const config = error.config as InternalAxiosRequestConfig | undefined;

      if (
        !config ||
        config._retry ||
        config.skipAuthRefresh ||
        error.response?.status !== 401 ||
        isAuthEndpoint(config.url)
      ) {
        throw error;
      }

      const tokens = tokenStorage.get();

      if (!tokens?.refreshToken) {
        tokenStorage.clear();
        throw error;
      }

      config._retry = true;

      const refreshed = await singleFlight.run(refreshAccessToken);

      if (!refreshed) {
        throw error;
      }

      config.headers.set("Authorization", `Bearer ${refreshed.accessToken}`);

      return client.request(config as AxiosRequestConfig);
    },
  );

  return client;
}

export const apiClient = createApiClient();
