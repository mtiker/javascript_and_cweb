export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface AuthResponseDto {
  token?: string | null;
  refreshToken?: string | null;
  firstName?: string | null;
  lastName?: string | null;
}

export interface LoginInput {
  email: string;
  password: string;
}

export interface RegisterInput extends LoginInput {
  firstName: string;
  lastName: string;
}

export interface CurrentUser {
  userId: string | null;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
}
