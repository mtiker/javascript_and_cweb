import { useEffect, useState, type PropsWithChildren } from "react";
import { NavLink } from "react-router-dom";
import { useAuth } from "../lib/auth";
import { useLanguage, type AppLanguage } from "../lib/language";
import type { AuthSession, GymSummary, TenantAccess } from "../lib/types";

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
  const canSwitchAllTenants = Boolean(session?.systemRoles.includes("SystemAdmin"));
  const assignedTenantOptions = session?.availableTenants ?? [];
  const tenantOptions = canSwitchAllTenants ? toSystemTenantOptions(gyms) : assignedTenantOptions;
  const activeTenantOption = tenantOptions.find((tenant) => tenant.gymCode === session?.activeGymCode);
  const roleOptions = getRoleOptions(session, activeTenantOption);
  const canSwitchTenant = tenantOptions.length > 1 || canSwitchAllTenants;
  const canSwitchTenantRole = Boolean(session?.activeGymCode && roleOptions.length > 1);

  useEffect(() => {
    if (!canSwitchAllTenants) {
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
  }, [api, canSwitchAllTenants]);
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
          {canSwitchTenant && tenantOptions.length > 0 ? (
            <label className="language-select">
              <span>{t("switchTenant")}</span>
              <select
                disabled={isSwitchingTenant}
                onChange={(event) => void handleTenantSwitch(event.target.value)}
                value={session?.activeGymCode ?? ""}
              >
                {canSwitchAllTenants ? <option value="">{t("system")}</option> : null}
                {tenantOptions.map((tenant) => (
                  <option key={tenant.gymId} value={tenant.gymCode}>
                    {tenant.gymName}
                  </option>
                ))}
              </select>
            </label>
          ) : null}
          {canSwitchTenantRole ? (
            <label className="language-select">
              <span>{t("switchRole")}</span>
              <select
                disabled={isSwitchingTenant}
                onChange={(event) => void handleRoleSwitch(event.target.value)}
                value={session?.activeRole ?? ""}
              >
                {roleOptions.map((role) => (
                  <option key={role} value={role}>
                    {role}
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
      if (canSwitchAllTenants) {
        await switchRole("GymOwner");
      }
    } finally {
      setIsSwitchingTenant(false);
    }
  }

  async function handleRoleSwitch(roleName: string) {
    if (!roleName || roleName === session?.activeRole) {
      return;
    }

    setIsSwitchingTenant(true);
    try {
      await switchRole(roleName);
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

function toSystemTenantOptions(gyms: GymSummary[]): TenantAccess[] {
  return gyms.map((gym) => ({
    gymId: gym.gymId,
    gymCode: gym.code,
    gymName: gym.name,
    roles: ["GymOwner", "GymAdmin"],
  }));
}

function getRoleOptions(session: AuthSession | null, activeTenantOption?: TenantAccess) {
  if (!session?.activeGymCode) {
    return [];
  }

  if (session.systemRoles.includes("SystemAdmin")) {
    return activeTenantOption?.roles ?? ["GymOwner", "GymAdmin"];
  }

  return activeTenantOption?.roles ?? [];
}
