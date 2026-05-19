import { apiClient, normalizeAuthTokens } from "@/api/http";
import type { AuthResponseDto, LoginInput, RegisterInput } from "@/types/auth";

export async function loginRequest(payload: LoginInput) {
  const response = await apiClient.post<AuthResponseDto>(
    "/Account/Login?expiresInSeconds=900",
    payload,
    {
      skipAuth: true,
      skipAuthRefresh: true,
    },
  );

  return normalizeAuthTokens(response.data);
}

export async function registerRequest(payload: RegisterInput) {
  const response = await apiClient.post<AuthResponseDto>(
    "/Account/Register?expiresInSeconds=900",
    payload,
    {
      skipAuth: true,
      skipAuthRefresh: true,
    },
  );

  return normalizeAuthTokens(response.data);
}
