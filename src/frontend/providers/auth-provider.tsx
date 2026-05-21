"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { authService } from "@/services/api/auth.service";
import { clearSession, getStoredSession } from "@/services/api/client";
import type { AuthSession } from "@/types/api";

type AuthContextValue = {
  session: AuthSession | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  bootstrapDemo: () => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [session, setSession] = useState<AuthSession | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setSession(getStoredSession());
    setLoading(false);

    const interval = setInterval(() => {
      const current = getStoredSession();
      setSession(current);
      if (!current && window.location.pathname !== "/login") {
        router.replace("/login");
      }
    }, 60_000);

    return () => clearInterval(interval);
  }, [router]);

  const login = useCallback(async (email: string, password: string) => {
    const s = await authService.login(email, password);
    setSession(s);
    router.replace("/");
  }, [router]);

  const bootstrapDemo = useCallback(async () => {
    const s = await authService.bootstrapDemo();
    setSession(s);
    router.replace("/");
  }, [router]);

  const logout = useCallback(() => {
    authService.logout();
    setSession(null);
    router.replace("/login");
  }, [router]);

  const value = useMemo(
    () => ({ session, loading, login, bootstrapDemo, logout }),
    [session, loading, login, bootstrapDemo, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth deve ser usado dentro de AuthProvider");
  return ctx;
}

export function useRequireAuth() {
  const { session, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !session) router.replace("/login");
  }, [loading, session, router]);

  return { session, loading };
}
