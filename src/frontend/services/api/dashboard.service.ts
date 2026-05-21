import { apiFetch } from "@/services/api/client";
import type { DashboardFilter, DashboardSummary, Insight } from "@/types/api";

function toQuery(filter?: DashboardFilter) {
  const params = new URLSearchParams();
  if (filter?.unitId) params.set("unitId", filter.unitId);
  if (filter?.from) params.set("from", filter.from);
  if (filter?.to) params.set("to", filter.to);
  const q = params.toString();
  return q ? `?${q}` : "";
}

function normalizeInsight(raw: Record<string, unknown>): Insight {
  return {
    code: String(raw.code ?? raw.Code ?? ""),
    title: String(raw.title ?? raw.Title ?? ""),
    message: String(raw.message ?? raw.Message ?? ""),
    severity: String(raw.severity ?? raw.Severity ?? "info"),
    generatedAt: String(raw.generatedAt ?? raw.GeneratedAt ?? new Date().toISOString()),
    recommendedAction: (raw.recommendedAction ?? raw.RecommendedAction) as string | null | undefined,
    deepLink: (raw.deepLink ?? raw.DeepLink) as string | null | undefined,
    category: (raw.category ?? raw.Category) as string | null | undefined,
  };
}

function normalizeSummary(raw: unknown): DashboardSummary {
  const r = (raw ?? {}) as Record<string, unknown>;
  const insightsRaw = (r.insights ?? r.Insights) as unknown;
  const insights = Array.isArray(insightsRaw)
    ? insightsRaw.map((i) => normalizeInsight(i as Record<string, unknown>))
    : [];

  return {
    ...(r as DashboardSummary),
    insights,
  };
}

export const dashboardService = {
  async getSummary(filter?: DashboardFilter) {
    const raw = await apiFetch<unknown>(`/api/dashboard/summary${toQuery(filter)}`);
    return normalizeSummary(raw);
  },
};
