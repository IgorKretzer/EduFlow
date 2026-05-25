"use client";

import { useMemo, useState } from "react";
import { AlertTriangle, ArrowDownRight, ArrowUpRight, CalendarDays } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { FinancePulseCalendarDay } from "@/types/api";
import { formatCompactCurrency, formatCurrency } from "@/lib/format-currency";
import { cn } from "@/lib/utils";

const WEEKDAYS = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab"];

type Props = {
  days: FinancePulseCalendarDay[];
  year: number;
  month: number;
};

function statusClass(status: string) {
  const s = status.toLowerCase();
  if (s === "critical") return "border-rose-300 bg-rose-50 dark:border-rose-500/40 dark:bg-rose-500/10";
  if (s === "attention") return "border-amber-300 bg-amber-50 dark:border-amber-500/40 dark:bg-amber-500/10";
  if (s === "positive") return "border-emerald-300 bg-emerald-50 dark:border-emerald-500/40 dark:bg-emerald-500/10";
  return "border-border bg-card hover:bg-muted/40";
}

function statusLabel(status: string) {
  const s = status.toLowerCase();
  if (s === "critical") return "Critico";
  if (s === "attention") return "Atencao";
  if (s === "positive") return "Saudavel";
  return "Sem movimento";
}

function barWidth(value: number, max: number) {
  if (max <= 0 || value <= 0) return "0%";
  return `${Math.max(8, Math.min(100, (value / max) * 100))}%`;
}

