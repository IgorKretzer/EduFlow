"use client";

import { normalizePercentValue } from "@/lib/percent";
import { cn, formatKpiValue } from "@/lib/utils";
import type { DashboardKpi } from "@/types/api";

type Props = {
  kpi?: DashboardKpi;
  className?: string;
};

/** Indicador de retenção atual (não é série temporal — evita linha falsa no gráfico de receita). */
export function RetentionSnapshot({ kpi, className }: Props) {
  const raw = kpi?.value ?? 0;
  const percent = normalizePercentValue(raw);
  const label = kpi?.label ?? "Retenção";
  const format = kpi?.format === "currency" || !kpi?.format ? "percent" : kpi.format;
  const display = kpi ? formatKpiValue(kpi.value, format) : "—";

  const tone =
    percent >= 85
      ? "text-emerald-600 dark:text-emerald-400"
      : percent >= 70
        ? "text-amber-600 dark:text-amber-400"
        : "text-rose-600 dark:text-rose-400";

  const barTone =
    percent >= 85 ? "bg-emerald-500" : percent >= 70 ? "bg-amber-500" : "bg-rose-500";

  return (
    <div
      className={cn(
        "flex h-full flex-col justify-center rounded-xl border border-border/60 bg-muted/30 p-6",
        className
      )}
    >
      <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{label}</p>
      <p className={cn("mt-2 font-mono text-5xl font-bold tabular-nums tracking-tight", tone)}>{display}</p>
      <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
        Indicador consolidado do período. A curva abaixo mostra apenas a receita em reais.
      </p>
      <div className="mt-6 h-3 overflow-hidden rounded-full bg-muted">
        <div
          className={cn("h-full rounded-full transition-all duration-500", barTone)}
          style={{ width: `${Math.min(100, Math.max(0, percent))}%` }}
        />
      </div>
      <div className="mt-2 flex justify-between text-[10px] font-medium uppercase tracking-wide text-muted-foreground">
        <span>0%</span>
        <span>Meta 85%</span>
        <span>100%</span>
      </div>
    </div>
  );
}
