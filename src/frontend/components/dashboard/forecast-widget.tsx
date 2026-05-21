"use client";

import { memo } from "react";
import dynamic from "next/dynamic";
import { TrendingUp } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import type { TimeSeriesPoint } from "@/types/api";

const MiniSparkline = dynamic(
  () => import("@/components/charts/mini-sparkline").then((m) => m.MiniSparkline),
  { ssr: false, loading: () => <Skeleton className="h-12 w-full" /> }
);

type ForecastWidgetProps = {
  revenue: TimeSeriesPoint[];
};

export const ForecastWidget = memo(function ForecastWidget({ revenue }: ForecastWidgetProps) {
  const values = revenue.map((p) => p.value);
  const last = values.at(-1) ?? 0;
  const prev = values.at(-2) ?? last;
  const growth = prev > 0 ? ((last - prev) / prev) * 100 : 0;
  const forecast = last * (1 + growth / 100);

  const spark = revenue.map((p) => ({ value: p.value }));
  if (forecast > 0) spark.push({ value: forecast });

  return (
    <div className="glass-panel rounded-2xl p-5">
      <div className="mb-3 flex items-center justify-between">
        <div>
          <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
            Previsão receita
          </p>
          <p className="text-2xl font-bold tabular-nums">
            {forecast.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
          </p>
        </div>
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/15 text-primary">
          <TrendingUp className="h-5 w-5" />
        </div>
      </div>
      <p className="mb-3 text-xs text-muted-foreground">
        Projeção linear com base na tendência do período ({growth >= 0 ? "+" : ""}
        {growth.toFixed(1)}%)
      </p>
      <MiniSparkline data={spark} positive={growth >= 0} />
    </div>
  );
});
