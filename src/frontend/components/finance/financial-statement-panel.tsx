"use client";

import { AlertTriangle, CheckCircle2, Minus, Target, TrendingUp } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { FinanceStatementLine } from "@/types/api";
import { formatCurrency, formatPercent } from "@/lib/format-currency";
import { cn } from "@/lib/utils";

type Props = {
  lines: FinanceStatementLine[];
};

function lineByKey(lines: FinanceStatementLine[], key: string) {
  return lines.find((line) => line.key === key)?.amount ?? 0;
}

function rowTone(kind: string): "positive" | "negative" | "neutral" | "target" {
  if (kind === "revenue" || kind === "result") return "positive";
  if (kind === "expense" || kind === "deduction" || kind === "loss") return "negative";
  if (kind === "target") return "target";
  return "neutral";
}

function iconFor(kind: string): LucideIcon {
  if (kind === "target") return Target;
  if (kind === "expense" || kind === "deduction" || kind === "loss") return AlertTriangle;
  if (kind === "result") return CheckCircle2;
  return TrendingUp;
}

export function FinancialStatementPanel({ lines }: Props) {
  const gross = lineByKey(lines, "gross_revenue");
  const received = lineByKey(lines, "received_revenue");
  const goal = lineByKey(lines, "revenue_goal");
  const open = lineByKey(lines, "open_receivable");
  const overdue = lineByKey(lines, "overdue");
  const expectedOutflow = lineByKey(lines, "expected_outflow");
  const realizedOutflow = lineByKey(lines, "realized_outflow");
  const net = lineByKey(lines, "net_revenue");
  const margin = received > 0 ? (net / received) * 100 : 0;
  const goalProgress = goal > 0 ? (received / goal) * 100 : null;

  if (lines.length === 0) {
    return (
      <div className="rounded-lg border bg-card p-4 shadow-card">
        <p className="text-sm text-muted-foreground">Balancete ainda nao carregado.</p>
      </div>
    );
  }

  return (
    <div className="rounded-lg border bg-card shadow-card">
      <div className="border-b px-4 py-3">
        <h3 className="text-base font-semibold">Balancete gerencial</h3>
        <p className="text-xs text-muted-foreground">
          Visao executiva do resultado financeiro escolar no periodo.
        </p>
      </div>

      <div className="space-y-4 p-4">
        <div className="grid gap-2 sm:grid-cols-2">
          <StatementSummary
            label="Resultado liquido"
            value={net}
            helper={`Margem sobre recebido: ${formatPercent(margin)}`}
            tone={net >= 0 ? "positive" : "negative"}
          />
          <StatementSummary
            label="Progresso da meta"
            value={goalProgress ?? 0}
            helper={goal > 0 ? `${formatCurrency(received)} de ${formatCurrency(goal)}` : "Meta ainda nao cadastrada"}
            tone={goalProgress == null ? "neutral" : goalProgress >= 100 ? "positive" : "target"}
            percent
          />
        </div>

        <StatementGroup title="Receita" lines={lines.filter((line) => line.kind === "revenue")} />
        <StatementGroup
          title="Meta e carteira"
          lines={lines.filter((line) => line.kind === "target" || line.key === "open_receivable")}
        />
        <StatementGroup
          title="Deducoes e despesas"
          lines={lines.filter((line) => ["deduction", "expense"].includes(line.kind))}
        />

        <div className="rounded-lg border bg-muted/20 p-3">
          <div className="flex items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              <span
                className={cn(
                  "rounded-md p-1.5",
                  net >= 0
                    ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300"
                    : "bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300"
                )}
              >
                {net >= 0 ? <CheckCircle2 className="h-4 w-4" /> : <AlertTriangle className="h-4 w-4" />}
              </span>
              <div>
                <p className="text-sm font-semibold">Resultado do periodo</p>
                <p className="text-xs text-muted-foreground">
                  Recebido menos despesas pagas identificadas.
                </p>
              </div>
            </div>
            <p className={cn("font-mono text-lg font-semibold tabular-nums", net >= 0 ? "text-emerald-600" : "text-rose-600")}>
              {formatCurrency(net)}
            </p>
          </div>
        </div>

        <div className="grid gap-2 text-xs text-muted-foreground sm:grid-cols-2">
          <p className="rounded-lg border border-dashed bg-muted/20 p-3">
            Inadimplencia vencida: <strong className="text-foreground">{formatCurrency(overdue)}</strong>
          </p>
          <p className="rounded-lg border border-dashed bg-muted/20 p-3">
            Despesas previstas/pagas:{" "}
            <strong className="text-foreground">
              {formatCurrency(expectedOutflow)} / {formatCurrency(realizedOutflow)}
            </strong>
          </p>
        </div>
      </div>
    </div>
  );
}

function StatementSummary({
  label,
  value,
  helper,
  tone,
  percent,
}: {
  label: string;
  value: number;
  helper: string;
  tone: "positive" | "negative" | "neutral" | "target";
  percent?: boolean;
}) {
  const colors = {
    positive: "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300",
    negative: "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300",
    neutral: "border-border bg-muted/20 text-foreground",
    target: "border-sky-200 bg-sky-50 text-sky-700 dark:border-sky-500/30 dark:bg-sky-500/10 dark:text-sky-300",
  };

  return (
    <div className={cn("rounded-lg border p-3", colors[tone])}>
      <p className="text-xs font-semibold uppercase opacity-75">{label}</p>
      <p className="mt-1 font-mono text-xl font-semibold tabular-nums">
        {percent ? formatPercent(value) : formatCurrency(value)}
      </p>
      <p className="mt-1 text-xs opacity-80">{helper}</p>
    </div>
  );
}

function StatementGroup({ title, lines }: { title: string; lines: FinanceStatementLine[] }) {
  if (lines.length === 0) return null;

  return (
    <section className="rounded-lg border">
      <div className="border-b bg-muted/20 px-3 py-2">
        <p className="text-xs font-semibold uppercase text-muted-foreground">{title}</p>
      </div>
      <div className="divide-y">
        {lines.map((line) => (
          <StatementRow key={line.key} line={line} />
        ))}
      </div>
    </section>
  );
}

function StatementRow({ line }: { line: FinanceStatementLine }) {
  const tone = rowTone(line.kind);
  const Icon = iconFor(line.kind);
  const iconClass = {
    positive: "bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300",
    negative: "bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-300",
    neutral: "bg-muted text-muted-foreground",
    target: "bg-sky-100 text-sky-700 dark:bg-sky-500/10 dark:text-sky-300",
  }[tone];
  const valueClass = {
    positive: "text-emerald-600",
    negative: "text-rose-600",
    neutral: "text-foreground",
    target: "text-sky-600",
  }[tone];

  return (
    <div className="flex items-center justify-between gap-3 px-3 py-2.5">
      <div className="flex items-center gap-2">
        <span className={cn("rounded-md p-1.5", iconClass)}>
          {line.kind === "neutral" ? <Minus className="h-3.5 w-3.5" /> : <Icon className="h-3.5 w-3.5" />}
        </span>
        <span className="text-sm text-muted-foreground">{line.label}</span>
      </div>
      <span className={cn("font-mono text-sm font-semibold tabular-nums", valueClass)}>
        {formatCurrency(line.amount)}
      </span>
    </div>
  );
}
