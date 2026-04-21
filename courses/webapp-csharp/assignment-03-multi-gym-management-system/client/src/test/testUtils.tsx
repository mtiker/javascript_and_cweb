import type { PropsWithChildren, ReactElement } from "react";
import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AuthProvider } from "../lib/auth";
import type { AuthSession } from "../lib/types";

export const defaultSession: AuthSession = {
  jwt: "jwt-token",
  refreshToken: "refresh-token",
  expiresInSeconds: 3600,
  activeGymCode: "peak-forge",
  activeGymId: "gym-id",
  activeRole: "GymAdmin",
  systemRoles: [],
};

export function renderWithAuth(
  ui: ReactElement,
  { route = "/", session = defaultSession }: { route?: string; session?: AuthSession | null } = {},
) {
  return render(
    <AuthProvider initialSession={session}>
      <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
    </AuthProvider>,
  );
}

export function AuthRouteWrapper({
  children,
  route = "/",
  session = defaultSession,
}: PropsWithChildren<{ route?: string; session?: AuthSession | null }>) {
  return (
    <AuthProvider initialSession={session}>
      <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
    </AuthProvider>
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
