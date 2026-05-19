// POST /api/v1/Account/Login | /api/v1/Account/Register | /api/v1/Account/RefreshToken
export interface IJWTResponse {
  readonly token?: string | null;
  readonly refreshToken?: string | null;
  readonly firstName?: string | null;
  readonly lastName?: string | null;
}
