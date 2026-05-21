"use client";

import { Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import type { TimeSeriesPoint } from "@/types/api";

export function KpiTrendChart({
  data,
  color = "#2563eb",
  valueFormatter,
}: {
  data: TimeSeriesPoint[];
  color?: string;
  valueFormatter?: (v: number) => string;
}) {
  const chartData = data.map((d) => ({
    date: new Date(d.date).toLocaleDateString("pt-BR", { month: "short" }),
    value: d.value,
  }));

  if (chartData.length === 0) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Sem série temporal.
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height="100%">
      <LineChart data={chartData}>
        <XAxis dataKey="date" tick={{ fontSize: 10 }} hide />
        <YAxis hide />
        <Tooltip
          formatter={(v: number) => valueFormatter?.(v) ?? v.toLocaleString("pt-BR")}
          contentStyle={{ borderRadius: 8, fontSize: 12 }}
        />
        <Line type="monotone" dataKey="value" stroke={color} strokeWidth={2} dot={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}
