"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  CheckCircle2,
  LineChart,
  Signal,
  Target,
  Wallet,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { FinancialCalendarMap } from "@/components/finance/financial-calendar-map";
import { FinancialPulseChart } from "@/components/finance/financial-pulse-chart";
import { FinancialStatementPanel } from "@/components/finance/financial-statement-panel";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useFinanceCashFlow } from "@/hooks/use-finance-cash-flow";
import { type FinanceDataBasis, useFinancePulse } from "@/hooks/use-finance-pulse";
import { financeService } from "@/services/api/finance.service";
import { formatCurrency, formatPercent } from "@/lib/format-currency";
import { cn } from "@/lib/utils";
import type { FinancePulseMetric, FinancePulseSignal, FinanceReceivables } from "@/types/api";

type Props = {
  receivables: FinanceReceivables;
  refreshSignal?: number;
};

function currentMonth() {
  const now = new Date();
  return { year: now.getFullYear(), month: now.getMonth() };
}

function severityTone(severity: string): "critical" | "attention" | "opportunity" | "stable" {
  const s = severity.toLowerCase();
  if (s === "critical" || s === "high") return "critical";
  if (s === "warning" || s === "medium") return "attention";
  if (s === "success" || s === "positive") return "opportunity";
  return "stable";
}

function signalStyles(tone: ReturnType<typeof severityTone>) {
  if (tone === "critical") {
    return "border-rose-200 bg-rose-50 text-rose-950 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-50";
  }
  if (tone === "attention") {
    return "border-amber-200 bg-amber-50 text-amber-950 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-50";
  }
  if (tone === "opportunity") {
    return "border-emerald-200 bg-emerald-50 text-emerald-950 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-50";
  }
  return "border-sky-200 bg-sky-50 text-sky-950 dark:border-sky-500/30 dark:bg-sky-500/10 dark:text-sky-50";
}

function metricIcon(key: string): LucideIcon {
  if (/received|gross|revenue/i.test(key)) return ArrowUpRight;
  if (/net|balance/i.test(key)) return ArrowDownRight;
  if (/overdue|open/i.test(key)) return AlertTriangle;
  return Wallet;
}

function metricTone(metric: FinancePulseMetric): "sky" | "emerald" | "amber" | "rose" | "slate" {
  const tone = metric.tone.toLowerCase();
  if (tone === "critical") return "rose";
  if (tone === "attention") return "amber";
  if (tone === "positive") return "emerald";
  if (/gross|received|revenue/i.test(metric.key)) return "sky";
  return "slate";
}

function formatMetric(metric: FinancePulseMetric) {
  if (metric.format === "percent") return formatPercent(metric.value);
  if (metric.format === "integer") return metric.value.toLocaleString("pt-BR");
  return formatCurrency(metric.value);
}

function fallbackMetrics(receivables: FinanceReceivables, totalOutflow: number): FinancePulseMetric[] {
  const grossRevenue = receivables.summary.totalInvoice;
  const received = receivables.summary.totalPaid;
  const overdue = receivables.summary.overdue;
  const netRevenue = received - totalOutflow;
  const goalProgress = grossRevenue > 0 ? Math.min(100, (received / grossRevenue) * 100) : 0;

  return [
    {
      key: "gross_revenue",
      label: "Faturamento bruto",
      value: grossRevenue,
      format: "currency",
      tone: "stable",
      hint: "Valor total das parcelas sincronizadas.",
    },
    {
      key: "received_revenue",
      label: "Receita recebida",
      value: received,
      format: "currency",
      tone: "positive",
      hint: `${formatPercent(goalProgress, 0)} do faturamento bruto atual.`,
    },
    {
      key: "net_revenue",
      label: "Faturamento liquido",
      value: netRevenue,
      format: "currency",
      tone: netRevenue >= 0 ? "positive" : "critical",
      hint: "Recebido menos saidas identificadas.",
    },
    {
      key: "overdue",
      label: "Inadimplencia",
      value: overdue,
      format: "currency",
      tone: receivables.summary.overdueShare >= 30 ? "critical" : "attention",
      hint: `${formatPercent(receivables.summary.overdueShare)} da carteira em aberto.`,
    },
  ];
}

