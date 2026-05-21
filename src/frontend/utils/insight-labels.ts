import type { Insight } from "@/types/api";

export function severityLabel(severity: string): string {
  const s = severity.toLowerCase();
  if (s === "critical" || s === "high") return "Urgente";
  if (s === "warning" || s === "medium") return "Atenção";
  if (s === "success" || s === "low") return "Oportunidade";
  return "Informação";
}

export function insightDeepLink(insight: Insight): string {
  if (insight.deepLink) return insight.deepLink;
  if (insight.category === "financeiro" || insight.code.includes("delinq") || insight.code.includes("revenue"))
    return "/financeiro";
  if (insight.category === "retencao" || insight.code.includes("retention")) return "/retencao";
  if (
    insight.category === "evasao" ||
    insight.code.includes("churn") ||
    insight.code.includes("evasion")
  )
    return insight.code.includes("evasion") ? "/risk-enrollments" : "/evasao";
  return "/insights";
}
