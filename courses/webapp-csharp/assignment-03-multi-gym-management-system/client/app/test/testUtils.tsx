import type { PropsWithChildren, ReactElement } from "react";
import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AuthProvider } from "../lib/auth";
import { LanguageProvider } from "../lib/language";
import type { AuthSession } from "../lib/types";

export const defaultSession: AuthSession = {
  jwt: "jwt-token",
  refreshToken: "refresh-token",
  expiresInSeconds: 3600,
  activeGymCode: "peak-forge",
  activeGymId: "gym-id",
  activeRole: "GymAdmin",
  systemRoles: [],
  availableTenants: [
    {
      gymId: "gym-id",
      gymCode: "peak-forge",
      gymName: "Peak Forge",
      roles: ["GymAdmin"],
    },
  ],
};

export function renderWithAuth(
  ui: ReactElement,
  { route = "/", session = defaultSession }: { route?: string; session?: AuthSession | null } = {},
) {
  return render(
    <LanguageProvider>
      <AuthProvider initialSession={session}>
        <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
      </AuthProvider>
    </LanguageProvider>,
  );
}

export function AuthRouteWrapper({
  children,
  route = "/",
  session = defaultSession,
}: PropsWithChildren<{ route?: string; session?: AuthSession | null }>) {
  return (
    <LanguageProvider>
      <AuthProvider initialSession={session}>
        <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
      </AuthProvider>
    </LanguageProvider>
  );
}

export function jsonResponse(body: unknown, init?: ResponseInit) {
  return new Response(JSON.stringify(body), {
    headers: {
      "Content-Type": "application/json",
    },
    status: init?.status ?? 200,
  });
}
