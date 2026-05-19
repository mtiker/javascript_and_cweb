import { describe, expect, it } from "vitest";
import axios, { AxiosError, AxiosHeaders } from "axios";
import { getErrorMessage } from "@/utils/errorUtils";

function makeAxiosError(data: unknown, status = 400): AxiosError {
  // axios.AxiosError requires a config/headers to be considered "isAxiosError"
  const err = new AxiosError(
    "Request failed",
    "ERR_BAD_REQUEST",
    { headers: new AxiosHeaders(), url: "/x" } as any,
    null,
    {
      data,
      status,
      statusText: "",
      headers: {},
      config: { headers: new AxiosHeaders() } as any,
    },
  );
  return err;
}

describe("getErrorMessage", () => {
  it("uses the first field-level error when present", () => {
    const err = makeAxiosError({
      errors: { Email: ["Email is invalid"], Password: ["too short"] },
    });
    expect(getErrorMessage(err, "fallback")).toBe("Email is invalid");
  });

  it("falls back to .messages[0] for aggregated API errors", () => {
    const err = makeAxiosError({ messages: ["Bad credentials"] });
    expect(getErrorMessage(err, "fallback")).toBe("Bad credentials");
  });

  it("uses ProblemDetails .detail when no field/messages exist", () => {
    const err = makeAxiosError({
      title: "One or more validation errors",
      detail: "Token has expired",
    });
    expect(getErrorMessage(err, "fallback")).toBe("Token has expired");
  });

  it("uses ProblemDetails .title as a final API fallback", () => {
    const err = makeAxiosError({ title: "Bad request" });
    expect(getErrorMessage(err, "fallback")).toBe("Bad request");
  });

  it("returns the fallback for non-Error non-axios values", () => {
    expect(getErrorMessage("oops", "fallback")).toBe("fallback");
    expect(getErrorMessage(42, "fallback")).toBe("fallback");
  });

  it("returns error.message for plain Error instances", () => {
    expect(getErrorMessage(new Error("plain failure"), "fallback")).toBe(
      "plain failure",
    );
  });

  it("isAxiosError sanity check", () => {
    const err = makeAxiosError({ messages: ["x"] });
    expect(axios.isAxiosError(err)).toBe(true);
  });
});
