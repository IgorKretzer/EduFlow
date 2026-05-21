"use client";

import dynamic from "next/dynamic";
import { useRouter } from "next/navigation";
import { KpiCard } from "@/components/dashboard/kpi-card";
import { ChartCard } from "@/components/dashboard/chart-card";
import { PageHeader } from "@/components/dashboard/page-header";
import { EmptyState } from "@/components/dashboard/empty-state";
import { InsightCard } from "@/components/dashboard/insight-card";
import { ScreenCta } from "@/components/dashboard/screen-cta";
import { SyncToolbar } from "@/components/dashboard/sync-toolbar";
import { UnitRanking } from "@/components/dashboard/unit-ranking";
import { DelinquencyBarChart } from "@/components/charts/delinquency-bar-chart";
import { KpiTrendChart } from "@/components/charts/kpi-trend-chart";
import { Skeleton } from "@/components/ui/skeleton";
import { useDashboardSummary } from "@/hooks/use-dashboard-summary";
import { getKpiIcon } from "@/utils/kpi-icons";
import { insightsForSection, topInsightForSection, type AnalyticsSection } from "@/utils/section-insights";
import { cn } from "@/lib/utils";
import type { DashboardKpi } from "@/types/api";

const RevenueAreaChart = dynamic(
  () => import("@/components/charts/revenue-area-chart").then((m) => m.RevenueAreaChart),
  { ssr: false, loading: () => <Skeleton className="h-full w-full" /> }
);

const filters: Record<AnalyticsSection, (k: DashboardKpi) => boolean> = {
  financeiro: (k) =>
    /receita|revenue|inadimpl|delinq|finance/i.test(k.key + k.label),
  retencao: (k) => /retenc|retention|ativo|active|growth|cresc/i.test(k.key + k.label),
  evasao: (k) => /churn|evas|cancel/i.test(k.key + k.label),
};

const meta: Record<
  AnalyticsSection,
  {
    title: string;
    description: string;
    breadcrumbs: string[];
    cta: { title: string; description: string; primary: { label: string; href: string }; secondary?: { label: string; href: string } };
  }
> = {
  financeiro: {
    title: "Financeiro",
    description: "Receita, inadimplência e exposição — o que priorizar na cobrança esta semana.",
    breadcrumbs: ["EduFlow", "Financeiro"],
    cta: {
      title: "Próximo passo",
      description: "Identifique matrículas com maior risco financeiro e acione a equipe de cobrança.",
      primary: { label: "Revisar inadimplência", href: "/risk-enrollments" },
      secondary: { label: "Ranking por unidade", href: "/relatorios" },
    },
  },
  retencao: {
    title: "Retenção",
    description: "Permanência dos alunos e saúde da base ativa — onde a escola está ganhando ou perdendo.",
    breadcrumbs: ["EduFlow", "Retenção"],
    cta: {
      title: "Próximo passo",
      description: "Compare unidades e reforce ações de engajamento onde a retenção caiu.",
      primary: { label: "Ver recomendações", href: "/insights" },
      secondary: { label: "Matrículas em risco", href: "/risk-enrollments" },
    },
  },
  evasao: {
    title: "Evasão",
    description: "Sinais de perda de alunos e churn — antecipe antes do cancelamento definitivo.",
    breadcrumbs: ["EduFlow", "Evasão"],
    cta: {
      title: "Próximo passo",
      description: "Priorize matrículas com churn alto e alinhe pedagógico + financeiro.",
      primary: { label: "Ver matrículas em risco", href: "/risk-enrollments" },
      secondary: { label: "Central de recomendações", href: "/insights" },
    },
  },
};

