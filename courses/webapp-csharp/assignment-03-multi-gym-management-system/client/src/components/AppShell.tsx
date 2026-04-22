import { useEffect, useState, type PropsWithChildren } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "../lib/auth";
import { useLanguage, type AppLanguage } from "../lib/language";
import type { AuthSession, GymSummary } from "../lib/types";

const tenantAdminNavigationItems = [
  { to: "/members", label: "Members" },
  { to: "/sessions", label: "Sessions" },
  { to: "/training-categories", label: "Training Categories" },
  { to: "/membership-packages", label: "Membership Packages" },
  { to: "/console", label: "Function Console" },
];

export function AppShell({ children }: PropsWithChildren) {
  const { api, logout, session, switchGym, switchRole } = useAuth();
  const { language, setLanguage, t } = useLanguage();
  const [gyms, setGyms] = useState<GymSummary[]>([]);
  const [isSwitchingTenant, setIsSwitchingTenant] = useState(false);
  const systemRole = hasSystemRole(session);
  const canSwitchTenant = Boolean(session?.systemRoles.includes("SystemAdmin"));

  useEffect(() => {
    if (!canSwitchTenant) {
      setGyms([]);
      return;
    }

    let isMounted = true;
    api
      .getGyms()
      .then((loadedGyms) => {
        if (isMounted) {
          setGyms(loadedGyms.filter((gym) => gym.isActive));
        }
      })
      .catch(() => {
        if (isMounted) {
          setGyms([]);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [api, canSwitchTenant]);
  const navigationItems = [
    ...(systemRole ? [{ to: "/platform", label: t("platform") }, { to: "/console", label: t("console") }] : []),
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
        <p className="shell__subtitle">{t("shellSubtitle")}</p>
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
            <strong className="shell__meta-value">{session?.activeGymCode ?? t("system")}</strong>
          </div>
          <div>
            <p className="shell__meta-label">{session?.activeRole ? t("activeRole") : t("systemRoles")}</p>
            <strong className="shell__meta-value">{session?.activeRole ?? session?.systemRoles.join(", ")}</strong>
          </div>
          {canSwitchTenant && gyms.length > 0 ? (
            <label className="language-select">
              <span>{t("switchTenant")}</span>
              <select
                disabled={isSwitchingTenant}
                onChange={(event) => void handleTenantSwitch(event.target.value)}
                value={session?.activeGymCode ?? ""}
              >
                <option value="">{t("system")}</option>
                {gyms.map((gym) => (
                  <option key={gym.gymId} value={gym.code}>
                    {gym.name}
                  </option>
                ))}
              </select>
            </label>
          ) : null}
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

  async function handleTenantSwitch(gymCode: string) {
    if (!gymCode) {
      return;
    }

    setIsSwitchingTenant(true);
    try {
      await switchGym(gymCode);
      await switchRole("GymOwner");
    } finally {
      setIsSwitchingTenant(false);
    }
  }
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
