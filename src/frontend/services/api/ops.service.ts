import { apiFetch } from "@/services/api/client";
import type { OpsStatus } from "@/types/api";

export const opsService = {
  getStatus() {
    return apiFetch<OpsStatus>("/api/ops/status");
  },
};
