import { apiFetch } from "@/services/api/client";
import type {
  DashboardFilter,
  FinanceCashFlow,
  FinanceGoal,
  FinancePulse,
  FinanceReceivables,
  UpsertFinanceGoalRequest,
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

  async getPulse(
    year: number,
    month: number,
    dataBasis: "cash" | "due" | "competence",
    filter?: DashboardFilter
  ) {
    return apiFetch<FinancePulse>(
      `/api/finance/pulse${toQuery(filter, {
        year: String(year),
        month: String(month + 1),
        dataBasis,
      })}`,
      { timeoutMs: FINANCE_TIMEOUT_MS }
    );
  },

  async getGoals(year: number, month: number, filter?: Pick<DashboardFilter, "unitId">) {
    return apiFetch<FinanceGoal[]>(
      `/api/finance/goals${toQuery(filter, {
        year: String(year),
        month: String(month + 1),
      })}`,
      { timeoutMs: FINANCE_TIMEOUT_MS }
    );
  },

  async upsertGoal(key: string, request: UpsertFinanceGoalRequest) {
    return apiFetch<FinanceGoal>(`/api/finance/goals/${key}`, {
      method: "PUT",
      body: JSON.stringify(request),
      timeoutMs: FINANCE_TIMEOUT_MS,
    });
  },
};
