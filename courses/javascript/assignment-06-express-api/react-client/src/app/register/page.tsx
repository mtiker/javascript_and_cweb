"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { useAuth } from "@/context/AuthContext";
import { FormField } from "@/components/FormField";

interface RegisterFormValues {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

const NAME_RULES = {
  required: "This field is required",
  minLength: { value: 2, message: "Must be at least 2 characters" },
};

const VALIDATION = {
  firstName: NAME_RULES,
  lastName: NAME_RULES,
  email: {
    required: "Email is required",
    pattern: { value: /^\S+@\S+\.\S+$/, message: "Enter a valid email address" },
  },
  password: {
    required: "Password is required",
    minLength: { value: 6, message: "Must be at least 6 characters" },
  },
} satisfies Partial<Record<keyof RegisterFormValues, object>>;

export default function RegisterPage() {
  const router = useRouter();
  const { register: authRegister } = useAuth();
  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>();

  const [serverError, setServerError] = useState<string | null>(null);

  const onSubmit = handleSubmit(async ({ firstName, lastName, email, password }) => {
    setServerError(null);
    try {
      await authRegister(email, password, firstName, lastName);
      router.push("/login?registered=true");
    } catch (error) {
      setServerError(
        error instanceof Error ? error.message : "Registration failed",
      );
    }
  });

  return (
    <section className="col-md-5 col-lg-4 mx-auto mt-4">
      <div className="card tf-auth-card shadow-sm">
        <div className="card-body p-4">
          <h1 className="h3 mb-1 text-center">Create your account</h1>
          <p className="text-center text-muted mb-4">
            Join TaskFlow to start organizing your work.
          </p>

          {serverError && (
            <div className="alert alert-danger" role="alert">
              {serverError}
            </div>
          )}

          <form onSubmit={onSubmit} noValidate>
            <FormField
              id="firstName"
              label="First name"
              autoComplete="given-name"
              error={errors.firstName}
              registration={register("firstName", VALIDATION.firstName)}
            />
            <FormField
              id="lastName"
              label="Last name"
              autoComplete="family-name"
              error={errors.lastName}
              registration={register("lastName", VALIDATION.lastName)}
            />
            <FormField
              id="email"
              label="Email"
              type="email"
              autoComplete="email"
              placeholder="name@example.com"
              error={errors.email}
              registration={register("email", VALIDATION.email)}
            />
            <FormField
              id="password"
              label="Password"
              type="password"
              autoComplete="new-password"
              error={errors.password}
              registration={register("password", VALIDATION.password)}
            />
            <FormField
              id="confirmPassword"
              label="Confirm password"
              type="password"
              autoComplete="new-password"
              error={errors.confirmPassword}
              registration={register("confirmPassword", {
                required: "Please confirm your password",
                validate: (value) =>
                  value === getValues("password") || "Passwords do not match",
              })}
            />

            <button
              type="submit"
              className="btn btn-primary w-100"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Creating account…" : "Register"}
            </button>
          </form>

          <div className="text-center mt-3">
            <Link href="/login">Already have an account? Login</Link>
          </div>
        </div>
      </div>
    </section>
  );
}
