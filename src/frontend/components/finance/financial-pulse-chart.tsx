"use client";

import {
  Area,
  Bar,
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { FinancePulseCalendarDay } from "@/types/api";
import { formatCompactCurrency, formatCurrency } from "@/lib/format-currency";

type Props = {
  days: FinancePulseCalendarDay[];
};

type TooltipPayload = {
  name: string;
  value: number;
};

function labelFor(name: string) {
  switch (name) {
    case "expectedInflow":
      return "Entrada prevista";
    case "realizedInflow":
      return "Entrada realizada";
    case "expectedOutflow":
      return "Saida prevista";
    case "projectedBalance":
      return "Saldo projetado";
    default:
      return name;
  }
}

function PulseTooltip({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: TooltipPayload[];
  label?: string;
}) {
  if (!active || !payload?.length) return null;

  return (
    <div className="rounded-lg border bg-card px-3 py-2 text-xs shadow-lg">
      <p className="mb-2 font-semibold text-foreground">Dia {label}</p>
      <div className="space-y-1">
        {payload.map((item) => (
          <div key={item.name} className="flex justify-between gap-6">
            <span className="text-muted-foreground">{labelFor(item.name)}</span>
            <span className="font-mono font-semibold tabular-nums">
              {formatCurrency(Number(item.value))}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

export function FinancialPulseChart({ days }: Props) {
  const chartData = days.map((day) => ({
    day: String(day.day).padStart(2, "0"),
    expectedInflow: day.expectedInflow,
    realizedInflow: day.realizedInflow,
    expectedOutflow: day.expectedOutflow,
    projectedBalance: day.projectedBalance,
  }));

  const hasMovement = chartData.some(
    (day) =>
      day.expectedInflow > 0 ||
      day.realizedInflow > 0 ||
      day.expectedOutflow > 0 ||
      Math.abs(day.projectedBalance) > 0.01
  );

  if (!hasMovement) {
    return (
      <div className="flex h-[260px] items-center justify-center rounded-lg border border-dashed bg-muted/20 text-sm text-muted-foreground">
        Sem movimento financeiro para montar a curva do periodo.
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={280}>
      <ComposedChart data={chartData} margin={{ top: 12, right: 18, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id="balanceFill" x1="0" x2="0" y1="0" y2="1">
            <stop offset="5%" stopColor="#2563eb" stopOpacity={0.18} />
            <stop offset="95%" stopColor="#2563eb" stopOpacity={0.02} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="6 8" stroke="hsl(var(--border))" vertical={false} />
        <XAxis
          dataKey="day"
          tick={{ fontSize: 11 }}
          tickLine={false}
          axisLine={{ stroke: "hsl(var(--border))" }}
          interval="preserveStartEnd"
        />
        <YAxis
          tick={{ fontSize: 11 }}
          tickFormatter={(value) => formatCompactCurrency(Number(value))}
          tickLine={false}
          axisLine={false}
          width={54}
        />
        <Tooltip content={<PulseTooltip />} />
        <Bar
          dataKey="expectedInflow"
          fill="#38bdf8"
          radius={[4, 4, 0, 0]}
          barSize={8}
          name="Entrada prevista"
        />
        <Bar
          dataKey="realizedInflow"
          fill="#10b981"
          radius={[4, 4, 0, 0]}
          barSize={8}
          name="Entrada realizada"
        />
        <Bar
          dataKey="expectedOutflow"
          fill="#8b5cf6"
          radius={[4, 4, 0, 0]}
          barSize={8}
          name="Saida prevista"
        />
        <Area
          type="monotone"
          dataKey="projectedBalance"
          fill="url(#balanceFill)"
          stroke="#2563eb"
          strokeWidth={2}
          dot={false}
          activeDot={{ r: 4 }}
          name="Saldo projetado"
        />
        <Line
          type="monotone"
          dataKey="projectedBalance"
          stroke="#1d4ed8"
          strokeWidth={2}
          dot={false}
          name="Saldo projetado"
        />
      </ComposedChart>
    </ResponsiveContainer>
  );
}
