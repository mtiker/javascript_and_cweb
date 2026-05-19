import type { IJWTResponse } from "@/domain";

export interface AuthState {
  jwt: string | null;
  refreshToken: string | null;
  userEmail: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export type AuthAction =
  | { type: "AUTH_INIT"; payload: IJWTResponse & { email: string } }
  | { type: "LOGIN_SUCCESS"; payload: IJWTResponse & { email: string } }
  | { type: "LOGOUT" }
  | { type: "TOKEN_REFRESHED"; payload: Pick<IJWTResponse, "token" | "refreshToken"> }
  | { type: "AUTH_ERROR"; payload: string }
  | { type: "SET_LOADING"; payload: boolean };

export const initialAuthState: AuthState = {
  jwt: null,
  refreshToken: null,
  userEmail: null,
  isAuthenticated: false,
  isLoading: true,
  error: null,
};

export function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case "AUTH_INIT":
    case "LOGIN_SUCCESS":
      return {
        ...state,
        jwt: action.payload.token ?? null,
        refreshToken: action.payload.refreshToken ?? null,
        userEmail: action.payload.email,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      };

    case "LOGOUT":
      return { ...initialAuthState, isLoading: false };

    case "TOKEN_REFRESHED":
      return {
        ...state,
        jwt: action.payload.token ?? null,
        refreshToken: action.payload.refreshToken ?? null,
      };

    case "AUTH_ERROR":
      return { ...state, error: action.payload, isLoading: false };

    case "SET_LOADING":
      return { ...state, isLoading: action.payload };

    default:
      return state;
  }
}
