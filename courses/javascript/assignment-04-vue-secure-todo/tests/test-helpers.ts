export function makeJwt(overrides: Record<string, unknown> = {}) {
  const header = {
    alg: "HS256",
    typ: "JWT",
  };

  const payload = {
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier":
      "019d97f1-21af-70a1-94e9-44149949b92d",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":
      "student@example.com",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname": "Student",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname": "Example",
    ...overrides,
  };

  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");

  return `${encode(header)}.${encode(payload)}.signature`;
}

export const baseCategory = {
  id: "11111111-1111-4111-8111-111111111111",
  name: "Work",
  sortOrder: 10,
  syncAt: "2026-04-16T10:00:00.000Z",
  tag: "work",
};

export const basePriority = {
  id: "22222222-2222-4222-8222-222222222222",
  name: "High",
  sortOrder: 10,
  syncAt: "2026-04-16T10:00:00.000Z",
  tag: null,
};
