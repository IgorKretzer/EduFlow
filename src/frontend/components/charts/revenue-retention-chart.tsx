"use client";

import { RevenueAreaChart } from "@/components/charts/revenue-area-chart";
import { RetentionSnapshot } from "@/components/charts/retention-snapshot";
import type { DashboardKpi, TimeSeriesPoint } from "@/types/api";

type Props = {
  revenue: TimeSeriesPoint[];
  retentionKpi?: DashboardKpi;
};

/**
 * Receita = série temporal (R$). Retenção = indicador atual (%) — sem linha falsa repetida.
 */
export function RevenueRetentionChart({ revenue, retentionKpi }: Props) {
  if (revenue.length === 0) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Sem dados de receita no período. Sincronize o financeiro em Configurações.
      </div>
    );
  }

  return (
    <div className="flex h-full min-w-0 flex-col gap-4 xl:grid xl:grid-cols-[minmax(0,1fr)_240px] xl:items-stretch">
      <div className="min-h-[240px] min-w-0 flex-1 rounded-xl border border-border/50 bg-background/50 p-2">
        <p className="mb-2 px-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Receita (R$)
        </p>
        <div className="h-[220px] w-full min-w-0 md:h-[260px]">
          <RevenueAreaChart data={revenue} />
        </div>
      </div>
      <RetentionSnapshot kpi={retentionKpi} className="shrink-0 xl:max-w-[240px]" />
    </div>
  );
}
