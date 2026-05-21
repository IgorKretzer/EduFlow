"use client";

import { useState } from "react";
import { PageHeader } from "@/components/dashboard/page-header";
import { EnrollmentTable } from "@/components/analytics/enrollment-table";
import { EmptyState } from "@/components/dashboard/empty-state";
import { KpiCard } from "@/components/dashboard/kpi-card";
import { ScreenCta } from "@/components/dashboard/screen-cta";
import { Skeleton } from "@/components/ui/skeleton";
import { useEnrollmentsPaged } from "@/hooks/use-enrollments-paged";
import { useDashboardSummary } from "@/hooks/use-dashboard-summary";
import { getKpiIcon } from "@/utils/kpi-icons";
import { insightsForSection } from "@/utils/section-insights";
import { InsightCard } from "@/components/dashboard/insight-card";
import { AlertTriangle } from "lucide-react";

export function RiskEnrollmentsView() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState("");
  const [risk, setRisk] = useState("");

  const summary = useDashboardSummary();
  const { data, loading, error, reload } = useEnrollmentsPaged({
    page,
    pageSize,
    search,
    risk: risk || undefined,
    riskOnly: true,
  });

  const evasionInsights = summary.data
    ? insightsForSection("evasao", summary.data.insights).slice(0, 1)
    : [];
  const riskKpis =
    summary.data?.kpis.filter((k) =>
      /churn|evas|inadimpl|delinq|ativo|active/i.test(k.key + k.label)
    ) ?? [];

  if (error) {
    return (
      <EmptyState
        title="Não foi possível carregar as matrículas"
        description={error}
        actionLabel="Tentar novamente"
        onAction={reload}
      />
    );
  }

  return (
    <div className="animate-fade-in space-y-8">
      <PageHeader
        breadcrumbs={["EduFlow", "Matrículas em Risco"]}
        title="Matrículas em risco"
        description="Lista operacional por código de matrícula — sem nome, CPF ou telefone (LGPD)."
      />

      {evasionInsights[0] && <InsightCard insight={evasionInsights[0]} />}

      {!summary.loading && riskKpis.length > 0 && (
        <section className="grid gap-4 md:grid-cols-3">
          {riskKpis.slice(0, 3).map((kpi) => (
            <KpiCard key={kpi.key} kpi={kpi} icon={getKpiIcon(kpi.key)} />
          ))}
        </section>
      )}

      {summary.loading && (
        <div className="grid gap-4 md:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-2xl" />
          ))}
        </div>
      )}

      <ScreenCta
        title="Como usar esta lista"
        description="Clique em uma matrícula para ver score, histórico financeiro e tendência. Priorize churn alto e inadimplência."
        primary={{ label: "Voltar ao financeiro", href: "/financeiro" }}
        secondary={{ label: "Central de recomendações", href: "/insights" }}
      />

      {data && data.totalCount > 0 && (
        <p className="flex items-center gap-2 rounded-lg border border-border/60 bg-muted/30 px-4 py-2 text-sm text-muted-foreground">
          <AlertTriangle className="h-4 w-4 shrink-0 text-amber-500" />
          <span>
            <strong>{data.totalCount}</strong> matrícula(s) em risco no total — mostrando{" "}
            <strong>{data.items.length}</strong> nesta página. Ajuste o tamanho da página ou use a busca.
          </span>
        </p>
      )}

      <EnrollmentTable
        page={data}
        loading={loading}
        search={search}
        pageSize={pageSize}
        riskFilter={risk}
        onSearchChange={(v) => {
          setSearch(v);
          setPage(1);
        }}
        onPageChange={setPage}
        onPageSizeChange={(s) => {
          setPageSize(s);
          setPage(1);
        }}
        onRiskFilterChange={(v) => {
          setRisk(v);
          setPage(1);
        }}
      />
    </div>
  );
}
