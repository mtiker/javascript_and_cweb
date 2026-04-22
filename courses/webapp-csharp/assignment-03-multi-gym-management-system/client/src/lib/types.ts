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

export type HttpMethod = "GET" | "POST" | "PUT" | "DELETE";

export interface RawApiResponse {
  status: number;
  data: unknown;
}

export enum SubscriptionPlan {
  Starter = 0,
  Growth = 1,
  Enterprise = 2,
}

export enum SubscriptionStatus {
  Trial = 0,
  Active = 1,
  Suspended = 2,
  Cancelled = 3,
}

export enum SupportTicketPriority {
  Low = 0,
  Medium = 1,
  High = 2,
}

export enum SupportTicketStatus {
  Open = 0,
  InProgress = 1,
  Resolved = 2,
}

export interface PlatformAnalytics {
  gymCount: number;
  userCount: number;
  memberCount: number;
  openSupportTicketCount: number;
}

export interface GymSummary {
  gymId: string;
  name: string;
  code: string;
  isActive: boolean;
  city: string;
  country: string;
}

export interface GymSnapshot {
  gymId: string;
  gymName: string;
  memberCount: number;
  sessionCount: number;
  openMaintenanceTaskCount: number;
}

export interface SubscriptionSummary {
  gymId: string;
  gymName: string;
  plan: SubscriptionPlan;
  status: SubscriptionStatus;
  monthlyPrice: number;
  startDate: string;
  endDate?: string | null;
}

export interface SupportTicket {
  ticketId: string;
  gymId: string;
  gymName: string;
  title: string;
  status: SupportTicketStatus;
  priority: SupportTicketPriority;
  createdAtUtc: string;
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
  equipmentAssetTag?: string | null;
  equipmentName: string;
  assignedStaffId?: string | null;
  assignedStaffName?: string | null;
  createdByStaffId?: string | null;
  taskType: MaintenanceTaskType;
  priority: MaintenancePriority;
  status: MaintenanceTaskStatus;
  dueAtUtc?: string | null;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  notes?: string | null;
}

export interface MaintenanceTaskUpsertRequest {
  equipmentId: string;
  assignedStaffId?: string | null;
  createdByStaffId?: string | null;
  taskType: MaintenanceTaskType;
  priority: MaintenancePriority;
  status: MaintenanceTaskStatus;
  dueAtUtc?: string | null;
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

export interface Membership {
  id: string;
  memberId: string;
  membershipPackageId: string;
  startDate: string;
  endDate: string;
  priceAtPurchase: number;
  currencyCode: string;
  status: number;
}

export interface Payment {
  id: string;
  amount: number;
  currencyCode: string;
  paidAtUtc: string;
  status: number;
  reference?: string | null;
  membershipId?: string | null;
  bookingId?: string | null;
}

export interface OpeningHours {
  id: string;
  weekday: number;
  opensAt: string;
  closesAt: string;
}

export interface OpeningHoursException {
  id: string;
  exceptionDate: string;
  isClosed: boolean;
  opensAt?: string | null;
  closesAt?: string | null;
  reason?: string | null;
}

export enum EquipmentType {
  Cardio = 0,
  Strength = 1,
  Mobility = 2,
  Accessory = 3,
  Other = 4,
}

export enum EquipmentStatus {
  Active = 0,
  Maintenance = 1,
  Broken = 2,
  Decommissioned = 3,
}

export interface EquipmentModel {
  id: string;
  name: string;
  type: EquipmentType;
  manufacturer?: string | null;
  maintenanceIntervalDays: number;
  description?: string | null;
}

export interface Equipment {
  id: string;
  equipmentModelId: string;
  assetTag?: string | null;
  serialNumber?: string | null;
  currentStatus: EquipmentStatus;
  commissionedAt?: string | null;
  decommissionedAt?: string | null;
  notes?: string | null;
}

export interface GymSettings {
  gymId: string;
  currencyCode: string;
  timeZone: string;
  allowNonMemberBookings: boolean;
  bookingCancellationHours: number;
  publicDescription?: string | null;
}

export interface GymUser {
  appUserId: string;
  email: string;
  roleName: string;
  isActive: boolean;
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
