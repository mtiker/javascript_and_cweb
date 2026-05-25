import {
  ApiError,
  type AttendanceUpdateRequest,
  type AuthSession,
  type Booking,
  type BookingCreateRequest,
  type LoginRequest,
  type MemberDetail,
  type MemberSummary,
  type MemberUpsertRequest,
  type MemberWorkspace,
  type Membership,
  type MessageResponse,
  type Payment,
  type RegisterRequest,
  type Staff,
  type TrainingCategory,
  type TrainingCategoryUpsertRequest,
  type TrainingSession,
  type TrainingSessionUpsertRequest,
} from "./types";

interface ApiClientOptions {
  baseUrl: string;
  getSession: () => AuthSession | null;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
}

export class ApiClient {
  private readonly baseUrl: string;
  private readonly getSession: () => AuthSession | null;
  private readonly setSession: (session: AuthSession) => void;
  private readonly clearSession: () => void;
  private refreshInFlight: Promise<boolean> | null = null;

  constructor(options: ApiClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/+$/, "");
    this.getSession = options.getSession;
    this.setSession = options.setSession;
    this.clearSession = options.clearSession;
  }

  // ---- account ----
  login(request: LoginRequest): Promise<AuthSession> {
    return this.req<AuthSession>("/api/v1/account/login", { method: "POST", body: JSON.stringify(request) }, false);
  }
  register(request: RegisterRequest): Promise<AuthSession> {
    return this.req<AuthSession>("/api/v1/account/register", { method: "POST", body: JSON.stringify(request) }, false);
  }
  logout(): Promise<MessageResponse> {
    return this.req<MessageResponse>("/api/v1/account/logout", { method: "POST" });
  }
  switchGym(gymCode: string): Promise<AuthSession> {
    return this.req<AuthSession>("/api/v1/account/switch-gym", { method: "POST", body: JSON.stringify({ gymCode }) });
  }
  switchRole(roleName: string): Promise<AuthSession> {
    return this.req<AuthSession>("/api/v1/account/switch-role", { method: "POST", body: JSON.stringify({ roleName }) });
  }

  // ---- members ----
  getMembers(gym: string) {
    return this.req<MemberSummary[]>(`${this.t(gym)}/members`);
  }
  getMember(gym: string, id: string) {
    return this.req<MemberDetail>(`${this.t(gym)}/members/${encodeURIComponent(id)}`);
  }
  getCurrentMember(gym: string) {
    return this.req<MemberDetail>(`${this.t(gym)}/members/me`);
  }
  getMemberWorkspace(gym: string) {
    return this.req<MemberWorkspace>(`${this.t(gym)}/member-workspace/me`);
  }
  createMember(gym: string, body: MemberUpsertRequest) {
    return this.req<MemberDetail>(`${this.t(gym)}/members`, { method: "POST", body: JSON.stringify(body) });
  }
  updateMember(gym: string, id: string, body: MemberUpsertRequest) {
    return this.req<MemberDetail>(`${this.t(gym)}/members/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
  }
  deleteMember(gym: string, id: string) {
    return this.req<void>(`${this.t(gym)}/members/${encodeURIComponent(id)}`, { method: "DELETE" });
  }

  // ---- training categories ----
  getTrainingCategories(gym: string) {
    return this.req<TrainingCategory[]>(`${this.t(gym)}/training-categories`);
  }
  createTrainingCategory(gym: string, body: TrainingCategoryUpsertRequest) {
    return this.req<TrainingCategory>(`${this.t(gym)}/training-categories`, { method: "POST", body: JSON.stringify(body) });
  }
  updateTrainingCategory(gym: string, id: string, body: TrainingCategoryUpsertRequest) {
    return this.req<TrainingCategory>(`${this.t(gym)}/training-categories/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
  }
  deleteTrainingCategory(gym: string, id: string) {
    return this.req<void>(`${this.t(gym)}/training-categories/${encodeURIComponent(id)}`, { method: "DELETE" });
  }

  // ---- training sessions ----
  getTrainingSessions(gym: string) {
    return this.req<TrainingSession[]>(`${this.t(gym)}/training-sessions`);
  }
  getTrainingSession(gym: string, id: string) {
    return this.req<TrainingSession>(`${this.t(gym)}/training-sessions/${encodeURIComponent(id)}`);
  }
  createTrainingSession(gym: string, body: TrainingSessionUpsertRequest) {
    return this.req<TrainingSession>(`${this.t(gym)}/training-sessions`, { method: "POST", body: JSON.stringify(body) });
  }
  updateTrainingSession(gym: string, id: string, body: TrainingSessionUpsertRequest) {
    return this.req<TrainingSession>(`${this.t(gym)}/training-sessions/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
  }

  // ---- staff ----
  getStaff(gym: string) {
    return this.req<Staff[]>(`${this.t(gym)}/staff`);
  }

  // ---- bookings ----
  createBooking(gym: string, body: BookingCreateRequest) {
    return this.req<Booking>(`${this.t(gym)}/bookings`, { method: "POST", body: JSON.stringify(body) });
  }
  getBookings(gym: string) {
    return this.req<Booking[]>(`${this.t(gym)}/bookings`);
  }
  updateAttendance(gym: string, bookingId: string, body: AttendanceUpdateRequest) {
    return this.req<Booking>(`${this.t(gym)}/bookings/${encodeURIComponent(bookingId)}/attendance`, {
      method: "PUT",
      body: JSON.stringify(body),
    });
  }

  // ---- memberships / payments ----
  getMemberships(gym: string) {
    return this.req<Membership[]>(`${this.t(gym)}/memberships`);
  }
  getPayments(gym: string) {
    return this.req<Payment[]>(`${this.t(gym)}/payments`);
  }

  // ---- internals ----
  private t(gym: string) {
    return `/api/v1/${encodeURIComponent(gym)}`;
  }

  private async req<T>(
    path: string,
    init: RequestInit = {},
    requiresAuth = true,
    retry = true,
  ): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set("Accept", "application/json");
    if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");

    const session = this.getSession();
    if (requiresAuth && session?.jwt) headers.set("Authorization", `Bearer ${session.jwt}`);

    const res = await fetch(`${this.baseUrl}${path}`, { ...init, headers });

    if (res.status === 401 && requiresAuth && retry && session) {
      const ok = await this.refresh();
      if (ok) return this.req<T>(path, init, requiresAuth, false);
      throw new ApiError("Session expired. Please sign in again.", 401);
    }

    if (!res.ok) throw await ApiError.fromResponse(res);
    if (res.status === 204) return undefined as T;
    const text = await res.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
  }

  private async refresh(): Promise<boolean> {
    if (this.refreshInFlight) return this.refreshInFlight;
    const session = this.getSession();
    if (!session) return false;

    this.refreshInFlight = (async () => {
      const res = await fetch(`${this.baseUrl}/api/v1/account/renew-refresh-token`, {
        method: "POST",
        headers: { Accept: "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ jwt: session.jwt, refreshToken: session.refreshToken }),
      });
      if (!res.ok) {
        this.clearSession();
        return false;
      }
      const next = (await res.json()) as AuthSession;
      this.setSession(next);
      return true;
    })();

    try {
      return await this.refreshInFlight;
    } finally {
      this.refreshInFlight = null;
    }
  }
}

export function resolveApiBaseUrl(): string {
  const explicit = import.meta.env.VITE_API_BASE_URL as string | undefined;
  if (explicit) return explicit;
  return "https://mtiker-cweb-4.proxy.itcollege.ee";
}
