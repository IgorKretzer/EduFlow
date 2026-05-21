"use client";

import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { FinanceCashFlowDay } from "@/types/api";
import { formatCompactCurrency, formatCurrency } from "@/lib/format-currency";

type Props = {
  days: FinanceCashFlowDay[];
};

export function CashFlowEvolutionChart({ days }: Props) {
  const chartData = days
    .filter(
      (d) =>
        d.inflow > 0 ||
        d.outflow > 0 ||
        Math.abs(d.operationalBalance) > 0.01 ||
        d.scheduledReceivable > 0
    )
    .map((d) => {
      const [, m, day] = d.date.split("-");
      return {
        label: `${day}/${m}`,
        inflow: d.inflow,
        outflow: d.outflow,
        balance: d.operationalBalance,
      };
    });

  if (chartData.length === 0) {
    return (
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">
        Sem movimentação no período.
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={180}>
      <ComposedChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="8 8" stroke="hsl(214 32% 91%)" vertical={false} />
        <XAxis dataKey="label" tick={{ fontSize: 10 }} />
        <YAxis tick={{ fontSize: 10 }} tickFormatter={(v) => formatCompactCurrency(Number(v))} />
        <Tooltip
          formatter={(value: number, name: string) => [
            formatCurrency(Number(value)),
            name === "inflow"
              ? "Entradas"
              : name === "outflow"
                ? "Saídas"
                : "Saldo operacional",
          ]}
        />
        <Bar dataKey="inflow" fill="rgba(158,231,247,0.85)" barSize={10} />
        <Bar dataKey="outflow" fill="rgba(196,181,253,0.65)" barSize={10} />
        <Line type="monotone" dataKey="balance" stroke="#111827" strokeWidth={1.5} dot={false} />
      </ComposedChart>
    </ResponsiveContainer>
  );
}
