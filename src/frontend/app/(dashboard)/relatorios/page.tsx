"use client";

import dynamic from "next/dynamic";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/dashboard/page-header";
import { DataTable, type Column } from "@/components/dashboard/data-table";
import { EmptyState } from "@/components/dashboard/empty-state";
import { ChartCard } from "@/components/dashboard/chart-card";
import { KpiCard } from "@/components/dashboard/kpi-card";
import { ScreenCta } from "@/components/dashboard/screen-cta";
import { UnitRanking } from "@/components/dashboard/unit-ranking";
import { Skeleton } from "@/components/ui/skeleton";
import { useDashboardSummary } from "@/hooks/use-dashboard-summary";
import { getKpiIcon } from "@/utils/kpi-icons";
import { findKpi } from "@/utils/kpi-order";
import type { DelinquencyByUnit } from "@/types/api";

const DelinquencyBarChart = dynamic(
  () => import("@/components/charts/delinquency-bar-chart").then((m) => m.DelinquencyBarChart),
  { ssr: false, loading: () => <Skeleton className="h-48 w-full" /> }
);

const columns: Column<DelinquencyByUnit>[] = [
  { key: "code", header: "Código", render: (r) => <span className="font-mono text-xs">{r.unitCode}</span> },
  { key: "name", header: "Unidade", render: (r) => <span className="font-medium">{r.unitName}</span> },
  {
    key: "risk",
    header: "Inadimplência",
    render: (r) => (
      <span className="font-semibold tabular-nums text-rose-500 dark:text-rose-400">
        {(r.delinquencyRate * 100).toFixed(1)}%
      </span>
    ),
  },
  {
    key: "debt",
    header: "Exposição",
    render: (r) =>
      r.debtAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }),
  },
];

export default function RelatoriosPage() {
  const router = useRouter();
  const { data, loading, error, reload } = useDashboardSummary();

  if (loading) {
    return (
      <div className="space-y-8">
        <Skeleton className="h-20 w-full max-w-xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  if (error || !data) {
    return (
      <EmptyState
        title="Relatórios indisponíveis"
        description={error ?? "Sincronize os dados e tente novamente."}
        actionLabel="Recarregar"
        onAction={reload}
      />
    );
  }

  const delinqKpi = findKpi(data.kpis, /inadimpl|delinq/i);
  const revenueKpi = findKpi(data.kpis, /receita|revenue/i);
  const highlightKpis = [delinqKpi, revenueKpi].filter(Boolean);

  return (
    <div className="animate-fade-in space-y-8">
      <PageHeader
        breadcrumbs={["EduFlow", "Relatórios"]}
        title="Relatórios"
        description="Comparativo entre unidades — onde a escola mais precisa intervir."
      />

      {highlightKpis.length > 0 && (
        <section className="grid gap-4 md:grid-cols-2">
          {highlightKpis.map((kpi) => (
            <KpiCard key={kpi!.key} kpi={kpi!} icon={getKpiIcon(kpi!.key)} />
          ))}
        </section>
      )}

      <section className="grid gap-6 lg:grid-cols-2">
        <ChartCard title="Mapa de inadimplência" description="Visão consolidada por unidade">
          <div className="h-[260px]">
            <DelinquencyBarChart data={data.delinquencyByUnit} />
          </div>
        </ChartCard>
        <ChartCard title="Ranking rápido" description="As unidades mais críticas primeiro">
          <UnitRanking
            units={data.delinquencyByUnit}
            limit={6}
            onUnitClick={() => router.push("/financeiro")}
          />
        </ChartCard>
      </section>

      <ChartCard
        title="Tabela por unidade"
        description="Busque e compare exposição financeira"
        contentClassName="h-auto min-h-[200px]"
      >
        <DataTable
          data={data.delinquencyByUnit}
          columns={columns}
          searchPlaceholder="Buscar unidade…"
          searchFilter={(r, q) => r.unitName.toLowerCase().includes(q)}
        />
      </ChartCard>

      <ScreenCta
        title="Próximo passo"
        description="Unidade crítica identificada? Revise as matrículas em risco dessa unidade."
        primary={{ label: "Matrículas em risco", href: "/risk-enrollments" }}
        secondary={{ label: "Área financeira", href: "/financeiro" }}
      />
    </div>
  );
}
