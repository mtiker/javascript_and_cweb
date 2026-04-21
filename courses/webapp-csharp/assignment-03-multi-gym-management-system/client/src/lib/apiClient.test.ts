import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClient } from "./apiClient";
import type { AuthSession } from "./types";
import { jsonResponse } from "../test/testUtils";

describe("ApiClient", () => {
  let session: AuthSession | null;
  let client: ApiClient;
  let setSession: ReturnType<typeof vi.fn>;
  let clearSession: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    session = {
      jwt: "expired-jwt",
      refreshToken: "refresh-1",
      expiresInSeconds: 3600,
      activeGymCode: "peak-forge",
      activeRole: "GymAdmin",
      systemRoles: [],
    };
    setSession = vi.fn((nextSession: AuthSession) => {
      session = nextSession;
    });
    clearSession = vi.fn(() => {
      session = null;
    });
    client = new ApiClient({
      baseUrl: "https://localhost:7245",
      getSession: () => session,
      setSession,
      clearSession,
    });
    vi.stubGlobal("fetch", vi.fn());
  });

  it("retries once after refreshing the session on 401", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(
        jsonResponse({
          jwt: "fresh-jwt",
          refreshToken: "refresh-2",
          expiresInSeconds: 3600,
          activeGymCode: "peak-forge",
          activeRole: "GymAdmin",
          systemRoles: [],
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse([
          {
            id: "member-1",
            memberCode: "MEM-001",
            fullName: "Liis Lill",
            status: 0,
          },
        ]),
      );

    const members = await client.getMembers("peak-forge");

    expect(members).toHaveLength(1);
    expect(setSession).toHaveBeenCalled();
    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(fetchMock.mock.calls[1]?.[0]).toContain("/api/v1/account/renew-refresh-token");
  });

  it("clears the session when refresh fails", async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(
        jsonResponse(
          {
            title: "Forbidden",
            detail: "Refresh token is invalid or expired.",
          },
          { status: 403 },
        ),
      );

    await expect(client.getMembers("peak-forge")).rejects.toThrow("Session expired. Please sign in again.");
    expect(clearSession).toHaveBeenCalled();
  });
});
