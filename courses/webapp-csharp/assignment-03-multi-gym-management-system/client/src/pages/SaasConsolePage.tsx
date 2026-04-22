import { useEffect, useMemo, useState } from "react";
import { NoticeBanner } from "../components/NoticeBanner";
import { useAuth } from "../lib/auth";
import type {
  Booking,
  Equipment,
  EquipmentModel,
  GymSettings,
  GymSummary,
  GymUser,
  HttpMethod,
  MaintenanceTask,
  MemberSummary,
  Membership,
  MembershipPackage,
  Notice,
  OpeningHours,
  OpeningHoursException,
  Payment,
  PlatformAnalytics,
  Staff,
  SubscriptionSummary,
  SupportTicket,
  TrainingCategory,
  TrainingSession,
} from "../lib/types";
import { getErrorMessages } from "../lib/types";

interface OverviewState {
  analytics: PlatformAnalytics | null;
  gyms: GymSummary[];
  subscriptions: SubscriptionSummary[];
  supportTickets: SupportTicket[];
  members: MemberSummary[];
  staff: Staff[];
  trainingCategories: TrainingCategory[];
  sessions: TrainingSession[];
  bookings: Booking[];
  packages: MembershipPackage[];
  memberships: Membership[];
  payments: Payment[];
  openingHours: OpeningHours[];
  openingHoursExceptions: OpeningHoursException[];
  equipmentModels: EquipmentModel[];
  equipment: Equipment[];
  maintenanceTasks: MaintenanceTask[];
  gymSettings: GymSettings | null;
  gymUsers: GymUser[];
}

interface ConsoleAction {
  key: string;
  title: string;
  method: HttpMethod;
  path: string;
  body?: unknown;
  params?: string[];
}

interface ActionGroup {
  title: string;
  actions: ConsoleAction[];
}

const emptyOverview = (): OverviewState => ({
  analytics: null,
  gyms: [],
  subscriptions: [],
  supportTickets: [],
  members: [],
  staff: [],
  trainingCategories: [],
  sessions: [],
  bookings: [],
  packages: [],
  memberships: [],
  payments: [],
  openingHours: [],
  openingHoursExceptions: [],
  equipmentModels: [],
  equipment: [],
  maintenanceTasks: [],
  gymSettings: null,
  gymUsers: [],
});

