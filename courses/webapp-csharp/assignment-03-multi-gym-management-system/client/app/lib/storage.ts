import type { AuthSession } from "./types";

export const SESSION_STORAGE_KEY = "multi-gym-admin-client-session";

export function loadStoredSession(): AuthSession | null {
  const rawValue = sessionStorage.getItem(SESSION_STORAGE_KEY);
  if (!rawValue) {
    return null;
  }

  try {
    const parsedValue = JSON.parse(rawValue) as AuthSession;
    return normalizeSession(parsedValue);
  } catch {
    sessionStorage.removeItem(SESSION_STORAGE_KEY);
    return null;
  }
}

export function saveStoredSession(session: AuthSession): void {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(normalizeSession(session)));
}

export function clearStoredSession(): void {
  sessionStorage.removeItem(SESSION_STORAGE_KEY);
}

function normalizeSession(session: AuthSession): AuthSession {
  return {
    ...session,
    systemRoles: Array.isArray(session.systemRoles) ? session.systemRoles : [],
  };
}
