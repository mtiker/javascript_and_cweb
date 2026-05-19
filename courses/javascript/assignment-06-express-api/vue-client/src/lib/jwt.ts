import type { CurrentUser } from "@/types/auth";

const CLAIMS = {
  userId:
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
  email:
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
  firstName:
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname",
  lastName: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname",
  name: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
} as const;

function decodeBase64UrlSegment(segment: string): string {
  const padded = segment.replace(/-/g, "+").replace(/_/g, "/");
  const normalized = padded.padEnd(Math.ceil(padded.length / 4) * 4, "=");
  return atob(normalized);
}

export function decodeJwtPayload(token: string): Record<string, unknown> {
  const [, payload] = token.split(".");

  if (!payload) {
    throw new Error("Invalid JWT payload");
  }

  return JSON.parse(decodeBase64UrlSegment(payload)) as Record<string, unknown>;
}

export function buildCurrentUser(token: string): CurrentUser {
  const payload = decodeJwtPayload(token);
  const email = String(payload[CLAIMS.email] ?? payload[CLAIMS.name] ?? "");
  const firstName = String(payload[CLAIMS.firstName] ?? "");
  const lastName = String(payload[CLAIMS.lastName] ?? "");
  const displayName = [firstName, lastName].filter(Boolean).join(" ").trim() || email;

  return {
    userId: payload[CLAIMS.userId] ? String(payload[CLAIMS.userId]) : null,
    email,
    firstName,
    lastName,
    displayName,
  };
}
