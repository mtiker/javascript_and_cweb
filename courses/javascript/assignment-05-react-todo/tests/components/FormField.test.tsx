import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import type { UseFormRegisterReturn } from "react-hook-form";
import { FormField } from "@/components/FormField";

function makeRegistration(
  overrides: Partial<UseFormRegisterReturn> = {},
): UseFormRegisterReturn {
  return {
    name: overrides.name ?? "email",
    onChange: overrides.onChange ?? vi.fn().mockResolvedValue(undefined),
    onBlur: overrides.onBlur ?? vi.fn().mockResolvedValue(undefined),
    ref: overrides.ref ?? vi.fn(),
    ...overrides,
  } as UseFormRegisterReturn;
}

describe("FormField", () => {
  it("renders a labeled input wired to the given id", () => {
    render(
      <FormField
        id="email"
        label="Email"
        type="email"
        placeholder="name@example.com"
        registration={makeRegistration({ name: "email" })}
      />,
    );

    const input = screen.getByLabelText("Email") as HTMLInputElement;
    expect(input).toBeInTheDocument();
    expect(input.id).toBe("email");
    expect(input.type).toBe("email");
    expect(input.placeholder).toBe("name@example.com");
    expect(input.getAttribute("aria-invalid")).toBe("false");
    expect(input).not.toHaveClass("is-invalid");
  });

  it("renders the validation message and marks the input invalid when error is given", () => {
    render(
      <FormField
        id="password"
        label="Password"
        type="password"
        error={{ type: "required", message: "Password is required" }}
        registration={makeRegistration({ name: "password" })}
      />,
    );

    const input = screen.getByLabelText("Password");
    expect(input).toHaveClass("is-invalid");
    expect(input.getAttribute("aria-invalid")).toBe("true");
    expect(screen.getByText("Password is required")).toHaveClass(
      "invalid-feedback",
    );
  });

  it("defaults to type=text when type is omitted", () => {
    render(
      <FormField
        id="firstName"
        label="First name"
        registration={makeRegistration({ name: "firstName" })}
      />,
    );

    expect((screen.getByLabelText("First name") as HTMLInputElement).type).toBe(
      "text",
    );
  });
});