export function SaasConsolePage() {
  const { api, session, switchGym, switchRole } = useAuth();
  const [overview, setOverview] = useState<OverviewState>(() => emptyOverview());
  const [isLoading, setIsLoading] = useState(true);
  const [notice, setNotice] = useState<Notice | null>(null);

  const hasSystemRole = Boolean(session?.systemRoles.length);
  const canUseTenantAdmin = session?.activeRole === "GymOwner" || session?.activeRole === "GymAdmin";
  const activeGymCode = session?.activeGymCode ?? "peak-forge";

  useEffect(() => {
    void loadOverview();
  }, [session?.activeGymCode, session?.activeRole, session?.systemRoles.join("|")]);

  const defaults = useMemo(() => {
    const firstGym = overview.gyms[0];
    const firstMember = overview.members[0];
    const firstStaff = overview.staff[0];
    const firstCategory = overview.trainingCategories[0];
    const firstSession = overview.sessions[0];
    const firstBooking = overview.bookings[0];
    const firstPackage = overview.packages[0];
    const firstMembership = overview.memberships[0];
    const firstOpeningHours = overview.openingHours[0];
    const firstOpeningException = overview.openingHoursExceptions[0];
    const firstEquipmentModel = overview.equipmentModels[0];
    const firstEquipment = overview.equipment[0];
    const firstTask = overview.maintenanceTasks[0];
    const firstGymUser = overview.gymUsers[0];

    return {
      appUserId: firstGymUser?.appUserId ?? "00000000-0000-0000-0000-000000000000",
      bookingId: firstBooking?.id ?? "00000000-0000-0000-0000-000000000000",
      categoryId: firstCategory?.id ?? "00000000-0000-0000-0000-000000000000",
      contractId: "00000000-0000-0000-0000-000000000000",
      equipmentId: firstEquipment?.id ?? "00000000-0000-0000-0000-000000000000",
      equipmentModelId: firstEquipmentModel?.id ?? "00000000-0000-0000-0000-000000000000",
      exceptionId: firstOpeningException?.id ?? "00000000-0000-0000-0000-000000000000",
      gymCode: activeGymCode,
      gymId: firstGym?.gymId ?? session?.activeGymId ?? "00000000-0000-0000-0000-000000000000",
      jobRoleId: "00000000-0000-0000-0000-000000000000",
      maintenanceTaskId: firstTask?.id ?? "00000000-0000-0000-0000-000000000000",
      memberId: firstMember?.id ?? "00000000-0000-0000-0000-000000000000",
      membershipId: firstMembership?.id ?? "00000000-0000-0000-0000-000000000000",
      openingHoursId: firstOpeningHours?.id ?? "00000000-0000-0000-0000-000000000000",
      packageId: firstPackage?.id ?? "00000000-0000-0000-0000-000000000000",
      roleName: session?.activeRole ?? "GymAdmin",
      sessionId: firstSession?.id ?? "00000000-0000-0000-0000-000000000000",
      staffId: firstStaff?.id ?? "00000000-0000-0000-0000-000000000000",
      userId: firstGymUser?.appUserId ?? "00000000-0000-0000-0000-000000000000",
      vacationId: "00000000-0000-0000-0000-000000000000",
      workShiftId: "00000000-0000-0000-0000-000000000000",
    };
  }, [activeGymCode, overview, session?.activeGymId, session?.activeRole]);

  const actionGroups = useMemo(
    () => buildActionGroups({ canUseTenantAdmin, defaults, hasSystemRole }),
    [canUseTenantAdmin, defaults, hasSystemRole],
  );

  async function loadOverview() {
    setIsLoading(true);
    setNotice(null);

    const next = emptyOverview();

    try {
      if (hasSystemRole) {
        const [analytics, gyms, subscriptions, supportTickets] = await Promise.all([
          safeLoad(() => api.getPlatformAnalytics(), null),
          safeLoad(() => api.getGyms(), []),
          safeLoad(() => api.getSubscriptions(), []),
          safeLoad(() => api.getSupportTickets(), []),
        ]);

        next.analytics = analytics;
        next.gyms = gyms;
        next.subscriptions = subscriptions;
        next.supportTickets = supportTickets;
      }

      if (canUseTenantAdmin && session?.activeGymCode) {
        const [
          members,
          staff,
          trainingCategories,
          sessions,
          bookings,
          packages,
          memberships,
          payments,
          openingHours,
          openingHoursExceptions,
          equipmentModels,
          equipment,
          maintenanceTasks,
          gymSettings,
          gymUsers,
        ] = await Promise.all([
          safeLoad(() => api.getMembers(session.activeGymCode!), []),
          safeLoad(() => api.getStaff(session.activeGymCode!), []),
          safeLoad(() => api.getTrainingCategories(session.activeGymCode!), []),
          safeLoad(() => api.getTrainingSessions(session.activeGymCode!), []),
          safeLoad(() => api.getBookings(session.activeGymCode!), []),
          safeLoad(() => api.getMembershipPackages(session.activeGymCode!), []),
          safeLoad(() => api.getMemberships(session.activeGymCode!), []),
          safeLoad(() => api.getPayments(session.activeGymCode!), []),
          safeLoad(() => api.getOpeningHours(session.activeGymCode!), []),
          safeLoad(() => api.getOpeningHoursExceptions(session.activeGymCode!), []),
          safeLoad(() => api.getEquipmentModels(session.activeGymCode!), []),
          safeLoad(() => api.getEquipment(session.activeGymCode!), []),
          safeLoad(() => api.getMaintenanceTasks(session.activeGymCode!), []),
          safeLoad(() => api.getGymSettings(session.activeGymCode!), null),
          safeLoad(() => api.getGymUsers(session.activeGymCode!), []),
        ]);

        Object.assign(next, {
          members,
          staff,
          trainingCategories,
          sessions,
          bookings,
          packages,
          memberships,
          payments,
          openingHours,
          openingHoursExceptions,
          equipmentModels,
          equipment,
          maintenanceTasks,
          gymSettings,
          gymUsers,
        });
      }

      setOverview(next);
    } catch (error) {
      setNotice({
        tone: "error",
        title: "Could not load SaaS console",
        messages: getErrorMessages(error),
      });
    } finally {
      setIsLoading(false);
    }
  }

  async function runAction(action: ConsoleAction, path: string, bodyText: string) {
    if (action.key === "switch-gym") {
      const payload = JSON.parse(bodyText) as { gymCode?: string };
      await switchGym(payload.gymCode ?? activeGymCode);
      return { activeGymCode: payload.gymCode ?? activeGymCode };
    }

    if (action.key === "switch-role") {
      const payload = JSON.parse(bodyText) as { roleName?: string };
      await switchRole(payload.roleName ?? "GymAdmin");
      return { activeRole: payload.roleName ?? "GymAdmin" };
    }

    const response = await api.sendRaw(path, action.method, bodyText);
    await loadOverview();
    return response.data;
  }

  return (
    <section className="workspace">
      <header className="workspace__header">
        <div>
          <p className="workspace__eyebrow">SaaS coverage</p>
          <h2 className="workspace__title">Platform and Tenant Console</h2>
          <p className="workspace__copy workspace__copy--dark">
            The same REST client now exposes platform, support, billing, onboarding, tenant administration, scheduling,
            membership, facility, and account functions.
          </p>
        </div>
        <button className="button button--secondary" disabled={isLoading} onClick={() => void loadOverview()} type="button">
          {isLoading ? "Refreshing..." : "Refresh"}
        </button>
      </header>

      <NoticeBanner notice={notice} />

      <div className="overview-grid">
        {hasSystemRole ? <PlatformOverview overview={overview} /> : null}
        {canUseTenantAdmin ? <TenantOverview activeGymCode={activeGymCode} overview={overview} /> : null}
        {!hasSystemRole && !canUseTenantAdmin ? (
          <section className="panel">
            <p className="state">This account uses a focused member, trainer, or caretaker workspace.</p>
          </section>
        ) : null}
      </div>

      <section className="console-groups" aria-label="Available API functions">
        {actionGroups.map((group) => (
          <details className="console-group" key={group.title} open={group.title === "Account" || group.title === "Platform"}>
            <summary>{group.title}</summary>
            <div className="console-actions">
              {group.actions.map((action) => (
                <ActionCard action={action} defaults={defaults} key={action.key} onRun={runAction} />
              ))}
            </div>
          </details>
        ))}
      </section>
    </section>
  );
}

