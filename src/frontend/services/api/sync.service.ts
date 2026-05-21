import { apiFetch } from "@/services/api/client";
import type { SyncResult } from "@/types/api";

export type SyncEntityType =
  | "students"
  | "financial"
  | "payables"
  | "categories"
  | "financial-full"
  | "contracts";

/** Sync chama o ERP (Sponte) e pode levar vários minutos. */
const SYNC_TIMEOUT_MS = 5 * 60 * 1000;

export const syncService = {
  trigger(entityType: SyncEntityType) {
    return apiFetch<SyncResult>("/api/sync/trigger", {
      method: "POST",
      body: JSON.stringify({ entityType }),
      timeoutMs: SYNC_TIMEOUT_MS,
      retry: false,
    });
  },
};
