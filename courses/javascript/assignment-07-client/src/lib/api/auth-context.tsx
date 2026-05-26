import {
  createContext,
  useContext,
  useMemo,
  useRef,
  useState,
  type PropsWithChildren,
} from "react";
import { ApiClient, resolveApiBaseUrl } from "./client";
import { clearStoredSession, loadStoredSession, saveStoredSession } from "./storage";
import type { AuthSession, LoginRequest, RegisterRequest } from "./types";

interface AuthContextValue {
  api: ApiClient;
  session: AuthSession | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  activeGym: string | null;
  login: (req: LoginRequest) => Promise<AuthSession>;
  register: (req: RegisterRequest) => Promise<AuthSession>;
  logout: () => Promise<void>;
  switchGym: (gymCode: string) => Promise<void>;
  mockLogin: (role: "admin" | "member") => void;
}

const ADMIN_ROLES = new Set(["GymAdmin", "GymOwner", "SystemAdmin"]);
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSessionState] = useState<AuthSession | null>(() => loadStoredSession());
  const sessionRef = useRef<AuthSession | null>(session);
  const clientRef = useRef<ApiClient | null>(null);

  const persist = (next: AuthSession | null) => {
    sessionRef.current = next;
    setSessionState(next);
    if (next) saveStoredSession(next);
    else clearStoredSession();
  };

  if (!clientRef.current) {
    clientRef.current = new ApiClient({
      baseUrl: resolveApiBaseUrl(),
      getSession: () => sessionRef.current,
      setSession: persist,
      clearSession: () => persist(null),
    });
  }

  const value = useMemo<AuthContextValue>(() => {
    const api = clientRef.current!;
    const isAuthenticated = Boolean(session?.jwt);
    const isAdmin =
      Boolean(session?.activeRole && ADMIN_ROLES.has(session.activeRole)) ||
      Boolean(session?.systemRoles?.some((r) => ADMIN_ROLES.has(r)));

    return {
      api,
      session,
      isAuthenticated,
      isAdmin,
      activeGym: session?.activeGymCode ?? null,
      login: async (req) => {
        const next = await api.login(req);
        persist(next);
        return next;
      },
      register: async (req) => {
        const next = await api.register(req);
        persist(next);
        return next;
      },
      logout: async () => {
        try {
          await api.logout();
        } catch {
          /* ignore network errors on logout */
        } finally {
          persist(null);
        }
      },
      switchGym: async (gymCode) => {
        const next = await api.switchGym(gymCode);
        persist(next);
      },
      mockLogin: (role) => {
        const isAdmin = role === "admin";
        persist({
          jwt: "mock.jwt.preview",
          refreshToken: "mock.refresh.preview",
          expiresInSeconds: 3600,
          activeGymId: "mock-gym",
          activeGymCode: "DEMO",
          activeRole: isAdmin ? "GymAdmin" : "Member",
          systemRoles: isAdmin ? ["GymAdmin"] : ["Member"],
          availableTenants: [
            {
              gymId: "mock-gym",
              gymCode: "DEMO",
              gymName: "Demo Gym",
              roles: [isAdmin ? "GymAdmin" : "Member"],
            },
          ],
        });
      },
    };
  }, [session]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
