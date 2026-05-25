"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useReducer,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { authReducer, initialAuthState, type AuthState } from "@/reducers/authReducer";
import { tokenStore } from "@/services/tokenStore";
import { setOnAuthFailure, setOnTokenRefreshed } from "@/services/apiClient";
import { AccountService } from "@/services/AccountService";

const LOCAL_STORAGE_JWT_KEY = "auth_jwt";
const LOCAL_STORAGE_REFRESH_TOKEN_KEY = "auth_refreshToken";
const LOCAL_STORAGE_USER_EMAIL_KEY = "auth_userEmail";

export interface AuthContextType {
  state: AuthState;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  register: (
    email: string,
    password: string,
    firstName: string,
    lastName: string,
  ) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [state, dispatch] = useReducer(authReducer, initialAuthState);
  const router = useRouter();

  // On mount: rehydrate auth state from localStorage.
  useEffect(() => {
    const jwt = localStorage.getItem(LOCAL_STORAGE_JWT_KEY);
    const refreshToken = localStorage.getItem(LOCAL_STORAGE_REFRESH_TOKEN_KEY);
    const userEmail = localStorage.getItem(LOCAL_STORAGE_USER_EMAIL_KEY);

    if (jwt && refreshToken && userEmail) {
      tokenStore.setTokens(jwt, refreshToken);
      dispatch({
        type: "AUTH_INIT",
        payload: { token: jwt, refreshToken, email: userEmail },
      });
    } else {
      dispatch({ type: "SET_LOADING", payload: false });
    }
  }, []);

  // Mirror state.jwt / refresh into tokenStore + localStorage.
  // Note: tokenStore is also written directly by the apiClient refresh path
  // before this effect re-fires via TOKEN_REFRESHED — both writes target the
  // same value so the duplication is benign.
  useEffect(() => {
    if (state.jwt && state.refreshToken && state.userEmail) {
      localStorage.setItem(LOCAL_STORAGE_JWT_KEY, state.jwt);
      localStorage.setItem(LOCAL_STORAGE_REFRESH_TOKEN_KEY, state.refreshToken);
      localStorage.setItem(LOCAL_STORAGE_USER_EMAIL_KEY, state.userEmail);
      tokenStore.setTokens(state.jwt, state.refreshToken);
    } else {
      localStorage.removeItem(LOCAL_STORAGE_JWT_KEY);
      localStorage.removeItem(LOCAL_STORAGE_REFRESH_TOKEN_KEY);
      localStorage.removeItem(LOCAL_STORAGE_USER_EMAIL_KEY);
      tokenStore.clearTokens();
    }
  }, [state.jwt, state.refreshToken, state.userEmail]);

  // Subscribe to silent-refresh updates from apiClient so React state stays
  // in sync with the live tokens.
  useEffect(() => {
    setOnTokenRefreshed((jwt, refresh) => {
      dispatch({
        type: "TOKEN_REFRESHED",
        payload: { token: jwt, refreshToken: refresh },
      });
    });
    return () => setOnTokenRefreshed(null);
  }, []);

  // Subscribe to refresh-failure events from apiClient: clear React state and
  // route to /login without a full page reload (window.location.href would
  // drop all React state and is unsafe under SSR).
  useEffect(() => {
    setOnAuthFailure(() => {
      dispatch({ type: "LOGOUT" });
      router.replace("/login");
    });
    return () => setOnAuthFailure(null);
  }, [router]);

  // Cross-tab logout: when another tab clears the JWT in localStorage, mirror
  // that here so this tab also logs out. `storage` events only fire in OTHER
  // tabs by spec, so there is no echo loop with our own writes.
  useEffect(() => {
    const onStorage = (event: StorageEvent) => {
      if (event.key === LOCAL_STORAGE_JWT_KEY && event.newValue === null) {
        dispatch({ type: "LOGOUT" });
        tokenStore.clearTokens();
      }
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    dispatch({ type: "SET_LOADING", payload: true });
    try {
      const response = await AccountService.login({ email, password });
      if (!response.token || !response.refreshToken) {
        throw new Error("Login response missing tokens");
      }
      dispatch({
        type: "LOGIN_SUCCESS",
        payload: {
          token: response.token,
          refreshToken: response.refreshToken,
          email,
        },
      });
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : "Login failed";
      dispatch({ type: "AUTH_ERROR", payload: errorMsg });
      throw err;
    }
  }, []);

  const logout = useCallback(() => {
    dispatch({ type: "LOGOUT" });
    tokenStore.clearTokens();
  }, []);

  const register = useCallback(
    async (email: string, password: string, firstName: string, lastName: string) => {
      dispatch({ type: "SET_LOADING", payload: true });
      try {
        await AccountService.register({ email, password, firstName, lastName });
        // Do NOT auto-login; surface "registered=true" on the login page.
        dispatch({ type: "SET_LOADING", payload: false });
      } catch (err) {
        const errorMsg =
          err instanceof Error ? err.message : "Registration failed";
        dispatch({ type: "AUTH_ERROR", payload: errorMsg });
        throw err;
      }
    },
    [],
  );

  const value: AuthContextType = { state, login, logout, register };
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
