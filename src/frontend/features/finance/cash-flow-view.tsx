"use client";

import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import { CashFlowEvolutionChart } from "@/components/charts/cash-flow-evolution-chart";
import { CashFlowCalendar } from "@/components/finance/cash-flow-calendar";
import { CashFlowSummaryCard } from "@/components/finance/cash-flow-summary-card";
import { useFinanceCashFlow } from "@/hooks/use-finance-cash-flow";

type Props = {
  refreshSignal?: number;
};

export function CashFlowView({ refreshSignal = 0 }: Props) {
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth());
  const { data, loading, error, reload } = useFinanceCashFlow(year, month);

  useEffect(() => {
    if (refreshSignal > 0) reload();
  }, [refreshSignal, reload]);

  if (loading || !data) {
    return (
      <div className="flex items-center justify-center gap-2 py-24 text-muted-foreground">
        <Loader2 className="h-5 w-5 animate-spin" />
        Carregando fluxo de caixa…
      </div>
    );
  }

  if (error) {
    return (
      <p className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
        {error}
      </p>
    );
  }

  return (
    <div className="grid gap-3 lg:grid-cols-3 lg:grid-rows-[auto_auto]">
      <div className="lg:col-span-2 lg:row-span-2">
        <CashFlowCalendar
          days={data.days}
          year={year}
          month={month}
          onMonthChange={(y, m) => {
            setYear(y);
            setMonth(m);
          }}
        />
      </div>
      <div className="lg:row-span-2">
        <CashFlowSummaryCard summary={data.summary} />
      </div>
      <div className="rounded-xl border bg-card shadow-card lg:col-span-2">
        <div className="border-b px-4 py-3">
          <h3 className="text-base font-semibold text-muted-foreground">
            Evolução do fluxo de caixa
          </h3>
          <p className="mt-0.5 text-xs text-muted-foreground">
            Entradas = valores pagos na data de vencimento da parcela. Saídas não vêm do ERP.
          </p>
        </div>
        <div className="p-4">
          <CashFlowEvolutionChart days={data.days} />
        </div>
      </div>
    </div>
  );
}
