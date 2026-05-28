// API types — mirrored from cweb backend (assignment05_final2 Shared.Contracts/Dtos/v1)

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface AuthSession {
  jwt: string;
  refreshToken: string;
  expiresInSeconds: number;
  activeGymId?: string | null;
  activeGymCode?: string | null;
  activeRole?: string | null;
  systemRoles: string[];
  availableTenants?: TenantAccess[];
}

export interface TenantAccess {
  gymId: string;
  gymCode: string;
  gymName: string;
  roles: string[];
}

export interface MessageResponse {
  messages: string[];
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: unknown;
}

// Members
export enum MemberStatus {
  Active = 0,
  Suspended = 1,
  Left = 2,
}

export interface MemberSummary {
  id: string;
  memberCode: string;
  fullName: string;
  status: MemberStatus;
}

export interface MemberDetail extends MemberSummary {
  firstName: string;
  lastName: string;
  personalCode?: string | null;
  dateOfBirth?: string | null;
}

export interface MemberUpsertRequest {
  firstName: string;
  lastName: string;
  personalCode?: string | null;
  dateOfBirth?: string | null;
  memberCode: string;
  status: MemberStatus;
  email?: string | null;
  password?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface MembershipStatusUpdateRequest {
  status: MembershipStatus;
  reason?: string | null;
}

// Training categories
export interface TrainingCategory {
  id: string;
  name: string;
  description?: string | null;
}

export interface TrainingCategoryUpsertRequest {
  name: string;
  description?: string | null;
}

// Training sessions
export enum TrainingSessionStatus {
  Draft = 0,
  Published = 1,
  Cancelled = 2,
  Completed = 3,
}

export interface TrainingSession {
  id: string;
  categoryId: string;
  name: string;
  description?: string | null;
  startAtUtc: string;
  endAtUtc: string;
  capacity: number;
  basePrice: number;
  currencyCode: string;
  status: TrainingSessionStatus;
  trainerStaffId?: string | null;
  trainerName?: string | null;
}

export interface TrainingSessionUpsertRequest {
  categoryId: string;
  name: string;
  description?: string | null;
  startAtUtc: string;
  endAtUtc: string;
  capacity: number;
  basePrice: number;
  currencyCode: string;
  status: TrainingSessionStatus;
  trainerStaffId?: string | null;
}

// Bookings
export enum BookingStatus {
  Booked = 0,
  Cancelled = 1,
  Attended = 2,
  NoShow = 3,
}

export interface Booking {
  id: string;
  trainingSessionId: string;
  trainingSessionName: string;
  memberId: string;
  memberName: string;
  memberCode: string;
  status: BookingStatus;
  chargedPrice: number;
  paymentRequired: boolean;
}

export interface BookingCreateRequest {
  trainingSessionId: string;
  memberId: string;
  paymentReference?: string | null;
}

export interface AttendanceUpdateRequest {
  status: BookingStatus;
}

// Memberships / payments
export enum MembershipStatus {
  Pending = 0,
  Active = 1,
  Expired = 2,
  Cancelled = 3,
  Paused = 4,
  Refunded = 5,
  Renewed = 6,
}

export enum PaymentStatus {
  Pending = 0,
  Completed = 1,
  Failed = 2,
  Refunded = 3,
}

export interface Membership {
  id: string;
  memberId: string;
  membershipPackageId: string;
  startDate: string;
  endDate: string;
  priceAtPurchase: number;
  currencyCode: string;
  status: MembershipStatus;
}

export interface Payment {
  id: string;
  amount: number;
  currencyCode: string;
  paidAtUtc: string;
  status: PaymentStatus;
  reference?: string | null;
  membershipId?: string | null;
  bookingId?: string | null;
}

export interface MemberWorkspaceBooking {
  bookingId: string;
  trainingSessionId: string;
  trainingSessionName: string;
  startAtUtc: string;
  endAtUtc: string;
  status: BookingStatus;
  chargedPrice: number;
  currencyCode: string;
  paymentRequired: boolean;
}

export interface MemberWorkspace {
  profile: MemberDetail;
  memberships: Membership[];
  payments: Payment[];
  bookings: MemberWorkspaceBooking[];
  attendedSessionCount: number;
  upcomingBookingCount: number;
  outstandingBalance: number;
  outstandingActions: { code: string; title: string; detail: string }[];
}

// Staff
export enum StaffStatus {
  Active = 0,
  Suspended = 1,
  Inactive = 2,
}
export interface Staff {
  id: string;
  staffCode: string;
  fullName: string;
  status: StaffStatus;
}

// Errors
export class ApiError extends Error {
  status: number;
  details: string[];

  constructor(message: string, status: number, details: string[] = []) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      const problem = (await response.json().catch(() => null)) as ApiProblem | null;
      const details = flattenProblemErrors(problem?.errors);
      const message = problem?.detail || problem?.title || response.statusText || "Request failed.";
      return new ApiError(message, response.status, details);
    }
    const text = await response.text().catch(() => "");
    return new ApiError(text || response.statusText || "Request failed.", response.status);
  }
}

export function getErrorMessages(error: unknown): string[] {
  if (error instanceof ApiError) {
    return error.details.length > 0 ? error.details : [error.message];
  }
  if (error instanceof Error) return [error.message];
  return ["Unexpected error."];
}

function flattenProblemErrors(errors: unknown): string[] {
  if (Array.isArray(errors)) return errors.map((v) => String(v));
  if (errors && typeof errors === "object") {
    return Object.values(errors as Record<string, unknown>)
      .flatMap((v) => (Array.isArray(v) ? v : [v]))
      .map((v) => String(v));
  }
  return [];
}
