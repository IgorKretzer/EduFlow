import { apiFetch } from "@/services/api/client";
import type { ErpConfig, SyncSchedulerStatus } from "@/types/api";

export const erpService = {
  getConfig() {
    return apiFetch<ErpConfig>("/api/erp/config");
  },

  saveConfig(body: {
    providerKey?: string;
    endpointUrl: string;
    username: string;
    password?: string;
    pageSize: number;
    syncEnabled: boolean;
    syncIntervalMinutes: number;
    searchParametersStudents?: string;
    searchParametersFinancial?: string;
    searchParametersContracts?: string;
  }) {
    return apiFetch<ErpConfig>("/api/erp/config", {
      method: "PUT",
      body: JSON.stringify(body),
    });
  },

  getScheduler() {
    return apiFetch<SyncSchedulerStatus>("/api/erp/scheduler");
  },
};
