import {
  apiFetch,
  clearSession,
  sessionFromLogin,
  storeSession,
} from "@/services/api/client";
import type { AuthSession, LoginResponse } from "@/types/api";

export const authService = {
  async login(email: string, password: string): Promise<AuthSession> {
    const res = await apiFetch<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
      auth: false,
    });
    const session = sessionFromLogin(res);
    storeSession(session);
    return session;
  },

  async bootstrapDemo(): Promise<AuthSession> {
    const res = await apiFetch<LoginResponse>("/api/bootstrap/demo", {
      method: "POST",
      auth: false,
    });
    const session = sessionFromLogin(res);
    storeSession(session);
    return session;
  },

  logout() {
    clearSession();
  },
};