export function AnalyticsSection({ section }: { section: AnalyticsSection }) {
  const router = useRouter();
  const { data, loading, error, reload } = useDashboardSummary();
  const info = meta[section];

  if (loading) {
    return (
      <div className="space-y-8">
        <Skeleton className="h-20 w-full max-w-xl" />
        <div className="grid gap-4 md:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-2xl" />
          ))}
        </div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <EmptyState
        title="Dados indisponíveis"
        description={error ?? "Sincronize os dados da escola em Configurações ou use os botões abaixo."}
        actionLabel="Recarregar"
        onAction={reload}
      />
    );
  }

  const kpis = data.kpis.filter(filters[section]);
  const displayKpis = kpis.length > 0 ? kpis : data.kpis.slice(0, 3);
  const sectionInsights = insightsForSection(section, data.insights);
  const priority = topInsightForSection(section, data.insights);
  const revenueSpark = data.revenueTrend.map((p) => ({ value: p.value }));

  const pageAccent =
    section === "financeiro"
      ? "analytics-page analytics-page-financeiro"
      : section === "retencao"
        ? "analytics-page analytics-page-retencao"
        : "analytics-page analytics-page-evasao";

  return (
    <div className={cn("animate-fade-in space-y-6", pageAccent)}>
      <PageHeader
        breadcrumbs={info.breadcrumbs}
        title={info.title}
        description={info.description}
        actions={<SyncToolbar onSynced={reload} />}
      />

      {priority && (
        <InsightCard insight={priority} className="border-primary/20 shadow-card" />
      )}

      <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {displayKpis.map((kpi) => {
          const isRevenue = /receita|revenue/i.test(kpi.key + kpi.label);
          const isRetention = /retenc/i.test(kpi.key + kpi.label);
          return (
            <KpiCard
              key={kpi.key}
              kpi={
                isRetention && kpi.format === "currency"
                  ? { ...kpi, format: "percent" }
                  : kpi
              }
              icon={getKpiIcon(kpi.key)}
              sparkline={isRevenue ? revenueSpark : undefined}
            />
          );
        })}
      </section>

      <section className="grid gap-6 lg:grid-cols-2">
        {section === "financeiro" && (
          <>
            <ChartCard title="Receita no período" description="Evolução da receita consolidada">
              <div className="h-[280px]">
                <RevenueAreaChart data={data.revenueTrend} />
              </div>
            </ChartCard>
            <ChartCard title="Inadimplência por unidade" description="Unidades que exigem atenção">
              <div className="h-[280px]">
                <DelinquencyBarChart data={data.delinquencyByUnit} />
              </div>
            </ChartCard>
          </>
        )}
        {section === "retencao" && (
          <>
            <ChartCard title="Tendência da base" description="Proxy de movimento da operação">
              <div className="h-[280px]">
                <KpiTrendChart data={data.revenueTrend} color="hsl(var(--primary))" />
              </div>
            </ChartCard>
            <ChartCard title="Recomendações de retenção" description="Filtradas para esta área">
              <div className="max-h-[280px] space-y-3 overflow-y-auto pr-1">
                {sectionInsights.slice(0, 4).map((i) => (
                  <InsightCard key={`${i.code}-${i.generatedAt}`} insight={i} />
                ))}
              </div>
            </ChartCard>
          </>
        )}
        {section === "evasao" && (
          <>
            <ChartCard title="Pressão por unidade" description="Proxy de risco via inadimplência">
              <div className="h-[280px]">
                <DelinquencyBarChart data={data.delinquencyByUnit} />
              </div>
            </ChartCard>
            <ChartCard title="Alertas de evasão" description="O que exige ação imediata">
              <div className="max-h-[280px] space-y-3 overflow-y-auto pr-1">
                {sectionInsights.slice(0, 4).map((i) => (
                  <InsightCard key={`${i.code}-${i.generatedAt}`} insight={i} />
                ))}
              </div>
            </ChartCard>
          </>
        )}
      </section>

      {section === "financeiro" && (
        <ChartCard title="Ranking de unidades" description="Clique para abrir relatórios detalhados">
          <UnitRanking
            units={data.delinquencyByUnit}
            limit={6}
            onUnitClick={() => router.push("/relatorios")}
          />
        </ChartCard>
      )}

      <ScreenCta
        title={info.cta.title}
        description={info.cta.description}
        primary={info.cta.primary}
        secondary={info.cta.secondary}
      />
    </div>
  );
}