export function FinancialCalendarMap({ days, year, month }: Props) {
  const [selectedDay, setSelectedDay] = useState<FinancePulseCalendarDay | null>(null);
  const selected = selectedDay ?? days.find((d) => d.status !== "neutral") ?? days[0];
  const monthLabel = new Date(year, month, 1).toLocaleDateString("pt-BR", {
    month: "long",
    year: "numeric",
  });

  const leadingBlanks = useMemo(() => new Date(year, month, 1).getDay(), [year, month]);
  const maxMovement = useMemo(
    () =>
      Math.max(
        0,
        ...days.map((day) =>
          Math.max(day.expectedInflow, day.realizedInflow, day.expectedOutflow, day.realizedOutflow)
        )
      ),
    [days]
  );

  const monthTotals = useMemo(
    () => ({
      expectedInflow: days.reduce((sum, d) => sum + d.expectedInflow, 0),
      realizedInflow: days.reduce((sum, d) => sum + d.realizedInflow, 0),
      expectedOutflow: days.reduce((sum, d) => sum + d.expectedOutflow, 0),
      overdue: days.reduce((sum, d) => sum + d.overdueAmount, 0),
    }),
    [days]
  );

  if (days.length === 0) {
    return (
      <div className="rounded-lg border bg-card p-8 text-center text-sm text-muted-foreground shadow-card">
        Calendario financeiro sem dados para o periodo.
      </div>
    );
  }

  return (
    <div className="rounded-lg border bg-card shadow-card">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3">
        <div className="flex items-center gap-2">
          <CalendarDays className="h-4 w-4 text-primary" />
          <div>
            <h3 className="text-base font-semibold capitalize">{monthLabel}</h3>
            <p className="text-xs text-muted-foreground">
              Mapa financeiro: previsto, realizado, vencidos e pressao de caixa por dia.
            </p>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-2 text-xs sm:grid-cols-4">
          <MiniTotal label="Previsto" value={monthTotals.expectedInflow} tone="sky" />
          <MiniTotal label="Realizado" value={monthTotals.realizedInflow} tone="emerald" />
          <MiniTotal label="Saidas" value={monthTotals.expectedOutflow} tone="violet" />
          <MiniTotal label="Vencido" value={monthTotals.overdue} tone="rose" />
        </div>
      </div>

      <div className="grid gap-4 p-4 xl:grid-cols-[minmax(0,1fr)_320px]">
        <div>
          <div className="mb-2 grid grid-cols-7">
            {WEEKDAYS.map((weekday) => (
              <div key={weekday} className="px-1 text-center text-xs font-semibold text-muted-foreground">
                {weekday}
              </div>
            ))}
          </div>
          <div className="grid grid-cols-7 overflow-hidden rounded-lg border">
            {Array.from({ length: leadingBlanks }).map((_, index) => (
              <div key={`blank-${index}`} className="min-h-[6.25rem] border-l border-t bg-muted/30" />
            ))}

            {days.map((day) => {
              const selectedState = selected?.date === day.date;
              const net = day.realizedInflow - day.realizedOutflow;
              return (
                <button
                  key={day.date}
                  type="button"
                  onClick={() => setSelectedDay(day)}
                  className={cn(
                    "flex min-h-[6.25rem] flex-col gap-1 border-l border-t p-2 text-left transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                    statusClass(day.status),
                    selectedState && "ring-2 ring-primary"
                  )}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-xs font-semibold">{day.day}</span>
                    {day.overdueAmount > 0 && <AlertTriangle className="h-3.5 w-3.5 text-rose-600" />}
                  </div>
                  <div className="mt-auto space-y-1">
                    <BarLine
                      value={day.expectedInflow}
                      max={maxMovement}
                      className="bg-sky-400"
                      label="Prev."
                    />
                    <BarLine
                      value={day.realizedInflow}
                      max={maxMovement}
                      className="bg-emerald-500"
                      label="Real."
                    />
                    {day.expectedOutflow > 0 && (
                      <BarLine
                        value={day.expectedOutflow}
                        max={maxMovement}
                        className="bg-violet-500"
                        label="Sai."
                      />
                    )}
                    <div
                      className={cn(
                        "truncate rounded px-1 py-0.5 text-[10px] font-semibold",
                        net >= 0
                          ? "bg-emerald-500/10 text-emerald-700 dark:text-emerald-300"
                          : "bg-rose-500/10 text-rose-700 dark:text-rose-300"
                      )}
                    >
                      {net >= 0 ? "+" : "-"}
                      {formatCompactCurrency(Math.abs(net))}
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        </div>

        {selected && (
          <aside className="rounded-lg border bg-muted/20 p-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-xs uppercase text-muted-foreground">Dia selecionado</p>
                <h4 className="text-lg font-semibold">
                  {new Date(`${selected.date}T00:00:00`).toLocaleDateString("pt-BR", {
                    day: "2-digit",
                    month: "long",
                  })}
                </h4>
              </div>
              <span className="rounded-full border px-2 py-1 text-xs font-semibold">
                {statusLabel(selected.status)}
              </span>
            </div>

            <div className="mt-4 space-y-3">
              <DetailLine icon={ArrowUpRight} label="Entrada prevista" value={selected.expectedInflow} tone="sky" />
              <DetailLine icon={ArrowUpRight} label="Entrada realizada" value={selected.realizedInflow} tone="emerald" />
              <DetailLine icon={ArrowDownRight} label="Saida prevista" value={selected.expectedOutflow} tone="violet" />
              <DetailLine icon={ArrowDownRight} label="Saida realizada" value={selected.realizedOutflow} tone="violet" />
              <DetailLine icon={AlertTriangle} label="Vencido no dia" value={selected.overdueAmount} tone="rose" />
            </div>

            <div className="mt-4 rounded-lg border bg-card p-3">
              <p className="text-xs text-muted-foreground">Saldo projetado acumulado</p>
              <p className="mt-1 font-mono text-xl font-semibold tabular-nums">
                {formatCurrency(selected.projectedBalance)}
              </p>
            </div>
          </aside>
        )}
      </div>
    </div>
  );
}

function MiniTotal({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: "sky" | "emerald" | "violet" | "rose";
}) {
  const colors = {
    sky: "text-sky-700 dark:text-sky-300",
    emerald: "text-emerald-700 dark:text-emerald-300",
    violet: "text-violet-700 dark:text-violet-300",
    rose: "text-rose-700 dark:text-rose-300",
  };
  return (
    <div className="rounded-md border bg-card px-2 py-1">
      <p className="text-[10px] uppercase text-muted-foreground">{label}</p>
      <p className={cn("font-mono font-semibold tabular-nums", colors[tone])}>
        {formatCompactCurrency(value)}
      </p>
    </div>
  );
}

function BarLine({
  value,
  max,
  className,
  label,
}: {
  value: number;
  max: number;
  className: string;
  label: string;
}) {
  if (value <= 0) return null;
  return (
    <div className="flex items-center gap-1">
      <span className="w-7 text-[9px] text-muted-foreground">{label}</span>
      <span className="h-1.5 flex-1 rounded-full bg-background/70">
        <span className={cn("block h-1.5 rounded-full", className)} style={{ width: barWidth(value, max) }} />
      </span>
    </div>
  );
}

function DetailLine({
  icon: Icon,
  label,
  value,
  tone,
}: {
  icon: LucideIcon;
  label: string;
  value: number;
  tone: "sky" | "emerald" | "violet" | "rose";
}) {
  const colors = {
    sky: "bg-sky-100 text-sky-700 dark:bg-sky-500/10 dark:text-sky-300",
    emerald: "bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300",
    violet: "bg-violet-100 text-violet-700 dark:bg-violet-500/10 dark:text-violet-300",
    rose: "bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300",
  };
  return (
    <div className="flex items-center justify-between gap-3">
      <div className="flex items-center gap-2">
        <span className={cn("rounded-md p-1.5", colors[tone])}>
          <Icon className="h-3.5 w-3.5" />
        </span>
        <span className="text-sm text-muted-foreground">{label}</span>
      </div>
      <span className="font-mono text-sm font-semibold tabular-nums">{formatCurrency(value)}</span>
    </div>
  );
}
