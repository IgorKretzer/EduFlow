"use client";

import { DonutWithLegend } from "@/components/charts/donut-with-legend";
import { HorizontalStackedBar } from "@/components/charts/horizontal-stacked-bar";
import { ReceivablesSummaryCard } from "@/components/finance/receivables-summary-card";
import { paletteColor, SKY_PALETTE, VIOLET_PALETTE } from "@/lib/finance-colors";
import type { FinanceAmountSlice, FinanceReceivables, FinanceSituation } from "@/types/api";

type Props = {
  data: FinanceReceivables;
};

function toDonut(slices: FinanceAmountSlice[], palette: string[]) {
  return slices.map((s, i) => ({
    id: s.id,
    label: s.label,
    value: s.openAmount,
    color: paletteColor(palette, i),
    secondaryValue: s.invoiceAmount,
  }));
}

function toBarItems(items: FinanceSituation[], palette: string[]) {
  return items.map((s, i) => ({
    id: s.id,
    label: s.label,
    amount: s.amount,
    color: paletteColor(palette, i),
  }));
}

export function ReceivablesView({ data }: Props) {
  const empty = data.summary.totalReceivable <= 0;

  if (empty) {
    return (
      <p className="rounded-lg border border-dashed bg-muted/30 px-6 py-16 text-center text-sm text-muted-foreground">
        Nenhuma parcela em aberto no financeiro sincronizado. Execute o sync de Financeiro nas
        configurações.
      </p>
    );
  }

  return (
    <div className="grid gap-3 lg:grid-cols-3">
      <DonutWithLegend
        title="Documentos vencidos"
        data={toDonut(data.overdueSlices, VIOLET_PALETTE)}
        centerLabel="Por categoria / forma de recebimento (valor em aberto)"
      />
      <HorizontalStackedBar
        title="Situação das parcelas (vencidos)"
        items={toBarItems(data.overdueSituations, VIOLET_PALETTE)}
      />
      <ReceivablesSummaryCard summary={data.summary} />
      <DonutWithLegend
        title="Documentos a vencer"
        data={toDonut(data.toMatureSlices, SKY_PALETTE)}
        centerLabel="Por categoria / forma de recebimento (valor em aberto)"
      />
      <HorizontalStackedBar
        title="Situação das parcelas (a vencer)"
        items={toBarItems(data.toMatureSituations, SKY_PALETTE)}
      />
    </div>
  );
}
