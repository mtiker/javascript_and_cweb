"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";

export default function Home() {
  const router = useRouter();
  const { state } = useAuth();

  useEffect(() => {
    if (!state.isLoading) {
      router.replace(state.isAuthenticated ? "/todos" : "/login");
    }
  }, [router, state.isAuthenticated, state.isLoading]);

  return (
    <section className="d-flex min-vh-50 align-items-center justify-content-center py-5">
      <div className="text-center">
        <div className="spinner-border text-primary" role="status" aria-label="Loading">
          <span className="visually-hidden">Loading TaskFlow…</span>
        </div>
        <p className="text-muted mt-3 mb-0">Preparing your workspace…</p>
      </div>
    </section>
  );
}
