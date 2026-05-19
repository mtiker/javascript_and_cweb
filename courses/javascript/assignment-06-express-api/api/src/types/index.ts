// POST /api/v1/Account/Login + Register request body
export interface ILoginData {
  email: string;
  password: string;
}

export interface IRegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

// Response from Login / Register / RefreshToken
// JWT field is called "token", NOT "jwt"
export interface IJwtResponse {
  token: string;
  refreshToken: string;
  firstName: string;
  lastName: string;
}

// Request body for POST /api/v1/Account/RefreshToken
// Field is called "jwt" on the REQUEST side
export interface IRefreshTokenModel {
  jwt: string;
  refreshToken: string;
}

// Error response shape
export interface IApiMessage {
  messages: string[] | null;
}

// TodoCategory
export interface ITodoCategory {
  id: string;
  categoryName: string | null;
  categorySort: number;
  syncDt: string;
  tag: string | null;
}

export interface ICreateCategoryDTO {
  categoryName: string | null;
  categorySort: number;
  tag?: string | null;
}

// TodoPriority
export interface ITodoPriority {
  id: string;
  priorityName: string | null;
  prioritySort: number;
  syncDt: string;
}

export interface ICreatePriorityDTO {
  priorityName: string | null;
  prioritySort: number;
  syncDt: string;
}

// TodoTask
export interface ITodoTask {
  id: string;
  taskName: string | null;
  taskSort: number;
  createdDt: string;
  dueDt: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: string;
  todoPriorityId: string;
  syncDt: string;
}

export interface ICreateTaskDTO {
  taskName: string | null;
  taskSort: number;
  createdDt: string;
  dueDt?: string | null;
  isCompleted: boolean;
  isArchived: boolean;
  todoCategoryId: string;
  todoPriorityId: string;
}

declare global {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace Express {
    interface Request {
      user?: { userId: string; email: string };
    }
  }
}
