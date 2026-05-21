"use client";

import {
  ArrowDownToLine,
  CalendarClock,
  CalendarX2,
  UsersRound,
} from "lucide-react";
import type { FinanceReceivablesSummary } from "@/types/api";
import { formatCurrency, formatPercent } from "@/lib/format-currency";

type Props = {
  summary: FinanceReceivablesSummary;
};

function MetricRow({
  icon: Icon,
  iconClass,
  label,
  value,
}: {
  icon: React.ComponentType<{ className?: string }>;
  iconClass: string;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-center gap-2">
      <div className={`rounded-lg p-2 ${iconClass}`}>
        <Icon className="h-4 w-4" />
      </div>
      <div>
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-sm font-medium tabular-nums">{value}</p>
      </div>
    </div>
  );
}

export function ReceivablesSummaryCard({ summary }: Props) {
  return (
    <div className="rounded-xl border bg-card shadow-card lg:row-span-2">
      <div className="border-b px-6 py-4">
        <h3 className="text-base font-semibold text-muted-foreground">
          Resumo de Contas a Receber
        </h3>
      </div>
      <div className="space-y-4 p-6">
        <div className="text-center">
          <p className="text-xs text-muted-foreground">Total em aberto</p>
          <p className="text-xl font-medium tabular-nums">
            {formatCurrency(summary.totalReceivable)}
          </p>
        </div>

        <MetricRow
          icon={CalendarX2}
          iconClass="bg-violet-100 text-violet-500"
          label="Vencidos"
          value={formatCurrency(summary.overdue)}
        />
        <MetricRow
          icon={CalendarClock}
          iconClass="bg-sky-100 text-sky-500"
          label="A vencer"
          value={formatCurrency(summary.toReceive)}
        />
        <MetricRow
          icon={ArrowDownToLine}
          iconClass="bg-green-100 text-green-500"
          label="Total já recebido (período filtrado)"
          value={formatCurrency(summary.totalPaid)}
        />
        <MetricRow
          icon={UsersRound}
          iconClass="bg-indigo-100 text-indigo-500"
          label="Matrículas com saldo em aberto"
          value={String(summary.peopleCount)}
        />

        <hr className="border-border" />

        <div className="flex gap-1">
          <div className="min-w-0 flex-1">
            <p className="text-xs text-muted-foreground">Vencidos</p>
            <div className="mt-1 rounded-md bg-violet-300 px-3 py-1 shadow-sm">
              <span className="text-xs font-semibold text-violet-700">
                {formatPercent(summary.overdueShare)}
              </span>
            </div>
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-xs text-muted-foreground">A vencer</p>
            <div className="mt-1 rounded-md bg-sky-300 px-3 py-1 shadow-sm">
              <span className="text-xs font-semibold text-sky-700">
                {formatPercent(summary.toReceiveShare)}
              </span>
            </div>
          </div>
        </div>

        <p className="text-center text-[11px] text-muted-foreground">
          Valores reais das parcelas em CanonicalFinancials (staging).
        </p>
      </div>
    </div>
  );
}
