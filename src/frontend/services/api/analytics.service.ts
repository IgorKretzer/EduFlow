import { apiFetch } from "@/services/api/client";
import type { EnrollmentDetail, EnrollmentPage, EnrollmentSummary } from "@/types/api";
import type { EnrollmentQuery } from "@/services/api/enrollment-query";

export type { EnrollmentQuery } from "@/services/api/enrollment-query";

/** Aceita resposta paginada (novo) ou array legado da API antiga. */
export function normalizeEnrollmentPage(
  raw: unknown,
  fallbackPage = 1,
  fallbackPageSize = 10
): EnrollmentPage {
  if (Array.isArray(raw)) {
    const items = raw.map(normalizeEnrollmentSummary);
    return {
      items,
      totalCount: items.length,
      page: fallbackPage,
      pageSize: fallbackPageSize,
    };
  }

  if (!raw || typeof raw !== "object") {
    return { items: [], totalCount: 0, page: fallbackPage, pageSize: fallbackPageSize };
  }

  const r = raw as Record<string, unknown>;
  const rawItems = (r.items ?? r.Items) as unknown;
  const items = Array.isArray(rawItems)
    ? rawItems.map(normalizeEnrollmentSummary)
    : [];

  return {
    items,
    totalCount: Number(r.totalCount ?? r.TotalCount ?? items.length) || 0,
    page: Number(r.page ?? r.Page ?? fallbackPage) || fallbackPage,
    pageSize: Number(r.pageSize ?? r.PageSize ?? fallbackPageSize) || fallbackPageSize,
  };
}

function normalizeEnrollmentSummary(raw: unknown): EnrollmentSummary {
  const r = (raw ?? {}) as Record<string, unknown>;
  return {
    enrollmentCode: String(r.enrollmentCode ?? r.EnrollmentCode ?? ""),
    unitCode: String(r.unitCode ?? r.UnitCode ?? ""),
    unitName: String(r.unitName ?? r.UnitName ?? ""),
    studentStatus: String(r.studentStatus ?? r.StudentStatus ?? ""),
    paymentStatus: String(r.paymentStatus ?? r.PaymentStatus ?? ""),
    debtAmount: Number(r.debtAmount ?? r.DebtAmount ?? 0),
    paidAmount: Number(r.paidAmount ?? r.PaidAmount ?? 0),
    churnRisk: String(r.churnRisk ?? r.ChurnRisk ?? "Baixo"),
    retentionLevel: String(r.retentionLevel ?? r.RetentionLevel ?? ""),
    trend: String(r.trend ?? r.Trend ?? ""),
    daysSinceLastPayment:
      r.daysSinceLastPayment != null || r.DaysSinceLastPayment != null
        ? Number(r.daysSinceLastPayment ?? r.DaysSinceLastPayment)
        : null,
    operationalScore: Number(r.operationalScore ?? r.OperationalScore ?? 50),
  };
}

export const analyticsService = {
  async listEnrollments(params: EnrollmentQuery = {}) {
    const page = params.page ?? 1;
    const pageSize = params.pageSize ?? 10;
    const q = new URLSearchParams();
    q.set("page", String(page));
    q.set("pageSize", String(pageSize));
    if (params.search) q.set("search", params.search);
    if (params.unitCode) q.set("unitCode", params.unitCode);
    if (params.risk) q.set("risk", params.risk);
    if (params.riskOnly) q.set("riskOnly", "true");
    const raw = await apiFetch<unknown>(`/api/analytics/enrollments?${q}`);
    return normalizeEnrollmentPage(raw, page, pageSize);
  },

  getEnrollment(code: string) {
    return apiFetch<EnrollmentDetail>(`/api/analytics/enrollments/${encodeURIComponent(code)}`);
  },
};
