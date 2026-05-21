"use client";

import { AlertTriangle, Sparkles, TrendingUp } from "lucide-react";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { insightDeepLink, severityLabel } from "@/utils/insight-labels";
import { cn } from "@/lib/utils";
import type { Insight } from "@/types/api";

function severityStyles(severity: string) {
  const s = severity.toLowerCase();
  if (s === "critical" || s === "high")
    return {
      card: "border-rose-500/30 bg-rose-500/10 dark:bg-rose-500/10",
      badge: "danger" as const,
      icon: AlertTriangle,
    };
  if (s === "warning" || s === "medium")
    return {
      card: "border-amber-500/30 bg-amber-500/10 dark:bg-amber-500/10",
      badge: "warning" as const,
      icon: AlertTriangle,
    };
  if (s === "success" || s === "low")
    return {
      card: "border-emerald-500/30 bg-emerald-500/10 dark:bg-emerald-500/10",
      badge: "success" as const,
      icon: TrendingUp,
    };
  return {
    card: "border-border bg-muted/40",
    badge: "default" as const,
    icon: Sparkles,
  };
}

export function InsightsRail({ insights }: { insights: Insight[] }) {
  if (insights.length === 0) {
    return (
      <div className="flex h-full flex-col items-center justify-center rounded-2xl border border-dashed border-border bg-card/50 p-8 text-center">
        <Sparkles className="mb-3 h-8 w-8 text-primary" />
        <p className="text-sm text-muted-foreground">
          As recomendações aparecem depois que os dados da escola forem sincronizados.
        </p>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col gap-3">
      <p className="text-sm font-semibold text-foreground">Prioridades para hoje</p>
      <div className="flex-1 space-y-3 overflow-y-auto pr-1">
        {insights.slice(0, 6).map((insight) => {
          const style = severityStyles(insight.severity);
          const Icon = style.icon;
          return (
            <article
              key={`${insight.code}-${insight.generatedAt}`}
              className={cn("rounded-xl border p-4 shadow-sm transition hover:shadow-md", style.card)}
            >
              <div className="flex items-start gap-3">
                <Icon className="mt-0.5 h-4 w-4 shrink-0 text-foreground/80" />
                <div className="min-w-0 space-y-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="text-sm font-semibold">{insight.title}</h3>
                    <Badge variant={style.badge}>{severityLabel(insight.severity)}</Badge>
                  </div>
                  <p className="text-sm leading-relaxed text-muted-foreground">{insight.message}</p>
                  {insight.recommendedAction && (
                    <p className="text-xs font-medium leading-relaxed text-foreground">
                      <span className="text-muted-foreground">O que fazer: </span>
                      {insight.recommendedAction}
                    </p>
                  )}
                  <Link
                    href={insightDeepLink(insight)}
                    className="inline-flex text-xs font-medium text-primary hover:underline"
                  >
                    Ver detalhes →
                  </Link>
                </div>
              </div>
            </article>
          );
        })}
      </div>
      <Button variant="outline" className="w-full" asChild>
        <Link href="/insights">Ver todas as recomendações</Link>
      </Button>
    </div>
  );
}
