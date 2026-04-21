export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthSession {
  jwt: string;
  refreshToken: string;
  expiresInSeconds: number;
  activeGymId?: string | null;
  activeGymCode?: string | null;
  activeRole?: string | null;
  systemRoles: string[];
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
}

export interface TrainingCategory {
  id: string;
  name: string;
  description?: string | null;
}

export interface TrainingCategoryUpsertRequest {
  name: string;
  description?: string | null;
}

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
  trainerContractIds: string[];
}

export enum BookingStatus {
  Booked = 0,
  Cancelled = 1,
  Attended = 2,
  NoShow = 3,
}

export interface Booking {
  id: string;
  trainingSessionId: string;
  memberId: string;
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

export enum MaintenanceTaskType {
  Scheduled = 0,
  Breakdown = 1,
}

export enum MaintenancePriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export enum MaintenanceTaskStatus {
  Open = 0,
  InProgress = 1,
  Done = 2,
}

export interface MaintenanceTask {
  id: string;
  equipmentId: string;
  assignedStaffId?: string | null;
  createdByStaffId?: string | null;
  taskType: MaintenanceTaskType;
  priority: MaintenancePriority;
  status: MaintenanceTaskStatus;
  dueAtUtc?: string | null;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  notes?: string | null;
}

export interface MaintenanceStatusUpdateRequest {
  status: MaintenanceTaskStatus;
  notes?: string | null;
}

export enum MembershipPackageType {
  Single = 0,
  Monthly = 1,
  Yearly = 2,
  Custom = 3,
}

export enum DurationUnit {
  Day = 0,
  Month = 1,
  Year = 2,
}

export interface MembershipPackage {
  id: string;
  name: string;
  packageType: MembershipPackageType;
  durationValue: number;
  durationUnit: DurationUnit;
  basePrice: number;
  currencyCode: string;
  trainingDiscountPercent?: number | null;
  isTrainingFree: boolean;
  description?: string | null;
}

export interface MembershipPackageUpsertRequest {
  name: string;
  packageType: MembershipPackageType;
  durationValue: number;
  durationUnit: DurationUnit;
  basePrice: number;
  currencyCode: string;
  trainingDiscountPercent?: number | null;
  isTrainingFree: boolean;
  description?: string | null;
}

export interface Notice {
  tone: "success" | "error" | "info";
  title: string;
  messages?: string[];
}

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

  if (error instanceof Error) {
    return [error.message];
  }

  return ["Unexpected error."];
}

function flattenProblemErrors(errors: unknown): string[] {
  if (Array.isArray(errors)) {
    return errors.map((value) => String(value));
  }

  if (errors && typeof errors === "object") {
    return Object.values(errors as Record<string, unknown>)
      .flatMap((value) => (Array.isArray(value) ? value : [value]))
      .map((value) => String(value));
  }

  return [];
}
