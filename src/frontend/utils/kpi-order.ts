import type { DashboardKpi } from "@/types/api";

const PRIORITY = [
  "active_students",
  "alunos",
  "retention",
  "retenc",
  "revenue",
  "receita",
  "churn",
  "evas",
  "delinq",
  "inadimpl",
];

export function orderKpis(kpis: DashboardKpi[]) {
  return [...kpis].sort((a, b) => score(a.key) - score(b.key));
}

function score(key: string) {
  const k = key.toLowerCase();
  const idx = PRIORITY.findIndex((p) => k.includes(p));
  return idx === -1 ? 99 : idx;
}

export function findKpi(kpis: DashboardKpi[], pattern: RegExp) {
  return kpis.find((k) => pattern.test(k.key) || pattern.test(k.label));
}
