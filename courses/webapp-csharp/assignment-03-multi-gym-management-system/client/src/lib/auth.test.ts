import { describe, expect, it } from "vitest";
import { resolveApiBaseUrl } from "./auth";

describe("resolveApiBaseUrl", () => {
  it("uses the explicit environment value first", () => {
    expect(resolveApiBaseUrl("https://api.example.test", true, "https://app.example.test")).toBe(
      "https://api.example.test",
    );
  });

  it("uses same origin in production when no environment value is set", () => {
    expect(resolveApiBaseUrl(undefined, true, "https://gym.example.test")).toBe("https://gym.example.test");
  });

  it("uses the HTTPS backend default during local development", () => {
    expect(resolveApiBaseUrl(undefined, false, "http://localhost:5173")).toBe("https://localhost:7245");
  });
});
