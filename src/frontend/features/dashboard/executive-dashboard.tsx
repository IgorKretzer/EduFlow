"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  ArrowRight,
  GraduationCap,
  Percent,
  TrendingDown,
  Wallet,
} from "lucide-react";
import { KpiCard } from "@/components/dashboard/kpi-card";
import { ChartCard } from "@/components/dashboard/chart-card";
import { InsightsRail } from "@/components/dashboard/insights-rail";
import { SyncToolbar } from "@/components/dashboard/sync-toolbar";
import { EmptyState } from "@/components/dashboard/empty-state";
import { ForecastWidget } from "@/components/dashboard/forecast-widget";
import { UnitRanking } from "@/components/dashboard/unit-ranking";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { useDashboardSummary } from "@/hooks/use-dashboard-summary";
import { findKpi, orderKpis } from "@/utils/kpi-order";
import { cn } from "@/lib/utils";

const RevenueRetentionChart = dynamic(
  () =>
    import("@/components/charts/revenue-retention-chart").then((m) => m.RevenueRetentionChart),
  { ssr: false, loading: () => <Skeleton className="h-full min-h-[260px] w-full" /> }
);

const DelinquencyBarChart = dynamic(
  () => import("@/components/charts/delinquency-bar-chart").then((m) => m.DelinquencyBarChart),
  { ssr: false, loading: () => <Skeleton className="h-48 w-full" /> }
);

const quickLinks = [
  { href: "/risk-enrollments", label: "Matrículas em risco", tone: "text-rose-600 dark:text-rose-400" },
  { href: "/financeiro", label: "Financeiro", tone: "text-amber-600 dark:text-amber-400" },
  { href: "/insights", label: "Recomendações", tone: "text-primary" },
];

export function ExecutiveDashboard() {
  const router = useRouter();
  const { data, loading, error, reload } = useDashboardSummary();

  if (loading) return <DashboardSkeleton />;

  if (error || !data) {
    return (
      <EmptyState
        title="Não foi possível carregar o painel"
        description={error ?? "Verifique a conexão com a API e sincronize os dados em Configurações."}
        actionLabel="Tentar novamente"
        onAction={reload}
      />
    );
  }

  const ordered = orderKpis(data.kpis);
  const heroRevenue = findKpi(data.kpis, /receita|revenue/i) ?? ordered[0];
  const heroRetention = findKpi(data.kpis, /retenc/i);
  const secondaryKpis = ordered.filter((k) => k.key !== heroRevenue?.key).slice(0, 3);
  const retention = heroRetention;
  const revenueSpark = data.revenueTrend.map((p) => ({ value: p.value }));

  const kpiNav: Record<string, string> = {
    revenue: "/financeiro",
    receita: "/financeiro",
    retention: "/retencao",
    retenc: "/retencao",
    churn: "/evasao",
    evas: "/evasao",
    delinq: "/financeiro",
    inadimpl: "/financeiro",
    alunos: "/retencao",
    student: "/retencao",
  };

  function routeForKpi(key: string) {
    const k = key.toLowerCase();
    const match = Object.entries(kpiNav).find(([p]) => k.includes(p));
    return match?.[1] ?? "/";
  }

  return (
    <div className="animate-fade-in w-full space-y-6 md:space-y-8">
      <section className="command-center">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0 space-y-2">
            <p className="text-xs font-semibold uppercase tracking-widest text-primary">
              Centro de comando
            </p>
            <h1 className="text-2xl font-semibold tracking-tight md:text-3xl">
              Como está a operação da escola
            </h1>
            <p className="text-sm text-muted-foreground md:text-base">
              Receita, retenção, evasão e unidades críticas — o que exige sua atenção hoje.
            </p>
            <div className="flex flex-wrap gap-2 pt-2">
              {quickLinks.map((link) => (
                <Button key={link.href} variant="outline" size="sm" asChild>
                  <Link href={link.href} className={cn("gap-1", link.tone)}>
                    {link.label}
                    <ArrowRight className="h-3.5 w-3.5" />
                  </Link>
                </Button>
              ))}
            </div>
          </div>
          <SyncToolbar onSynced={reload} />
        </div>
      </section>

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {heroRevenue && (
          <KpiCard
            className="sm:col-span-2"
            kpi={heroRevenue}
            icon={Wallet}
            featured
            sparkline={revenueSpark}
            onClick={() => router.push(routeForKpi(heroRevenue.key))}
          />
        )}
        {heroRetention && (
          <KpiCard
            kpi={{
              ...heroRetention,
              format: heroRetention.format === "currency" ? "percent" : heroRetention.format,
            }}
            icon={Percent}
            onClick={() => router.push("/retencao")}
          />
        )}
        {secondaryKpis.map((kpi) => {
          const isChurn = /evas|churn/i.test(kpi.key + kpi.label);
          return (
            <KpiCard
              key={kpi.key}
              kpi={kpi}
              icon={isChurn ? TrendingDown : GraduationCap}
              onClick={() => router.push(routeForKpi(kpi.key))}
            />
          );
        })}
      </section>

      <section className="grid grid-cols-1 gap-5 xl:grid-cols-3">
        <ChartCard
          className="xl:col-span-2"
          variant="hero"
          title="Receita no período"
          description="Evolução em reais e retenção consolidada"
        >
          <div className="min-h-[280px] w-full">
            <RevenueRetentionChart revenue={data.revenueTrend} retentionKpi={retention} />
          </div>
        </ChartCard>
        <div className="flex min-h-[320px] flex-col rounded-2xl border border-border/60 bg-card p-4 shadow-card">
          <InsightsRail insights={data.insights} />
        </div>
      </section>

      <section className="grid grid-cols-1 gap-5 md:grid-cols-2 xl:grid-cols-3">
        <ChartCard title="Ranking de unidades" description="Inadimplência por unidade">
          <UnitRanking
            units={data.delinquencyByUnit}
            onUnitClick={() => router.push("/relatorios")}
          />
        </ChartCard>
        <ChartCard title="Mapa de inadimplência">
          <div className="h-[220px] w-full">
            <DelinquencyBarChart data={data.delinquencyByUnit} />
          </div>
        </ChartCard>
        <ForecastWidget revenue={data.revenueTrend} />
      </section>
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <div className="w-full space-y-8">
      <Skeleton className="h-36 w-full rounded-2xl" />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-36 rounded-2xl" />
        ))}
      </div>
      <Skeleton className="h-96 w-full rounded-2xl" />
    </div>
  );
}
