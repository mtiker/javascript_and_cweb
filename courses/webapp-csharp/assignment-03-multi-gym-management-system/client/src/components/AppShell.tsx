import type { PropsWithChildren } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "../lib/auth";
import { useLanguage, type AppLanguage } from "../lib/language";
import type { AuthSession } from "../lib/types";

const tenantAdminNavigationItems = [
  { to: "/members", label: "Members" },
  { to: "/sessions", label: "Sessions" },
  { to: "/training-categories", label: "Training Categories" },
  { to: "/membership-packages", label: "Membership Packages" },
  { to: "/console", label: "Function Console" },
];

export function AppShell({ children }: PropsWithChildren) {
  const { logout, session } = useAuth();
  const { language, setLanguage, t } = useLanguage();
  const navigationItems = [
    ...(hasSystemRole(session) ? [{ to: "/platform", label: t("platform") }, { to: "/console", label: t("console") }] : []),
    ...(canUseAdminTools(session?.activeRole)
      ? tenantAdminNavigationItems.map((item) => ({ ...item, label: translateNavigationLabel(item.label, t) }))
      : session?.activeGymCode
        ? [{ to: "/sessions", label: t("sessions") }]
        : []),
    ...(canUseAttendance(session?.activeRole) ? [{ to: "/attendance", label: t("attendance") }] : []),
    ...(canUseMaintenance(session?.activeRole) ? [{ to: "/maintenance", label: t("caretaker") }] : []),
  ];

  return (
    <div className="shell">
      <aside className="shell__sidebar">
        <p className="shell__eyebrow">SaaS client</p>
        <h1 className="shell__title">{t("appTitle")}</h1>
        <p className="shell__subtitle">JWT, refresh tokens, platform administration, tenant operations, and role workspaces.</p>
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
            <p className="shell__meta-label">{t("activeGym")}</p>
            <strong className="shell__meta-value">{session?.activeGymCode ?? "System"}</strong>
          </div>
          <div>
            <p className="shell__meta-label">{session?.activeRole ? t("role") : t("systemRoles")}</p>
            <strong className="shell__meta-value">{session?.activeRole ?? session?.systemRoles.join(", ")}</strong>
          </div>
          <label className="language-select">
            <span>{t("language")}</span>
            <select onChange={(event) => setLanguage(event.target.value as AppLanguage)} value={language}>
              <option value="en">EN</option>
              <option value="et-EE">ET</option>
            </select>
          </label>
          <button className="button button--secondary" onClick={() => void logout()} type="button">
            {t("logOut")}
          </button>
        </header>
        <main className="shell__content">{children}</main>
      </div>
    </div>
  );
}

function hasSystemRole(session: AuthSession | null) {
  return Boolean(session?.systemRoles.some((role) => role === "SystemAdmin" || role === "SystemSupport" || role === "SystemBilling"));
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

function translateNavigationLabel(label: string, t: ReturnType<typeof useLanguage>["t"]) {
  switch (label) {
    case "Members":
      return t("members");
    case "Sessions":
      return t("sessions");
    case "Training Categories":
      return t("trainingCategories");
    case "Membership Packages":
      return t("membershipPackages");
    case "Function Console":
      return t("console");
    default:
      return label;
  }
}
