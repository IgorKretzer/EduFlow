import type { Insight } from "@/types/api";

export type AnalyticsSection = "financeiro" | "retencao" | "evasao";

const matchers: Record<AnalyticsSection, (i: Insight) => boolean> = {
  financeiro: (i) =>
    i.category === "financeiro" ||
    /receita|inadimpl|finance|delinq|revenue/i.test(i.code + i.title + i.message),
  retencao: (i) =>
    i.category === "retencao" ||
    /retenc|retention|perman/i.test(i.code + i.title + i.message),
  evasao: (i) =>
    i.category === "evasao" ||
    /evas|churn|evasion|risco/i.test(i.code + i.title + i.message),
};

export function insightsForSection(section: AnalyticsSection, insights: Insight[]): Insight[] {
  const filtered = insights.filter(matchers[section]);
  return filtered.length > 0 ? filtered : insights.slice(0, 4);
}

export function topInsightForSection(section: AnalyticsSection, insights: Insight[]): Insight | null {
  const list = insightsForSection(section, insights);
  const order = ["critical", "high", "warning", "medium", "success", "low", "info"];
  const rank = (s: string) => {
    const i = order.indexOf(s.toLowerCase());
    return i === -1 ? 50 : i;
  };
  return [...list].sort((a, b) => rank(a.severity) - rank(b.severity))[0] ?? null;
}
