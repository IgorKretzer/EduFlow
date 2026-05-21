"use client";

import { createElement } from "react";
import type { FinanceCashFlowDay } from "@/types/api";
import { formatCurrency } from "@/lib/format-currency";

type Props = {
  day: FinanceCashFlowDay;
};

export function CashFlowDayTooltip({ day }: Props) {
  const rows: { label: string; value: number; highlight?: boolean }[] = [
    { label: "Saldo inicial", value: day.openingBalance },
    { label: "Entradas", value: day.inflow },
    { label: "Saídas", value: day.outflow },
    { label: "Saldo operacional", value: day.operationalBalance, highlight: true },
    { label: "Saldo final", value: day.closingBalance },
  ];

  return createElement(
    "div",
    {
      className:
        "pointer-events-none absolute bottom-full left-1/2 z-20 mb-1 w-52 -translate-x-1/2 rounded-md bg-zinc-900 px-3 py-2 text-xs text-white shadow-lg",
      role: "tooltip",
    },
    rows.map((row) =>
      createElement(
        "div",
        { key: row.label, className: "flex justify-between gap-2 py-0.5" },
        createElement("span", { className: "text-zinc-300" }, row.label),
        createElement(
          "span",
          { className: row.highlight ? "font-semibold text-sky-300" : "tabular-nums" },
          formatCurrency(row.value)
        )
      )
    )
  );
}