function PlatformOverview({ overview }: { overview: OverviewState }) {
  return (
    <section className="panel">
      <div className="editor-header">
        <div>
          <p className="workspace__eyebrow">System</p>
          <h3>Platform</h3>
        </div>
      </div>
      <div className="metric-grid">
        <Metric label="Gyms" value={overview.analytics?.gymCount ?? overview.gyms.length} />
        <Metric label="Users" value={overview.analytics?.userCount ?? "-"} />
        <Metric label="Members" value={overview.analytics?.memberCount ?? "-"} />
        <Metric label="Open tickets" value={overview.analytics?.openSupportTicketCount ?? overview.supportTickets.length} />
      </div>
      <MiniTable
        columns={["Gym", "Code", "Status"]}
        rows={overview.gyms.map((gym) => [gym.name, gym.code, gym.isActive ? "Active" : "Inactive"])}
        emptyText="No gyms loaded."
      />
      <MiniTable
        columns={["Subscription", "Plan", "Status"]}
        rows={overview.subscriptions.map((item) => [item.gymName, planLabel(item.plan), subscriptionStatusLabel(item.status)])}
        emptyText="No subscriptions loaded."
      />
    </section>
  );
}

function TenantOverview({ activeGymCode, overview }: { activeGymCode: string; overview: OverviewState }) {
  return (
    <section className="panel">
      <div className="editor-header">
        <div>
          <p className="workspace__eyebrow">Tenant</p>
          <h3>{activeGymCode}</h3>
        </div>
      </div>
      <div className="metric-grid">
        <Metric label="Members" value={overview.members.length} />
        <Metric label="Staff" value={overview.staff.length} />
        <Metric label="Sessions" value={overview.sessions.length} />
        <Metric label="Open maintenance" value={overview.maintenanceTasks.filter((task) => task.status !== 2).length} />
      </div>
      <MiniTable
        columns={["Catalog", "Records", "Notes"]}
        rows={[
          ["Training categories", String(overview.trainingCategories.length), "Session taxonomy"],
          ["Membership packages", String(overview.packages.length), "Sales catalog"],
          ["Equipment", String(overview.equipment.length), "Assets"],
          ["Gym users", String(overview.gymUsers.length), "Role assignments"],
        ]}
        emptyText="No tenant data loaded."
      />
      {overview.gymSettings ? (
        <p className="state">
          {overview.gymSettings.currencyCode} / {overview.gymSettings.timeZone} / cancellation window{" "}
          {overview.gymSettings.bookingCancellationHours}h
        </p>
      ) : null}
    </section>
  );
}

