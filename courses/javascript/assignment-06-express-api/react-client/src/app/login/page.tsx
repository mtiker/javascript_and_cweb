"use client";

import { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { useAuth } from "@/context/AuthContext";
import { FormField } from "@/components/FormField";

interface LoginFormValues {
  email: string;
  password: string;
}

const VALIDATION = {
  email: {
    required: "Email is required",
    pattern: { value: /^\S+@\S+\.\S+$/, message: "Enter a valid email address" },
  },
  password: {
    required: "Password is required",
    minLength: { value: 6, message: "Password must be at least 6 characters" },
  },
};

export default function LoginPage() {
  return (
    <Suspense
      fallback={
        <div
          className="d-flex justify-content-center py-5"
          role="status"
          aria-live="polite"
        >
          <div className="spinner-border text-primary" aria-label="Loading" />
        </div>
      }
    >
      <LoginForm />
    </Suspense>
  );
}

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { login, state } = useAuth();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>();

  const [serverError, setServerError] = useState<string | null>(null);
  const registered = searchParams.get("registered") === "true";

  useEffect(() => {
    if (!state.isLoading && state.isAuthenticated) {
      router.replace("/todos");
    }
  }, [router, state.isAuthenticated, state.isLoading]);

  const onSubmit = handleSubmit(async ({ email, password }) => {
    setServerError(null);
    try {
      await login(email, password);
      router.push("/todos");
    } catch (error) {
      setServerError(error instanceof Error ? error.message : "Login failed");
    }
  });

  if (state.isLoading) {
    return (
      <div
        className="d-flex justify-content-center py-5"
        role="status"
        aria-live="polite"
      >
        <div className="spinner-border text-primary" aria-label="Loading" />
      </div>
    );
  }

  if (state.isAuthenticated) return null;

  return (
    <section className="col-md-5 col-lg-4 mx-auto mt-4">
      <div className="card tf-auth-card shadow-sm">
        <div className="card-body p-4">
          <h1 className="h3 mb-1 text-center">Welcome back</h1>
          <p className="text-center text-muted mb-4">
            Sign in to manage your TaskFlow board.
          </p>

          {registered && (
            <div className="alert alert-success" role="alert">
              Registration successful — please log in.
            </div>
          )}

          {serverError && (
            <div className="alert alert-danger" role="alert">
              {serverError}
            </div>
          )}

          <form onSubmit={onSubmit} noValidate>
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
              autoComplete="current-password"
              error={errors.password}
              registration={register("password", VALIDATION.password)}
            />

            <button
              type="submit"
              className="btn btn-primary w-100"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Signing in…" : "Login"}
            </button>
          </form>

          <div className="text-center mt-3">
            <Link href="/register">Need an account? Register</Link>
          </div>
        </div>
      </div>
    </section>
  );
}
