"use client";

import { ArrowDown, ArrowUp, HandCoins, Info } from "lucide-react";
import type { FinanceCashFlowSummary } from "@/types/api";
import { formatCurrency, formatPercent } from "@/lib/format-currency";

type Props = {
  summary: FinanceCashFlowSummary;
};

export function CashFlowSummaryCard({ summary }: Props) {
  const hasOutflows = summary.totalOutflow > 0;

  return (
    <div className="rounded-xl border bg-card shadow-card">
      <div className="border-b px-4 py-3">
        <h3 className="text-base font-semibold text-muted-foreground">Status geral</h3>
      </div>
      <div className="space-y-4 p-6">
        <div className="text-center">
          <p className="text-xs text-zinc-500">Saldo final do mês</p>
          <p className="text-xl font-medium tabular-nums text-zinc-600">
            {formatCurrency(summary.closingBalance)}
          </p>
        </div>

        <div className="flex flex-wrap justify-around gap-4 xl:flex-col">
          <div className="flex items-center gap-2">
            <div className="rounded-lg bg-green-100 p-2 text-green-500">
              <HandCoins className="h-4 w-4" />
            </div>
            <div>
              <p className="text-xs text-zinc-500">Saldo inicial (recebido antes do mês)</p>
              <p className="text-sm font-medium tabular-nums">
                {formatCurrency(summary.openingBalance)}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <div className="rounded-lg bg-sky-100 p-2 text-sky-500">
              <ArrowUp className="h-4 w-4" />
            </div>
            <div>
              <p className="text-xs text-zinc-500">Total de entradas no mês</p>
              <p className="flex items-center gap-1 text-sm font-medium tabular-nums">
                {formatCurrency(summary.totalInflow)}
                <Info className="h-3.5 w-3.5 text-muted-foreground" aria-hidden />
              </p>
              <p className="text-[11px] text-muted-foreground">
                Inadimplência (em aberto vencido): {formatPercent(summary.delinquencyRatePct)}
              </p>
            </div>
          </div>
          {hasOutflows && (
            <div className="flex items-center gap-2">
              <div className="rounded-lg bg-purple-100 p-2 text-purple-500">
                <ArrowDown className="h-4 w-4" />
              </div>
              <div>
                <p className="text-xs text-zinc-500">Total de saídas</p>
                <p className="text-sm font-medium tabular-nums">
                  {formatCurrency(summary.totalOutflow)}
                </p>
              </div>
            </div>
          )}
        </div>

        {!hasOutflows && (
          <p className="text-center text-xs text-muted-foreground">
            O ERP não envia despesas; saídas aparecem zeradas até integrarmos contas a pagar.
          </p>
        )}

        <hr className="border-border" />

        {hasOutflows && (
          <div className="flex gap-1">
            <div className="flex-1">
              <p className="text-xs text-muted-foreground">Entradas</p>
              <div className="mt-1 rounded-md bg-sky-400 px-3 py-1 shadow-sm">
                <span className="text-xs font-semibold text-white">
                  {formatPercent(summary.inflowShare)}
                </span>
              </div>
            </div>
            <div className="flex-1">
              <p className="text-xs text-muted-foreground">Saídas</p>
              <div className="mt-1 rounded-md bg-purple-400 px-3 py-1 shadow-sm">
                <span className="text-xs font-semibold text-white">
                  {formatPercent(summary.outflowShare)}
                </span>
              </div>
            </div>
          </div>
        )}

        {summary.inflowBreakdown.length > 0 && (
          <div className="space-y-2 text-xs">
            <p className="font-semibold text-muted-foreground">Entradas por situação</p>
            {summary.inflowBreakdown.map((m) => (
              <div key={m.label} className="flex justify-between gap-2">
                <span className="text-muted-foreground">{m.label}</span>
                <span className="tabular-nums">{formatCurrency(m.amount)}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
