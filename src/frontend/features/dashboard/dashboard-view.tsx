"use client";

import { KpiCard } from "@/components/dashboard/kpi-card";
import { ChartCard } from "@/components/dashboard/chart-card";
import { InsightList } from "@/components/dashboard/insight-card";
import { PageHeader } from "@/components/dashboard/page-header";
import { SyncToolbar } from "@/components/dashboard/sync-toolbar";
import { EmptyState } from "@/components/dashboard/empty-state";
import { DataTable, type Column } from "@/components/dashboard/data-table";
import { RevenueAreaChart } from "@/components/charts/revenue-area-chart";
import { DelinquencyBarChart } from "@/components/charts/delinquency-bar-chart";
import { UnitComparisonChart } from "@/components/charts/unit-comparison-chart";
import { Skeleton } from "@/components/ui/skeleton";
import { useDashboardSummary } from "@/hooks/use-dashboard-summary";
import { getKpiIcon } from "@/utils/kpi-icons";
import type { DelinquencyByUnit } from "@/types/api";

const riskColumns: Column<DelinquencyByUnit>[] = [
  { key: "unit", header: "Unidade", render: (r) => r.unitName },
  {
    key: "rate",
    header: "Inadimplência",
    render: (r) => (
      <span className="font-medium text-rose-600">{(r.delinquencyRate * 100).toFixed(1)}%</span>
    ),
  },
  {
    key: "debt",
    header: "Em aberto",
    render: (r) =>
      r.debtAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }),
  },
  { key: "overdue", header: "Vencidos", render: (r) => r.overdueCount },
];

export function DashboardView() {
  const { data, loading, error, reload } = useDashboardSummary();

  if (loading) {
    return <DashboardSkeleton />;
  }

  if (error || !data) {
    return (
      <EmptyState
        title="Não foi possível carregar o painel"
        description={error ?? "Verifique a conexão com a API e execute a sincronização em Configurações."}
        actionLabel="Tentar novamente"
        onAction={reload}
      />
    );
  }

  return (
    <div className="animate-fade-in space-y-8">
      <PageHeader
        breadcrumbs={["EduFlow", "Executivo"]}
        title="Painel executivo"
        description="Visão consolidada de receita, retenção, evasão e risco operacional entre unidades."
        actions={<SyncToolbar onSynced={reload} />}
      />

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6">
        {data.kpis.map((kpi) => (
          <KpiCard key={kpi.key} kpi={kpi} icon={getKpiIcon(kpi.key)} />
        ))}
      </section>

      <section className="grid gap-6 xl:grid-cols-3">
        <ChartCard
          className="xl:col-span-2"
          title="Evolução da receita"
          description="Série temporal consolidada do data warehouse"
        >
          <RevenueAreaChart data={data.revenueTrend} />
        </ChartCard>
        <ChartCard title="Insights executivos" description="Alertas gerados por regras de negócio">
          <div className="max-h-[280px] overflow-y-auto pr-1">
            <InsightList insights={data.insights.slice(0, 4)} />
          </div>
        </ChartCard>
      </section>

      <section className="grid gap-6 lg:grid-cols-2">
        <ChartCard title="Inadimplência por unidade" description="Taxa percentual atual">
          <DelinquencyBarChart data={data.delinquencyByUnit} />
        </ChartCard>
        <ChartCard title="Comparativo entre unidades" description="Risco e volume de títulos">
          <UnitComparisonChart data={data.delinquencyByUnit} />
        </ChartCard>
      </section>

      <section>
        <h2 className="mb-4 text-lg font-semibold">Ranking de risco por unidade</h2>
        <DataTable
          data={data.delinquencyByUnit}
          columns={riskColumns}
          searchPlaceholder="Buscar unidade…"
          searchFilter={(row, q) =>
            row.unitName.toLowerCase().includes(q) || row.unitCode.toLowerCase().includes(q)
          }
        />
      </section>
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <div className="space-y-8">
      <Skeleton className="h-20 w-full max-w-2xl" />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} className="h-28 rounded-xl" />
        ))}
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        <Skeleton className="h-80 rounded-xl" />
        <Skeleton className="h-80 rounded-xl" />
      </div>
    </div>
  );
}
