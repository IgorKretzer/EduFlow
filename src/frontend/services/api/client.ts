import type { AuthSession } from "@/types/api";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";
const SESSION_KEY = "eduflow_session";

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public body?: string
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export function getApiBaseUrl() {
  return API_URL;
}

export function getStoredSession(): AuthSession | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as AuthSession;
    if (new Date(session.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(SESSION_KEY);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

export function storeSession(session: AuthSession) {
  localStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function clearSession() {
  localStorage.removeItem(SESSION_KEY);
}

function parseJwtEmail(token: string): string | undefined {
  try {
    const payload = token.split(".")[1];
    const json = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/")));
    return json.email ?? json["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"];
  } catch {
    return undefined;
  }
}

export function sessionFromLogin(res: {
  accessToken: string;
  expiresAt: string;
  tenantId: string;
  tenantSlug: string;
}): AuthSession {
  return {
    accessToken: res.accessToken,
    expiresAt: res.expiresAt,
    tenantId: res.tenantId,
    tenantSlug: res.tenantSlug,
    email: parseJwtEmail(res.accessToken),
  };
}

type FetchOptions = RequestInit & {
  auth?: boolean;
  retry?: boolean;
  /** Padrão 30s; sync ERP usa valor maior. */
  timeoutMs?: number;
};

export async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  if (
    typeof window !== "undefined" &&
    window.location.protocol === "https:" &&
    API_URL.startsWith("http://")
  ) {
    throw new ApiError(
      "A API está em HTTP, mas o painel está em HTTPS. Use NEXT_PUBLIC_API_URL com https:// (Caddy + domínio ou TLS na VM).",
      0
    );
  }

  const { auth = true, retry = true, timeoutMs = 30_000, ...init } = options;
  const headers = new Headers(init.headers);
  if (!headers.has("Content-Type") && init.body) {
    headers.set("Content-Type", "application/json");
  }

  if (auth) {
    const session = getStoredSession();
    if (!session) {
      if (typeof window !== "undefined" && !window.location.pathname.startsWith("/login")) {
        window.location.href = "/login";
      }
      throw new ApiError("Sessão expirada", 401);
    }
    headers.set("Authorization", `Bearer ${session.accessToken}`);
  }

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const res = await fetch(`${API_URL}${path}`, {
      ...init,
      headers,
      cache: "no-store",
      signal: controller.signal,
    });

    if (res.status === 401) {
      clearSession();
      if (typeof window !== "undefined") window.location.href = "/login";
      throw new ApiError("Sessão expirada", 401);
    }

    if (!res.ok) {
      const text = await res.text();
      if (retry && res.status >= 500) {
        await delay(800);
        return apiFetch<T>(path, { ...options, retry: false });
      }
      throw new ApiError(safeApiErrorMessage(res.status, text), res.status);
    }

    if (res.status === 204) return undefined as T;
    return (await res.json()) as T;
  } catch (e) {
    if (e instanceof ApiError) throw e;
    if ((e as Error).name === "AbortError") {
      throw new ApiError("Tempo esgotado ao contactar a API", 408);
    }
    throw new ApiError("API indisponível. Verifique se o backend está rodando.", 0);
  } finally {
    clearTimeout(timeout);
  }
}

function delay(ms: number) {
  return new Promise((r) => setTimeout(r, ms));
}

/** Evita exibir stack trace ou corpo técnico da API na UI. */
function safeApiErrorMessage(status: number, body: string): string {
  if (status === 401) return "Sessão expirada";
  if (status >= 500) return "Serviço temporariamente indisponível. Tente novamente.";
  try {
    const parsed = JSON.parse(body) as { message?: string };
    if (typeof parsed.message === "string" && parsed.message.length > 0 && parsed.message.length < 300) {
      return parsed.message;
    }
  } catch {
    /* corpo não-JSON */
  }
  if (
    body.length > 240 ||
    /Exception|StackTrace| at \w+\./i.test(body)
  ) {
    return `Não foi possível concluir a operação (${status}).`;
  }
  return body.trim() || `Erro ${status}`;
}
