import type {
  AuthSession,
  AttendanceUpdateRequest,
  Booking,
  BookingCreateRequest,
  CoachingPlan,
  CoachingPlanCreateRequest,
  CoachingPlanItemDecisionRequest,
  CoachingPlanStatusUpdateRequest,
  CoachingPlanUpdateRequest,
  Equipment,
  EquipmentModel,
  FinanceWorkspace,
  GymSettings,
  GymSnapshot,
  GymSummary,
  GymUser,
  HttpMethod,
  Invoice,
  InvoiceCreateRequest,
  InvoicePaymentRequest,
  LoginRequest,
  MaintenanceAssignmentUpdateRequest,
  MaintenanceTaskAssignmentHistory,
  MaintenanceStatusUpdateRequest,
  MaintenanceTask,
  MaintenanceTaskUpsertRequest,
  MemberDetail,
  MemberWorkspace,
  MemberSummary,
  MemberUpsertRequest,
  Membership,
  MembershipPackage,
  MembershipPackageUpsertRequest,
  MessageResponse,
  OpeningHours,
  OpeningHoursException,
  Payment,
  PlatformAnalytics,
  RawApiResponse,
  Staff,
  SubscriptionSummary,
  SupportTicket,
  TrainingCategory,
  TrainingCategoryUpsertRequest,
  TrainingSession,
  TrainingSessionUpsertRequest,
} from "./types";
import { ApiError } from "./types";
import { getCurrentLanguage } from "./language";

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

  async login(request: LoginRequest): Promise<AuthSession> {
    return this.request<AuthSession>(
      "/api/v1/account/login",
      {
        method: "POST",
        body: JSON.stringify(request),
      },
      false,
    );
  }

  async logout(): Promise<MessageResponse> {
    return this.request<MessageResponse>("/api/v1/account/logout", {
      method: "POST",
    });
  }

  async switchGym(gymCode: string): Promise<AuthSession> {
    return this.request<AuthSession>("/api/v1/account/switch-gym", {
      method: "POST",
      body: JSON.stringify({ gymCode }),
    });
  }

  async switchRole(roleName: string): Promise<AuthSession> {
    return this.request<AuthSession>("/api/v1/account/switch-role", {
      method: "POST",
      body: JSON.stringify({ roleName }),
    });
  }

  async getPlatformAnalytics(): Promise<PlatformAnalytics> {
    return this.request<PlatformAnalytics>("/api/v1/system/platform/analytics");
  }

  async getGyms(): Promise<GymSummary[]> {
    return this.request<GymSummary[]>("/api/v1/system/gyms");
  }

  async getGymSnapshot(gymId: string): Promise<GymSnapshot> {
    return this.request<GymSnapshot>(`/api/v1/system/gyms/${encodeURIComponent(gymId)}/snapshot`);
  }

  async getSubscriptions(): Promise<SubscriptionSummary[]> {
    return this.request<SubscriptionSummary[]>("/api/v1/system/subscriptions");
  }

  async getSupportTickets(): Promise<SupportTicket[]> {
    return this.request<SupportTicket[]>("/api/v1/system/support");
  }

  async getMembers(gymCode: string): Promise<MemberSummary[]> {
    return this.request<MemberSummary[]>(`${this.tenantBase(gymCode)}/members`);
  }

  async getMember(gymCode: string, memberId: string): Promise<MemberDetail> {
    return this.request<MemberDetail>(`${this.tenantBase(gymCode)}/members/${memberId}`);
  }

  async getCurrentMember(gymCode: string): Promise<MemberDetail> {
    return this.request<MemberDetail>(`${this.tenantBase(gymCode)}/members/me`);
  }

  async getMemberWorkspace(gymCode: string): Promise<MemberWorkspace> {
    return this.request<MemberWorkspace>(`${this.tenantBase(gymCode)}/member-workspace/me`);
  }

  async getMemberWorkspaceForMember(gymCode: string, memberId: string): Promise<MemberWorkspace> {
    return this.request<MemberWorkspace>(`${this.tenantBase(gymCode)}/member-workspace/members/${memberId}`);
  }

  async createMember(gymCode: string, request: MemberUpsertRequest): Promise<MemberDetail> {
    return this.request<MemberDetail>(`${this.tenantBase(gymCode)}/members`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async updateMember(gymCode: string, memberId: string, request: MemberUpsertRequest): Promise<MemberDetail> {
    return this.request<MemberDetail>(`${this.tenantBase(gymCode)}/members/${memberId}`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async deleteMember(gymCode: string, memberId: string): Promise<void> {
    return this.request<void>(`${this.tenantBase(gymCode)}/members/${memberId}`, {
      method: "DELETE",
    });
  }

  async getTrainingCategories(gymCode: string): Promise<TrainingCategory[]> {
    return this.request<TrainingCategory[]>(`${this.tenantBase(gymCode)}/training-categories`);
  }

  async createTrainingCategory(gymCode: string, request: TrainingCategoryUpsertRequest): Promise<TrainingCategory> {
    return this.request<TrainingCategory>(`${this.tenantBase(gymCode)}/training-categories`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async updateTrainingCategory(gymCode: string, categoryId: string, request: TrainingCategoryUpsertRequest): Promise<TrainingCategory> {
    return this.request<TrainingCategory>(`${this.tenantBase(gymCode)}/training-categories/${categoryId}`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async deleteTrainingCategory(gymCode: string, categoryId: string): Promise<void> {
    return this.request<void>(`${this.tenantBase(gymCode)}/training-categories/${categoryId}`, {
      method: "DELETE",
    });
  }

  async getTrainingSessions(gymCode: string): Promise<TrainingSession[]> {
    return this.request<TrainingSession[]>(`${this.tenantBase(gymCode)}/training-sessions`);
  }

  async getTrainingSession(gymCode: string, sessionId: string): Promise<TrainingSession> {
    return this.request<TrainingSession>(`${this.tenantBase(gymCode)}/training-sessions/${sessionId}`);
  }

  async createTrainingSession(gymCode: string, request: TrainingSessionUpsertRequest): Promise<TrainingSession> {
    return this.request<TrainingSession>(`${this.tenantBase(gymCode)}/training-sessions`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async updateTrainingSession(gymCode: string, sessionId: string, request: TrainingSessionUpsertRequest): Promise<TrainingSession> {
    return this.request<TrainingSession>(`${this.tenantBase(gymCode)}/training-sessions/${sessionId}`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async getStaff(gymCode: string): Promise<Staff[]> {
    return this.request<Staff[]>(`${this.tenantBase(gymCode)}/staff`);
  }

  async createBooking(gymCode: string, request: BookingCreateRequest): Promise<Booking> {
    return this.request<Booking>(`${this.tenantBase(gymCode)}/bookings`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async getBookings(gymCode: string): Promise<Booking[]> {
    return this.request<Booking[]>(`${this.tenantBase(gymCode)}/bookings`);
  }

  async updateAttendance(gymCode: string, bookingId: string, request: AttendanceUpdateRequest): Promise<Booking> {
    return this.request<Booking>(`${this.tenantBase(gymCode)}/bookings/${bookingId}/attendance`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async getCoachingPlans(gymCode: string, memberId?: string): Promise<CoachingPlan[]> {
    const path = memberId
      ? `${this.tenantBase(gymCode)}/coaching-plans?memberId=${encodeURIComponent(memberId)}`
      : `${this.tenantBase(gymCode)}/coaching-plans`;
    return this.request<CoachingPlan[]>(path);
  }

  async createCoachingPlan(gymCode: string, request: CoachingPlanCreateRequest): Promise<CoachingPlan> {
    return this.request<CoachingPlan>(`${this.tenantBase(gymCode)}/coaching-plans`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async updateCoachingPlan(gymCode: string, planId: string, request: CoachingPlanUpdateRequest): Promise<CoachingPlan> {
    return this.request<CoachingPlan>(`${this.tenantBase(gymCode)}/coaching-plans/${planId}`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async updateCoachingPlanStatus(gymCode: string, planId: string, request: CoachingPlanStatusUpdateRequest): Promise<CoachingPlan> {
    return this.request<CoachingPlan>(`${this.tenantBase(gymCode)}/coaching-plans/${planId}/status`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async decideCoachingPlanItem(
    gymCode: string,
    planId: string,
    itemId: string,
    request: CoachingPlanItemDecisionRequest,
  ): Promise<CoachingPlan> {
    return this.request<CoachingPlan>(`${this.tenantBase(gymCode)}/coaching-plans/${planId}/items/${itemId}/decision`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async deleteCoachingPlan(gymCode: string, planId: string): Promise<void> {
    return this.request<void>(`${this.tenantBase(gymCode)}/coaching-plans/${planId}`, {
      method: "DELETE",
    });
  }

  async getMaintenanceTasks(gymCode: string): Promise<MaintenanceTask[]> {
    return this.request<MaintenanceTask[]>(`${this.tenantBase(gymCode)}/maintenance-tasks`);
  }

  async createMaintenanceTask(gymCode: string, request: MaintenanceTaskUpsertRequest): Promise<MaintenanceTask> {
    return this.request<MaintenanceTask>(`${this.tenantBase(gymCode)}/maintenance-tasks`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async updateMaintenanceTaskStatus(
    gymCode: string,
    taskId: string,
    request: MaintenanceStatusUpdateRequest,
  ): Promise<MaintenanceTask> {
    return this.request<MaintenanceTask>(`${this.tenantBase(gymCode)}/maintenance-tasks/${taskId}/status`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async updateMaintenanceTaskAssignment(
    gymCode: string,
    taskId: string,
    request: MaintenanceAssignmentUpdateRequest,
  ): Promise<MaintenanceTask> {
    return this.request<MaintenanceTask>(`${this.tenantBase(gymCode)}/maintenance-tasks/${taskId}/assignment`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async getMaintenanceTaskAssignmentHistory(gymCode: string, taskId: string): Promise<MaintenanceTaskAssignmentHistory[]> {
    return this.request<MaintenanceTaskAssignmentHistory[]>(`${this.tenantBase(gymCode)}/maintenance-tasks/${taskId}/assignment-history`);
  }

  async generateDueMaintenanceTasks(gymCode: string): Promise<MessageResponse> {
    return this.request<MessageResponse>(`${this.tenantBase(gymCode)}/maintenance-tasks/generate-due`, {
      method: "POST",
    });
  }

  async deleteMaintenanceTask(gymCode: string, taskId: string): Promise<void> {
    return this.request<void>(`${this.tenantBase(gymCode)}/maintenance-tasks/${taskId}`, {
      method: "DELETE",
    });
  }

  async getMembershipPackages(gymCode: string): Promise<MembershipPackage[]> {
    return this.request<MembershipPackage[]>(`${this.tenantBase(gymCode)}/membership-packages`);
  }

  async createMembershipPackage(gymCode: string, request: MembershipPackageUpsertRequest): Promise<MembershipPackage> {
    return this.request<MembershipPackage>(`${this.tenantBase(gymCode)}/membership-packages`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async updateMembershipPackage(gymCode: string, packageId: string, request: MembershipPackageUpsertRequest): Promise<MembershipPackage> {
    return this.request<MembershipPackage>(`${this.tenantBase(gymCode)}/membership-packages/${packageId}`, {
      method: "PUT",
      body: JSON.stringify(request),
    });
  }

  async deleteMembershipPackage(gymCode: string, packageId: string): Promise<void> {
    return this.request<void>(`${this.tenantBase(gymCode)}/membership-packages/${packageId}`, {
      method: "DELETE",
    });
  }

  async getMemberships(gymCode: string): Promise<Membership[]> {
    return this.request<Membership[]>(`${this.tenantBase(gymCode)}/memberships`);
  }

  async getPayments(gymCode: string): Promise<Payment[]> {
    return this.request<Payment[]>(`${this.tenantBase(gymCode)}/payments`);
  }

  async getFinanceWorkspace(gymCode: string): Promise<FinanceWorkspace> {
    return this.request<FinanceWorkspace>(`${this.tenantBase(gymCode)}/finance-workspace/me`);
  }

  async getFinanceWorkspaceForMember(gymCode: string, memberId: string): Promise<FinanceWorkspace> {
    return this.request<FinanceWorkspace>(`${this.tenantBase(gymCode)}/finance-workspace/members/${memberId}`);
  }

  async getInvoices(gymCode: string, memberId?: string): Promise<Invoice[]> {
    const path = memberId ? `${this.tenantBase(gymCode)}/invoices?memberId=${encodeURIComponent(memberId)}` : `${this.tenantBase(gymCode)}/invoices`;
    return this.request<Invoice[]>(path);
  }

  async getInvoice(gymCode: string, invoiceId: string): Promise<Invoice> {
    return this.request<Invoice>(`${this.tenantBase(gymCode)}/invoices/${invoiceId}`);
  }

  async createInvoice(gymCode: string, request: InvoiceCreateRequest): Promise<Invoice> {
    return this.request<Invoice>(`${this.tenantBase(gymCode)}/invoices`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async addInvoicePayment(gymCode: string, invoiceId: string, request: InvoicePaymentRequest): Promise<Invoice> {
    return this.request<Invoice>(`${this.tenantBase(gymCode)}/invoices/${invoiceId}/payments`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async addInvoiceRefund(gymCode: string, invoiceId: string, request: InvoicePaymentRequest): Promise<Invoice> {
    return this.request<Invoice>(`${this.tenantBase(gymCode)}/invoices/${invoiceId}/refunds`, {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async getOpeningHours(gymCode: string): Promise<OpeningHours[]> {
    return this.request<OpeningHours[]>(`${this.tenantBase(gymCode)}/opening-hours`);
  }

  async getOpeningHoursExceptions(gymCode: string): Promise<OpeningHoursException[]> {
    return this.request<OpeningHoursException[]>(`${this.tenantBase(gymCode)}/opening-hours-exceptions`);
  }

  async getEquipmentModels(gymCode: string): Promise<EquipmentModel[]> {
    return this.request<EquipmentModel[]>(`${this.tenantBase(gymCode)}/equipment-models`);
  }

  async getEquipment(gymCode: string): Promise<Equipment[]> {
    return this.request<Equipment[]>(`${this.tenantBase(gymCode)}/equipment`);
  }

  async getGymSettings(gymCode: string): Promise<GymSettings> {
    return this.request<GymSettings>(`${this.tenantBase(gymCode)}/gym-settings`);
  }

  async getGymUsers(gymCode: string): Promise<GymUser[]> {
    return this.request<GymUser[]>(`${this.tenantBase(gymCode)}/gym-users`);
  }

  async sendRaw(path: string, method: HttpMethod, bodyText = ""): Promise<RawApiResponse> {
    const parsedBody = bodyText.trim() ? JSON.parse(bodyText) : undefined;
    const data = await this.request<unknown>(path, {
      method,
      body: parsedBody === undefined ? undefined : JSON.stringify(parsedBody),
    });

    return {
      status: 200,
      data,
    };
  }

  private tenantBase(gymCode: string): string {
    return `/api/v1/${encodeURIComponent(gymCode)}`;
  }

  private async request<T>(
    path: string,
    init: RequestInit = {},
    requiresAuth = true,
    retryOnUnauthorized = true,
  ): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set("Accept", "application/json");
    headers.set("Accept-Language", getCurrentLanguage());

    if (init.body && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }

    const currentSession = this.getSession();
    if (requiresAuth && currentSession?.jwt) {
      headers.set("Authorization", `Bearer ${currentSession.jwt}`);
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers,
    });

    if (response.status === 401 && requiresAuth && retryOnUnauthorized && currentSession) {
      const refreshed = await this.refreshSession();
      if (refreshed) {
        return this.request<T>(path, init, requiresAuth, false);
      }

      throw new ApiError("Session expired. Please sign in again.", 401);
    }

    if (!response.ok) {
      throw await ApiError.fromResponse(response);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const responseText = await response.text();
    return responseText ? (JSON.parse(responseText) as T) : (undefined as T);
  }

  private async refreshSession(): Promise<boolean> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    const currentSession = this.getSession();
    if (!currentSession) {
      return false;
    }

    this.refreshInFlight = (async () => {
      const response = await fetch(`${this.baseUrl}/api/v1/account/renew-refresh-token`, {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          jwt: currentSession.jwt,
          refreshToken: currentSession.refreshToken,
        }),
      });

      if (!response.ok) {
        this.clearSession();
        return false;
      }

      const nextSession = (await response.json()) as AuthSession;
      this.setSession(nextSession);
      return true;
    })();

    try {
      return await this.refreshInFlight;
    } finally {
      this.refreshInFlight = null;
    }
  }
}
