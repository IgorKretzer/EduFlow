"use client";

import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { TimeSeriesPoint } from "@/types/api";

const tooltipStyle = {
  borderRadius: 8,
  border: "1px solid hsl(214 32% 91%)",
  boxShadow: "0 8px 24px rgba(15,23,42,0.08)",
};

export function RevenueAreaChart({ data }: { data: TimeSeriesPoint[] }) {
  const chartData = data.map((d) => ({
    date: new Date(d.date).toLocaleDateString("pt-BR", { day: "2-digit", month: "short" }),
    value: d.value,
  }));

  if (chartData.length === 0) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Sem dados de receita no período.
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height="100%">
      <AreaChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id="revenueFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#2563eb" stopOpacity={0.25} />
            <stop offset="100%" stopColor="#2563eb" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="hsl(214 32% 91%)" vertical={false} />
        <XAxis dataKey="date" tick={{ fontSize: 11 }} stroke="hsl(215 16% 47%)" />
        <YAxis
          tick={{ fontSize: 11 }}
          stroke="hsl(215 16% 47%)"
          tickFormatter={(v) =>
            Number(v).toLocaleString("pt-BR", { notation: "compact", compactDisplay: "short" })
          }
        />
        <Tooltip
          contentStyle={tooltipStyle}
          formatter={(v: number) =>
            v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })
          }
        />
        <Area
          type="monotone"
          dataKey="value"
          stroke="#2563eb"
          strokeWidth={2}
          fill="url(#revenueFill)"
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}
