import { apiFetch } from "@/services/api/client";
import type {
  DashboardFilter,
  FinanceCashFlow,
  FinanceReceivables,
} from "@/types/api";

function toQuery(filter?: DashboardFilter, extra?: Record<string, string>) {
  const params = new URLSearchParams();
  if (filter?.unitId) params.set("unitId", filter.unitId);
  if (filter?.from) params.set("from", filter.from);
  if (filter?.to) params.set("to", filter.to);
  if (extra) {
    for (const [k, v] of Object.entries(extra)) params.set(k, v);
  }
  const q = params.toString();
  return q ? `?${q}` : "";
}

const FINANCE_TIMEOUT_MS = 90_000;

export const financeService = {
  async getReceivables(filter?: DashboardFilter) {
    return apiFetch<FinanceReceivables>(`/api/finance/receivables${toQuery(filter)}`, {
      timeoutMs: FINANCE_TIMEOUT_MS,
    });
  },

  async getCashFlow(year: number, month: number, filter?: DashboardFilter) {
    return apiFetch<FinanceCashFlow>(
      `/api/finance/cash-flow${toQuery(filter, {
        year: String(year),
        month: String(month + 1),
      })}`,
      { timeoutMs: FINANCE_TIMEOUT_MS }
    );
  },
};
