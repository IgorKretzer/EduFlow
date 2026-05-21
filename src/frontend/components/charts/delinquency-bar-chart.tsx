"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { DelinquencyByUnit } from "@/types/api";

export function DelinquencyBarChart({ data }: { data: DelinquencyByUnit[] }) {
  const chartData = data.map((u) => ({
    name: u.unitName.length > 12 ? `${u.unitName.slice(0, 12)}…` : u.unitName,
    rate: Number((u.delinquencyRate * 100).toFixed(1)),
    fullName: u.unitName,
  }));

  if (chartData.length === 0) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Sem dados por unidade.
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="hsl(214 32% 91%)" vertical={false} />
        <XAxis dataKey="name" tick={{ fontSize: 11 }} />
        <YAxis tick={{ fontSize: 11 }} unit="%" />
        <Tooltip
          contentStyle={{ borderRadius: 8, border: "1px solid hsl(214 32% 91%)" }}
          formatter={(v: number, _n, item) => [
            `${v}%`,
            (item?.payload as { fullName?: string })?.fullName ?? "Unidade",
          ]}
        />
        <Bar dataKey="rate" fill="#1d4ed8" radius={[4, 4, 0, 0]} maxBarSize={48} />
      </BarChart>
    </ResponsiveContainer>
  );
}
