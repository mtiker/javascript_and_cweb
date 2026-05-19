"use client";

import { useEffect, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";

interface ProtectedRouteProps {
  children: ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const router = useRouter();
  const { state } = useAuth();

  useEffect(() => {
    if (!state.isLoading && !state.isAuthenticated) {
      router.replace("/login");
    }
  }, [router, state.isAuthenticated, state.isLoading]);

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

  if (!state.isAuthenticated) return null;

  return <>{children}</>;
}
