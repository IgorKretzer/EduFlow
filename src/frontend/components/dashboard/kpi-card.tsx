"use client";

import type { LucideIcon } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { TrendBadge } from "@/components/dashboard/trend-badge";
import { cn, formatKpiValue } from "@/lib/utils";
import type { DashboardKpi } from "@/types/api";
import { MiniSparkline } from "@/components/charts/mini-sparkline";

type KpiCardProps = {
  kpi: DashboardKpi;
  icon?: LucideIcon;
  featured?: boolean;
  sparkline?: { value: number }[];
  onClick?: () => void;
  className?: string;
};

function isNegativeMetric(key: string) {
  return /churn|evas|inadimpl|delinq/i.test(key);
}

export function KpiCard({ kpi, icon: Icon, featured, sparkline, onClick, className }: KpiCardProps) {
  const invert = isNegativeMetric(kpi.key);

  return (
    <Card
      role={onClick ? "button" : undefined}
      tabIndex={onClick ? 0 : undefined}
      onClick={onClick}
      onKeyDown={onClick ? (e) => e.key === "Enter" && onClick() : undefined}
      className={cn(
        "overflow-hidden border-0 shadow-card transition-all duration-300",
        onClick && "cursor-pointer hover:-translate-y-0.5 hover:shadow-card-hover focus-visible:ring-2 focus-visible:ring-ring",
        featured
          ? "glow-primary bg-gradient-to-br from-sky-600 via-primary to-emerald-600 text-white"
          : "glass-panel border-0",
        className
      )}
    >
      <CardContent className="p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 flex-1 space-y-2">
            <p
              className={cn(
                "text-xs font-semibold uppercase tracking-wider",
                featured ? "text-blue-100" : "text-muted-foreground"
              )}
            >
              {kpi.label}
            </p>
            <p
              className={cn(
                "font-mono text-3xl font-bold tabular-nums tracking-tight",
                featured ? "text-white" : "text-foreground"
              )}
            >
              {formatKpiValue(kpi.value, kpi.format)}
            </p>
            <TrendBadge
              changePercent={kpi.changePercent}
              invertColors={invert}
              className={featured ? "!bg-white/20 !text-white !ring-white/30" : undefined}
            />
          </div>
          {Icon && (
            <div
              className={cn(
                "flex h-11 w-11 shrink-0 items-center justify-center rounded-xl",
                featured ? "bg-white/20 text-white" : "bg-brand-50 text-brand-600"
              )}
            >
              <Icon className="h-5 w-5" />
            </div>
          )}
        </div>
        {sparkline && sparkline.length > 1 && (
          <div className={cn("mt-4 h-10", featured && "opacity-90")}>
            <MiniSparkline data={sparkline} light={featured} positive={!invert} />
          </div>
        )}
      </CardContent>
    </Card>
  );
}
