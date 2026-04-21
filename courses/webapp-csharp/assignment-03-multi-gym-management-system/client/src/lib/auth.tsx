import { createContext, type PropsWithChildren, useContext, useRef, useState } from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { AppShell } from "../components/AppShell";
import { ApiClient } from "./apiClient";
import { clearStoredSession, loadStoredSession, saveStoredSession } from "./storage";
import type { AuthSession, LoginRequest } from "./types";

interface AuthContextValue {
  api: ApiClient;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  session: AuthSession | null;
}

interface AuthProviderProps extends PropsWithChildren {
  apiBaseUrl?: string;
  initialSession?: AuthSession | null;
}

export function resolveApiBaseUrl(
  explicitBaseUrl: string | undefined = import.meta.env.VITE_API_BASE_URL,
  isProduction = import.meta.env.PROD,
  origin = window.location.origin,
) {
  return explicitBaseUrl ?? (isProduction ? origin : "https://localhost:7245");
}

const DEFAULT_API_BASE_URL = resolveApiBaseUrl();
const CLIENT_ROLES = new Set(["GymAdmin", "GymOwner", "Member", "Trainer", "Caretaker"]);
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ apiBaseUrl = DEFAULT_API_BASE_URL, children, initialSession }: AuthProviderProps) {
  const [session, setSessionState] = useState<AuthSession | null>(() => initialSession ?? loadStoredSession());
  const sessionRef = useRef<AuthSession | null>(session);
  const clientRef = useRef<ApiClient | null>(null);

  const persistSession = (nextSession: AuthSession | null) => {
    sessionRef.current = nextSession;
    setSessionState(nextSession);

    if (nextSession) {
      saveStoredSession(nextSession);
      return;
    }

    clearStoredSession();
  };

  if (!clientRef.current) {
    clientRef.current = new ApiClient({
      baseUrl: apiBaseUrl,
      getSession: () => sessionRef.current,
      setSession: (nextSession) => persistSession(nextSession),
      clearSession: () => persistSession(null),
    });
  }

  const login = async (request: LoginRequest) => {
    const nextSession = await clientRef.current!.login(request);

    if (!nextSession.activeGymCode || !nextSession.activeRole) {
      persistSession(null);
      throw new Error("The backend did not return an active gym context.");
    }

    if (!CLIENT_ROLES.has(nextSession.activeRole)) {
      persistSession(null);
      throw new Error("This client is limited to gym admin, owner, member, trainer, and caretaker accounts in this phase.");
    }

    persistSession(nextSession);
  };

  const logout = async () => {
    try {
      await clientRef.current!.logout();
    } finally {
      persistSession(null);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        api: clientRef.current,
        isAuthenticated: Boolean(session?.jwt && session.activeGymCode),
        login,
        logout,
        session,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function ProtectedLayout() {
  const auth = useAuth();
  const location = useLocation();

  if (!auth.isAuthenticated) {
    return <Navigate replace state={{ from: location }} to="/login" />;
  }

  return (
    <AppShell>
      <Outlet />
    </AppShell>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}
