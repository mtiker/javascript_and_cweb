import type { PropsWithChildren } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "../lib/auth";

const adminNavigationItems = [
  { to: "/members", label: "Members" },
  { to: "/sessions", label: "Sessions" },
  { to: "/training-categories", label: "Training Categories" },
  { to: "/membership-packages", label: "Membership Packages" },
];

export function AppShell({ children }: PropsWithChildren) {
  const { logout, session } = useAuth();
  const navigationItems = [
    ...(canUseAdminTools(session?.activeRole) ? adminNavigationItems : [{ to: "/sessions", label: "Sessions" }]),
    ...(canUseAttendance(session?.activeRole) ? [{ to: "/attendance", label: "Attendance" }] : []),
    ...(canUseMaintenance(session?.activeRole) ? [{ to: "/maintenance", label: "Maintenance" }] : []),
  ];

  return (
    <div className="shell">
      <aside className="shell__sidebar">
        <p className="shell__eyebrow">Separate Client App</p>
        <h1 className="shell__title">Gym Operations Workspace</h1>
        <p className="shell__subtitle">
          A focused REST client for proving JWT, refresh tokens, tenant workflows, and role-specific operations.
        </p>
        <nav aria-label="Primary" className="shell__nav">
          {navigationItems.map((item) => (
            <NavLink
              className={({ isActive }) => (isActive ? "shell__nav-link shell__nav-link--active" : "shell__nav-link")}
              key={item.to}
              to={item.to}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="shell__main">
        <header className="shell__header">
          <div>
            <p className="shell__meta-label">Active gym</p>
            <strong className="shell__meta-value">{session?.activeGymCode}</strong>
          </div>
          <div>
            <p className="shell__meta-label">Role</p>
            <strong className="shell__meta-value">{session?.activeRole}</strong>
          </div>
          <button className="button button--secondary" onClick={() => void logout()} type="button">
            Log out
          </button>
        </header>
        <main className="shell__content">{children}</main>
      </div>
    </div>
  );
}

function canUseAdminTools(role?: string | null) {
  return role === "GymAdmin" || role === "GymOwner";
}

function canUseAttendance(role?: string | null) {
  return role === "GymAdmin" || role === "GymOwner" || role === "Trainer";
}

function canUseMaintenance(role?: string | null) {
  return role === "GymAdmin" || role === "GymOwner" || role === "Caretaker";
}
