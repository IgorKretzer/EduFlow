"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { DelinquencyByUnit } from "@/types/api";

export function UnitComparisonChart({ data }: { data: DelinquencyByUnit[] }) {
  const chartData = data.map((u) => ({
    name: u.unitCode,
    inadimplencia: Number((u.delinquencyRate * 100).toFixed(1)),
    vencidos: u.overdueCount,
    divida: u.debtAmount / 1000,
  }));

  if (chartData.length === 0) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Sem comparativo entre unidades.
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" stroke="hsl(214 32% 91%)" />
        <XAxis dataKey="name" tick={{ fontSize: 11 }} />
        <YAxis yAxisId="left" tick={{ fontSize: 11 }} />
        <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 11 }} />
        <Tooltip contentStyle={{ borderRadius: 8 }} />
        <Legend />
        <Bar yAxisId="left" dataKey="inadimplencia" name="Inadimplência %" fill="#2563eb" radius={[4, 4, 0, 0]} />
        <Bar yAxisId="right" dataKey="vencidos" name="Títulos vencidos" fill="#94a3b8" radius={[4, 4, 0, 0]} />
      </BarChart>
    </ResponsiveContainer>
  );
}