function ActionCard({
  action,
  defaults,
  onRun,
}: {
  action: ConsoleAction;
  defaults: Record<string, string>;
  onRun: (action: ConsoleAction, path: string, bodyText: string) => Promise<unknown>;
}) {
  const [paramValues, setParamValues] = useState<Record<string, string>>(() => buildParamValues(action, defaults));
  const [bodyText, setBodyText] = useState(() => (action.body === undefined ? "" : JSON.stringify(action.body, null, 2)));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<string>("");
  const [error, setError] = useState<string>("");

  useEffect(() => {
    setParamValues(buildParamValues(action, defaults));
    setBodyText(action.body === undefined ? "" : JSON.stringify(action.body, null, 2));
  }, [action, defaults]);

  const path = (action.params ?? []).reduce(
    (current, param) => current.replace(`{${param}}`, encodeURIComponent(paramValues[param] ?? "")),
    action.path,
  );

  async function submit() {
    setIsSubmitting(true);
    setResult("");
    setError("");

    try {
      const data = await onRun(action, path, bodyText);
      setResult(JSON.stringify(data ?? { ok: true }, null, 2));
    } catch (exception) {
      setError(getErrorMessages(exception)[0] ?? "Request failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <article className="console-action">
      <div className="console-action__header">
        <span className={`method-badge method-badge--${action.method.toLowerCase()}`}>{action.method}</span>
        <strong>{action.title}</strong>
      </div>
      <code className="endpoint">{path}</code>
      {(action.params ?? []).length > 0 ? (
        <div className="form__two-up">
          {action.params!.map((param) => (
            <label className="field" key={param}>
              <span>{param}</span>
              <input
                onChange={(event) => setParamValues((current) => ({ ...current, [param]: event.target.value }))}
                value={paramValues[param] ?? ""}
              />
            </label>
          ))}
        </div>
      ) : null}
      {action.body !== undefined ? (
        <label className="field">
          <span>JSON body</span>
          <textarea onChange={(event) => setBodyText(event.target.value)} rows={8} value={bodyText} />
        </label>
      ) : null}
      <button className="button" disabled={isSubmitting} onClick={() => void submit()} type="button">
        {isSubmitting ? "Running..." : "Run"}
      </button>
      {error ? <p className="state state--error">{error}</p> : null}
      {result ? <pre className="response-preview">{result}</pre> : null}
    </article>
  );
}

function Metric({ label, value }: { label: string; value: number | string }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function MiniTable({ columns, emptyText, rows }: { columns: string[]; emptyText: string; rows: string[][] }) {
  if (rows.length === 0) {
    return <p className="state">{emptyText}</p>;
  }

  return (
    <div className="table-scroll">
      <table className="mini-table">
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column}>{column}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={row.join("|") || rowIndex}>
              {row.map((cell, cellIndex) => (
                <td key={`${rowIndex}-${cellIndex}`}>{cell}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function buildParamValues(action: ConsoleAction, defaults: Record<string, string>) {
  return Object.fromEntries((action.params ?? []).map((param) => [param, defaults[param] ?? ""]));
}

function buildActionGroups({
  canUseTenantAdmin,
  defaults,
  hasSystemRole,
}: {
  canUseTenantAdmin: boolean;
  defaults: Record<string, string>;
  hasSystemRole: boolean;
}): ActionGroup[] {
  const groups: ActionGroup[] = [
    {
      title: "Account",
      actions: [
        action("switch-gym", "Switch active gym", "POST", "/api/v1/account/switch-gym", { gymCode: defaults.gymCode }),
        action("switch-role", "Switch active role", "POST", "/api/v1/account/switch-role", { roleName: defaults.roleName }),
        action("forgot-password", "Generate password reset token", "POST", "/api/v1/account/forgot-password", {
          email: "member@peakforge.local",
        }),
        action("reset-password", "Reset password with token", "POST", "/api/v1/account/reset-password", {
          email: "member@peakforge.local",
          resetToken: "paste-token-here",
          newPassword: "Gym123!",
        }),
      ],
    },
  ];

  if (hasSystemRole) {
    groups.push({
      title: "Platform",
      actions: [
        action("platform-analytics", "Load analytics", "GET", "/api/v1/system/platform/analytics"),
        action("gyms-list", "List gyms", "GET", "/api/v1/system/gyms"),
        action("gyms-register", "Register gym", "POST", "/api/v1/system/gyms", {
          name: "Harbor Strength",
          code: "harbor-strength",
          registrationCode: "GYM-NEW",
          addressLine: "Demo 1",
          city: "Tallinn",
          postalCode: "10111",
          country: "Estonia",
          ownerEmail: "owner@harbor-strength.local",
          ownerPassword: "Gym123!",
          ownerFirstName: "Harbor",
          ownerLastName: "Owner",
        }),
        action("gyms-activation", "Update gym activation", "PUT", "/api/v1/system/gyms/{gymId}/activation", {
          isActive: true,
        }, ["gymId"]),
        action("gyms-snapshot", "Load gym snapshot", "GET", "/api/v1/system/gyms/{gymId}/snapshot", undefined, ["gymId"]),
        action("subscriptions-list", "List subscriptions", "GET", "/api/v1/system/subscriptions"),
        action("subscriptions-update", "Update subscription", "PUT", "/api/v1/system/subscriptions/{gymId}", {
          plan: 1,
          status: 1,
          endDate: null,
          monthlyPrice: 129,
        }, ["gymId"]),
        action("support-list", "List support tickets", "GET", "/api/v1/system/support"),
        action("support-create", "Create support ticket", "POST", "/api/v1/system/support/{gymId}/tickets", {
          title: "Demo support ticket",
          description: "Created from the React SaaS console.",
          priority: 1,
        }, ["gymId"]),
        action("impersonation-start", "Start impersonation", "POST", "/api/v1/system/impersonation", {
          userId: defaults.userId,
          gymCode: defaults.gymCode,
        }),
      ],
    });
  }

  if (canUseTenantAdmin) {
    groups.push(
      {
        title: "Members and Staff",
        actions: [
          action("members-list", "List members", "GET", "/api/v1/{gymCode}/members", undefined, ["gymCode"]),
          action("members-current", "Current member profile", "GET", "/api/v1/{gymCode}/members/me", undefined, ["gymCode"]),
          action("members-detail", "Load member", "GET", "/api/v1/{gymCode}/members/{memberId}", undefined, ["gymCode", "memberId"]),
          action("members-create", "Create member", "POST", "/api/v1/{gymCode}/members", {
            firstName: "Demo",
            lastName: "Member",
            personalCode: null,
            dateOfBirth: null,
            memberCode: `MEM-${Date.now()}`,
            status: 0,
          }, ["gymCode"]),
          action("members-update", "Update member", "PUT", "/api/v1/{gymCode}/members/{memberId}", {
            firstName: "Updated",
            lastName: "Member",
            personalCode: null,
            dateOfBirth: null,
            memberCode: "MEM-UPDATED",
            status: 0,
          }, ["gymCode", "memberId"]),
          action("members-delete", "Delete member", "DELETE", "/api/v1/{gymCode}/members/{memberId}", undefined, ["gymCode", "memberId"]),
          action("staff-list", "List staff", "GET", "/api/v1/{gymCode}/staff", undefined, ["gymCode"]),
          action("staff-create", "Create staff", "POST", "/api/v1/{gymCode}/staff", {
            firstName: "Demo",
            lastName: "Coach",
            staffCode: `STF-${Date.now()}`,
            status: 0,
          }, ["gymCode"]),
          action("staff-update", "Update staff", "PUT", "/api/v1/{gymCode}/staff/{staffId}", {
            firstName: "Updated",
            lastName: "Coach",
            staffCode: "STF-UPDATED",
            status: 0,
          }, ["gymCode", "staffId"]),
          action("staff-delete", "Delete staff", "DELETE", "/api/v1/{gymCode}/staff/{staffId}", undefined, ["gymCode", "staffId"]),
          action("job-roles-list", "List job roles", "GET", "/api/v1/{gymCode}/job-roles", undefined, ["gymCode"]),
          action("job-roles-create", "Create job role", "POST", "/api/v1/{gymCode}/job-roles", {
            code: "front-desk",
            title: "Front Desk",
            description: "Handles reception and member service.",
          }, ["gymCode"]),
          action("job-roles-update", "Update job role", "PUT", "/api/v1/{gymCode}/job-roles/{jobRoleId}", {
            code: "front-desk",
            title: "Front Desk Lead",
            description: "Handles reception and member service.",
          }, ["gymCode", "jobRoleId"]),
          action("job-roles-delete", "Delete job role", "DELETE", "/api/v1/{gymCode}/job-roles/{jobRoleId}", undefined, ["gymCode", "jobRoleId"]),
        ],
      },
      {
        title: "Scheduling",
        actions: [
          action("categories-list", "List training categories", "GET", "/api/v1/{gymCode}/training-categories", undefined, ["gymCode"]),
          action("sessions-list", "List sessions", "GET", "/api/v1/{gymCode}/training-sessions", undefined, ["gymCode"]),
          action("sessions-detail", "Load session", "GET", "/api/v1/{gymCode}/training-sessions/{sessionId}", undefined, ["gymCode", "sessionId"]),
          action("sessions-create", "Create session", "POST", "/api/v1/{gymCode}/training-sessions", {
            categoryId: defaults.categoryId,
            name: "Demo Conditioning",
            description: "Created from the React SaaS console.",
            startAtUtc: new Date(Date.now() + 86400000).toISOString(),
            endAtUtc: new Date(Date.now() + 90000000).toISOString(),
            capacity: 10,
            basePrice: 15,
            currencyCode: "EUR",
            status: 1,
            trainerContractIds: [],
          }, ["gymCode"]),
          action("sessions-update", "Update session", "PUT", "/api/v1/{gymCode}/training-sessions/{sessionId}", {
            categoryId: defaults.categoryId,
            name: "Updated Conditioning",
            description: "Updated from the React SaaS console.",
            startAtUtc: new Date(Date.now() + 86400000).toISOString(),
            endAtUtc: new Date(Date.now() + 90000000).toISOString(),
            capacity: 12,
            basePrice: 18,
            currencyCode: "EUR",
            status: 1,
            trainerContractIds: [],
          }, ["gymCode", "sessionId"]),
          action("sessions-delete", "Delete session", "DELETE", "/api/v1/{gymCode}/training-sessions/{sessionId}", undefined, ["gymCode", "sessionId"]),
          action("work-shifts-list", "List work shifts", "GET", "/api/v1/{gymCode}/work-shifts", undefined, ["gymCode"]),
          action("work-shifts-create", "Create work shift", "POST", "/api/v1/{gymCode}/work-shifts", {
            contractId: defaults.contractId,
            startAtUtc: new Date(Date.now() + 86400000).toISOString(),
            endAtUtc: new Date(Date.now() + 90000000).toISOString(),
            shiftType: 1,
            trainingSessionId: null,
            comment: "Console shift",
          }, ["gymCode"]),
          action("work-shifts-update", "Update work shift", "PUT", "/api/v1/{gymCode}/work-shifts/{workShiftId}", {
            contractId: defaults.contractId,
            startAtUtc: new Date(Date.now() + 86400000).toISOString(),
            endAtUtc: new Date(Date.now() + 90000000).toISOString(),
            shiftType: 1,
            trainingSessionId: null,
            comment: "Updated console shift",
          }, ["gymCode", "workShiftId"]),
          action("work-shifts-delete", "Delete work shift", "DELETE", "/api/v1/{gymCode}/work-shifts/{workShiftId}", undefined, ["gymCode", "workShiftId"]),
          action("bookings-list", "List bookings", "GET", "/api/v1/{gymCode}/bookings", undefined, ["gymCode"]),
          action("bookings-create", "Create booking", "POST", "/api/v1/{gymCode}/bookings", {
            trainingSessionId: defaults.sessionId,
            memberId: defaults.memberId,
            paymentReference: "CONSOLE-DEMO",
          }, ["gymCode"]),
          action("bookings-attendance", "Update attendance", "PUT", "/api/v1/{gymCode}/bookings/{bookingId}/attendance", {
            status: 2,
          }, ["gymCode", "bookingId"]),
          action("bookings-delete", "Cancel booking", "DELETE", "/api/v1/{gymCode}/bookings/{bookingId}", undefined, ["gymCode", "bookingId"]),
        ],
      },
      {
        title: "Memberships and Payments",
        actions: [
          action("packages-list", "List packages", "GET", "/api/v1/{gymCode}/membership-packages", undefined, ["gymCode"]),
          action("memberships-list", "List memberships", "GET", "/api/v1/{gymCode}/memberships", undefined, ["gymCode"]),
          action("memberships-sell", "Sell membership", "POST", "/api/v1/{gymCode}/memberships", {
            memberId: defaults.memberId,
            membershipPackageId: defaults.packageId,
            requestedStartDate: null,
            paymentReference: "CONSOLE-MEMBERSHIP",
          }, ["gymCode"]),
          action("memberships-delete", "Delete membership", "DELETE", "/api/v1/{gymCode}/memberships/{membershipId}", undefined, ["gymCode", "membershipId"]),
          action("payments-list", "List payments", "GET", "/api/v1/{gymCode}/payments", undefined, ["gymCode"]),
          action("payments-create", "Create payment", "POST", "/api/v1/{gymCode}/payments", {
            amount: 25,
            currencyCode: "EUR",
            reference: "CONSOLE-PAYMENT",
            membershipId: defaults.membershipId,
            bookingId: null,
          }, ["gymCode"]),
        ],
      },
      {
        title: "Facilities and Tenant Settings",
        actions: [
          action("opening-hours-list", "List opening hours", "GET", "/api/v1/{gymCode}/opening-hours", undefined, ["gymCode"]),
          action("opening-hours-create", "Create opening hours", "POST", "/api/v1/{gymCode}/opening-hours", {
            weekday: 2,
            opensAt: "06:00:00",
            closesAt: "22:00:00",
          }, ["gymCode"]),
          action("opening-hours-update", "Update opening hours", "PUT", "/api/v1/{gymCode}/opening-hours/{openingHoursId}", {
            weekday: 2,
            opensAt: "07:00:00",
            closesAt: "21:00:00",
          }, ["gymCode", "openingHoursId"]),
          action("opening-hours-delete", "Delete opening hours", "DELETE", "/api/v1/{gymCode}/opening-hours/{openingHoursId}", undefined, ["gymCode", "openingHoursId"]),
          action("exceptions-list", "List opening exceptions", "GET", "/api/v1/{gymCode}/opening-hours-exceptions", undefined, ["gymCode"]),
          action("exceptions-create", "Create opening exception", "POST", "/api/v1/{gymCode}/opening-hours-exceptions", {
            exceptionDate: new Date(Date.now() + 172800000).toISOString().slice(0, 10),
            isClosed: true,
            opensAt: null,
            closesAt: null,
            reason: "Console closure",
          }, ["gymCode"]),
          action("exceptions-update", "Update opening exception", "PUT", "/api/v1/{gymCode}/opening-hours-exceptions/{exceptionId}", {
            exceptionDate: new Date(Date.now() + 172800000).toISOString().slice(0, 10),
            isClosed: false,
            opensAt: "10:00:00",
            closesAt: "18:00:00",
            reason: "Console partial day",
          }, ["gymCode", "exceptionId"]),
          action("exceptions-delete", "Delete opening exception", "DELETE", "/api/v1/{gymCode}/opening-hours-exceptions/{exceptionId}", undefined, ["gymCode", "exceptionId"]),
          action("settings-load", "Load gym settings", "GET", "/api/v1/{gymCode}/gym-settings", undefined, ["gymCode"]),
          action("settings-update", "Update gym settings", "PUT", "/api/v1/{gymCode}/gym-settings", {
            currencyCode: "EUR",
            timeZone: "Europe/Tallinn",
            allowNonMemberBookings: true,
            bookingCancellationHours: 6,
            publicDescription: "Updated from the React SaaS console.",
          }, ["gymCode"]),
          action("gym-users-list", "List gym users", "GET", "/api/v1/{gymCode}/gym-users", undefined, ["gymCode"]),
          action("gym-users-upsert", "Upsert gym user role", "POST", "/api/v1/{gymCode}/gym-users", {
            appUserId: defaults.appUserId,
            roleName: "GymAdmin",
            isActive: true,
          }, ["gymCode"]),
          action("gym-users-delete", "Delete gym user role", "DELETE", "/api/v1/{gymCode}/gym-users/{appUserId}/{roleName}", undefined, ["gymCode", "appUserId", "roleName"]),
        ],
      },
      {
        title: "Equipment and Maintenance",
        actions: [
          action("equipment-models-list", "List equipment models", "GET", "/api/v1/{gymCode}/equipment-models", undefined, ["gymCode"]),
          action("equipment-models-create", "Create equipment model", "POST", "/api/v1/{gymCode}/equipment-models", {
            name: "Demo Bike",
            type: 0,
            manufacturer: "Console",
            maintenanceIntervalDays: 90,
            description: "Created from the React SaaS console.",
          }, ["gymCode"]),
          action("equipment-models-update", "Update equipment model", "PUT", "/api/v1/{gymCode}/equipment-models/{equipmentModelId}", {
            name: "Updated Demo Bike",
            type: 0,
            manufacturer: "Console",
            maintenanceIntervalDays: 120,
            description: "Updated from the React SaaS console.",
          }, ["gymCode", "equipmentModelId"]),
          action("equipment-models-delete", "Delete equipment model", "DELETE", "/api/v1/{gymCode}/equipment-models/{equipmentModelId}", undefined, ["gymCode", "equipmentModelId"]),
          action("equipment-list", "List equipment", "GET", "/api/v1/{gymCode}/equipment", undefined, ["gymCode"]),
          action("equipment-create", "Create equipment", "POST", "/api/v1/{gymCode}/equipment", {
            equipmentModelId: defaults.equipmentModelId,
            assetTag: "EQ-CONSOLE",
            serialNumber: "CONSOLE-001",
            currentStatus: 0,
            commissionedAt: new Date().toISOString().slice(0, 10),
            decommissionedAt: null,
            notes: "Created from console",
          }, ["gymCode"]),
          action("equipment-update", "Update equipment", "PUT", "/api/v1/{gymCode}/equipment/{equipmentId}", {
            equipmentModelId: defaults.equipmentModelId,
            assetTag: "EQ-CONSOLE",
            serialNumber: "CONSOLE-001",
            currentStatus: 1,
            commissionedAt: new Date().toISOString().slice(0, 10),
            decommissionedAt: null,
            notes: "Updated from console",
          }, ["gymCode", "equipmentId"]),
          action("equipment-delete", "Delete equipment", "DELETE", "/api/v1/{gymCode}/equipment/{equipmentId}", undefined, ["gymCode", "equipmentId"]),
          action("maintenance-list", "List maintenance tasks", "GET", "/api/v1/{gymCode}/maintenance-tasks", undefined, ["gymCode"]),
          action("maintenance-create", "Create maintenance task", "POST", "/api/v1/{gymCode}/maintenance-tasks", {
            equipmentId: defaults.equipmentId,
            assignedStaffId: null,
            createdByStaffId: null,
            taskType: 0,
            priority: 1,
            status: 0,
            dueAtUtc: new Date(Date.now() + 86400000).toISOString(),
            notes: "Created from console",
          }, ["gymCode"]),
          action("maintenance-status", "Update maintenance status", "PUT", "/api/v1/{gymCode}/maintenance-tasks/{maintenanceTaskId}/status", {
            status: 1,
            notes: "Updated from console",
          }, ["gymCode", "maintenanceTaskId"]),
          action("maintenance-generate", "Generate due maintenance", "POST", "/api/v1/{gymCode}/maintenance-tasks/generate-due", {}, ["gymCode"]),
          action("maintenance-delete", "Delete maintenance task", "DELETE", "/api/v1/{gymCode}/maintenance-tasks/{maintenanceTaskId}", undefined, ["gymCode", "maintenanceTaskId"]),
        ],
      },
    );
  }

  return groups;
}

function action(
  key: string,
  title: string,
  method: HttpMethod,
  path: string,
  body?: unknown,
  params?: string[],
): ConsoleAction {
  return { body, key, method, params, path, title };
}

async function safeLoad<T>(factory: () => Promise<T>, fallback: T): Promise<T> {
  try {
    return await factory();
  } catch {
    return fallback;
  }
}

function planLabel(plan: number) {
  return ["Starter", "Growth", "Enterprise"][plan] ?? "Unknown";
}

function subscriptionStatusLabel(status: number) {
  return ["Trial", "Active", "Suspended", "Cancelled"][status] ?? "Unknown";
}
