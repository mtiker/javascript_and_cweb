import axios from "axios";
import type { IJWTResponse, ILoginData, IRegisterData } from "@/domain";
import { getErrorMessage } from "@/utils/errorUtils";
import { BASE_URL } from "./config";

// Plain axios instance — login/register endpoints do not require a Bearer
// token, so the auth interceptors in apiClient would only add noise here.
const authHttp = axios.create({
  baseURL: BASE_URL,
  headers: { "Content-Type": "application/json" },
});

export const AccountService = {
  login: async (data: ILoginData): Promise<IJWTResponse> => {
    try {
      const { data: response } = await authHttp.post<IJWTResponse>(
        "/api/v1/Account/Login",
        data,
      );
      return response;
    } catch (err) {
      throw new Error(getErrorMessage(err, "Login failed"));
    }
  },

  register: async (data: IRegisterData): Promise<IJWTResponse> => {
    try {
      const { data: response } = await authHttp.post<IJWTResponse>(
        "/api/v1/Account/Register",
        data,
      );
      return response;
    } catch (err) {
      throw new Error(getErrorMessage(err, "Registration failed"));
    }
  },
};
