"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/context/AuthContext";

export default function NavBar() {
  const { state, logout } = useAuth();
  const pathname = usePathname();

  if (state.isLoading) return null;

  const isActive = (href: string) =>
    pathname === href ? "active fw-semibold" : "";

  return (
    <nav className="navbar navbar-expand-lg navbar-tf">
      <div className="container">
        <Link className="navbar-brand d-flex align-items-center gap-2" href="/">
          <span className="navbar-brand-mark" aria-hidden="true">
            TF
          </span>
          <span>TaskFlow</span>
        </Link>

        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#tfNav"
          aria-controls="tfNav"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon" />
        </button>

        <div className="collapse navbar-collapse" id="tfNav">
          {state.isAuthenticated && (
            <ul className="navbar-nav me-auto mb-2 mb-lg-0">
              <li className="nav-item">
                <Link className={`nav-link ${isActive("/todos")}`} href="/todos">
                  Todos
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  className={`nav-link ${isActive("/categories")}`}
                  href="/categories"
                >
                  Categories
                </Link>
              </li>
              <li className="nav-item">
                <Link
                  className={`nav-link ${isActive("/priorities")}`}
                  href="/priorities"
                >
                  Priorities
                </Link>
              </li>
            </ul>
          )}

          <div className="d-flex align-items-center gap-3 ms-auto">
            {state.isAuthenticated ? (
              <>
                <span className="navbar-text text-light small">
                  {state.userEmail}
                </span>
                <button
                  type="button"
                  className="btn btn-outline-light btn-sm"
                  onClick={logout}
                >
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link className="btn btn-outline-light btn-sm" href="/login">
                  Login
                </Link>
                <Link className="btn btn-light btn-sm fw-semibold" href="/register">
                  Register
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
}