export function FinancialPulseView({ receivables, refreshSignal = 0 }: Props) {
  const initial = currentMonth();
  const [year, setYear] = useState(initial.year);
  const [month, setMonth] = useState(initial.month);
  const [basis, setBasis] = useState<FinanceDataBasis>("cash");
  const [goalInput, setGoalInput] = useState("");
  const [savingGoal, setSavingGoal] = useState(false);
  const [goalMessage, setGoalMessage] = useState<string | null>(null);
  const { data: cashFlow, loading: cashLoading, reload: reloadCashFlow } = useFinanceCashFlow(year, month);
  const { data: pulse, loading: pulseLoading, error: pulseError, reload: reloadPulse } =
    useFinancePulse(year, month, basis);

  useEffect(() => {
    if (refreshSignal > 0) {
      reloadCashFlow();
      reloadPulse();
    }
  }, [refreshSignal, reloadCashFlow, reloadPulse]);

  const metrics = pulse?.metrics.length
    ? pulse.metrics.slice(0, 4)
    : fallbackMetrics(receivables, cashFlow?.summary.totalOutflow ?? 0);
  const healthStatus = pulse?.healthStatus ?? "stable";
  const healthMessage = pulse?.healthMessage ?? "Saude financeira calculada com base nos recebiveis carregados.";
  const goal = pulse?.goals[0];
  const statement = pulse?.statement ?? [];
  const signals: FinancePulseSignal[] =
    pulse?.signals.length
      ? pulse.signals
      : [
          {
            code: "fallback",
            title: "Pulso em modo inicial",
            message: "A API nova ainda nao retornou sinais; exibindo dados financeiros carregados.",
            severity: "warning",
            metricKey: "health",
            recommendedAction: "Verifique a API e a sincronizacao.",
            deepLink: "/financeiro",
          },
        ];

  useEffect(() => {
    const target = goal?.targetAmount;
    setGoalInput(target != null ? String(target).replace(".", ",") : "");
  }, [goal?.targetAmount, year, month]);

  async function saveRevenueGoal() {
    const normalized = goalInput.replace(/\./g, "").replace(",", ".");
    const target = Number(normalized);
    if (!Number.isFinite(target) || target < 0) {
      setGoalMessage("Informe uma meta valida.");
      return;
    }

    setSavingGoal(true);
    setGoalMessage(null);
    try {
      await financeService.upsertGoal("monthly_revenue", {
        year,
        month: month + 1,
        label: "Meta de receita mensal",
        targetAmount: target,
      });
      await reloadPulse();
      setGoalMessage("Meta atualizada.");
    } catch (e) {
      setGoalMessage(e instanceof Error ? e.message : "Nao foi possivel salvar a meta.");
    } finally {
      setSavingGoal(false);
    }
  }

  return (
    <div className="space-y-5">
      <section className="rounded-lg border border-border/70 bg-card p-5 shadow-card">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div className="max-w-3xl space-y-2">
            <div className="flex flex-wrap items-center gap-2 text-xs font-semibold uppercase tracking-wider text-primary">
              <Signal className="h-4 w-4" />
              Pulso financeiro
              <span className="rounded-full border border-border px-2 py-0.5 text-muted-foreground">
                Dados externos do ERP
              </span>
            </div>
            <h2 className="text-2xl font-semibold tracking-tight md:text-3xl">
              A leitura executiva do mes em uma tela
            </h2>
            <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground">
              Receita, inadimplencia, fluxo e risco de caixa organizados para o gestor decidir sem
              abrir planilhas. Metas entram como camada EduFlow, mantendo os dados financeiros vindos
              da integracao.
            </p>
          </div>

          <div className="min-w-[280px] space-y-3 rounded-lg border bg-muted/25 p-3">
            <div className="grid grid-cols-2 gap-2">
              <div>
                <p className="text-[11px] uppercase text-muted-foreground">Saude do mes</p>
                <p
                  className={cn(
                    "text-sm font-semibold",
                    healthStatus === "critical"
                      ? "text-rose-600"
                      : healthStatus === "attention"
                        ? "text-amber-600"
                        : "text-emerald-600"
                  )}
                >
                  {healthStatus === "critical" ? "Critica" : healthStatus === "attention" ? "Atencao" : "Controlada"}
                </p>
              </div>
              <div>
                <p className="text-[11px] uppercase text-muted-foreground">Meta do gestor</p>
                <p className="text-sm font-semibold text-foreground">
                  {goal?.progressPercent != null ? formatPercent(goal.progressPercent, 0) : "A cadastrar"}
                </p>
              </div>
            </div>
            <p className="text-xs leading-relaxed text-muted-foreground">{healthMessage}</p>
            <div className="space-y-2 rounded-md border bg-card/70 p-2">
              <label className="text-[11px] font-semibold uppercase text-muted-foreground">
                Meta de receita do mes
              </label>
              <div className="flex gap-2">
                <Input
                  inputMode="decimal"
                  value={goalInput}
                  onChange={(event) => setGoalInput(event.target.value)}
                  placeholder="Ex.: 120000"
                  className="h-8"
                />
                <Button type="button" size="sm" onClick={saveRevenueGoal} disabled={savingGoal}>
                  {savingGoal ? "Salvando" : "Salvar"}
                </Button>
              </div>
              {goalMessage && <p className="text-xs text-muted-foreground">{goalMessage}</p>}
            </div>
          </div>
        </div>

        <div className="mt-4 flex flex-wrap gap-2">
          {[
            ["cash", "Caixa"],
            ["due", "Vencimento"],
            ["competence", "Competencia"],
          ].map(([value, label]) => (
            <Button
              key={value}
              type="button"
              variant={basis === value ? "secondary" : "outline"}
              size="sm"
              onClick={() => setBasis(value as FinanceDataBasis)}
            >
              {label}
            </Button>
          ))}
        </div>
      </section>

      {pulseError && (
        <p className="rounded-lg border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-700 dark:text-amber-200">
          {pulseError}
        </p>
      )}

      {pulse?.gaps.map((gap) => (
        <p key={gap} className="rounded-lg border border-dashed bg-muted/30 px-4 py-3 text-xs text-muted-foreground">
          {gap}
        </p>
      ))}

      <section className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        {metrics.map((metric) => (
          <PulseMetric
            key={metric.key}
            icon={metricIcon(metric.key)}
            label={metric.label}
            value={formatMetric(metric)}
            helper={metric.hint}
            tone={metricTone(metric)}
          />
        ))}
      </section>

      <section className="grid gap-3 xl:grid-cols-[minmax(0,1.35fr)_minmax(320px,0.65fr)]">
        <div className="rounded-lg border bg-card shadow-card">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3">
            <div>
              <h3 className="text-base font-semibold">Curva de caixa</h3>
              <p className="text-xs text-muted-foreground">
                Previsto, realizado, saidas e saldo projetado para leitura tipo BI financeiro.
              </p>
            </div>
            <Button variant="outline" size="sm" asChild>
              <Link href="/financeiro?aba=fluxo">
                <LineChart className="h-4 w-4" />
                Fluxo completo
              </Link>
            </Button>
          </div>
          <div className="p-4">
            {pulseLoading || !pulse ? (
              <Skeleton className="h-[220px] w-full rounded-lg" />
            ) : (
              <FinancialPulseChart days={pulse.calendar} />
            )}
          </div>
        </div>

        <div className="rounded-lg border bg-card p-4 shadow-card">
          <div className="mb-3 flex items-center gap-2">
            <Target className="h-4 w-4 text-primary" />
            <h3 className="text-base font-semibold">Sinais EduFlow</h3>
            {pulseLoading && <span className="text-xs text-muted-foreground">Atualizando...</span>}
          </div>
          <div className="space-y-2">
            {signals.map((signal) => {
              const tone = severityTone(signal.severity);
              return (
                <article key={signal.code} className={cn("rounded-lg border p-3", signalStyles(tone))}>
                  <div className="flex items-start gap-2">
                    {tone === "opportunity" ? (
                      <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" />
                    ) : (
                      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
                    )}
                    <div className="min-w-0">
                      <p className="text-sm font-semibold">{signal.title}</p>
                      <p className="mt-1 text-xs leading-relaxed opacity-80">{signal.message}</p>
                      <p className="mt-2 text-xs font-semibold">{signal.recommendedAction}</p>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        </div>
      </section>

      <section className="grid gap-3 xl:grid-cols-[minmax(0,1.4fr)_minmax(300px,0.6fr)]">
        <div>
          {pulseLoading || !pulse ? (
            <Skeleton className="h-[520px] w-full rounded-lg" />
          ) : (
            <FinancialCalendarMap
              days={pulse.calendar}
              year={year}
              month={month}
            />
          )}
        </div>
        <FinancialStatementPanel lines={statement} />
      </section>
    </div>
  );
}

function PulseMetric({
  icon: Icon,
  label,
  value,
  helper,
  tone,
}: {
  icon: LucideIcon;
  label: string;
  value: string;
  helper: string;
  tone: "sky" | "emerald" | "amber" | "rose" | "slate";
}) {
  const tones = {
    sky: "bg-sky-50 text-sky-700 dark:bg-sky-500/10 dark:text-sky-300",
    emerald: "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300",
    amber: "bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-300",
    rose: "bg-rose-50 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300",
    slate: "bg-slate-100 text-slate-700 dark:bg-slate-500/10 dark:text-slate-300",
  };

  return (
    <article className="rounded-lg border bg-card p-4 shadow-card">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{label}</p>
          <p className="mt-2 truncate font-mono text-2xl font-semibold tabular-nums">{value}</p>
        </div>
        <div className={cn("rounded-lg p-2", tones[tone])}>
          <Icon className="h-4 w-4" />
        </div>
      </div>
      <p className="mt-3 text-xs leading-relaxed text-muted-foreground">{helper}</p>
    </article>
  );
}
