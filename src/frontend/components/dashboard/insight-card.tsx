import Link from "next/link";
import { AlertTriangle, ArrowRight, Info } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { insightDeepLink, severityLabel } from "@/utils/insight-labels";
import type { Insight } from "@/types/api";

function severityVariant(severity: string) {
  const s = severity.toLowerCase();
  if (s === "critical" || s === "high") return "danger" as const;
  if (s === "medium" || s === "warning") return "warning" as const;
  return "default" as const;
}

function SeverityIcon({ severity }: { severity: string }) {
  const s = severity.toLowerCase();
  if (s === "critical" || s === "high") return <AlertTriangle className="h-4 w-4" />;
  if (s === "medium" || s === "warning") return <AlertTriangle className="h-4 w-4" />;
  return <Info className="h-4 w-4" />;
}

export function InsightCard({ insight, className }: { insight: Insight; className?: string }) {
  const href = insightDeepLink(insight);
  const D = "div" as keyof JSX.IntrinsicElements;

  return (
    <Card className={cn("transition-shadow hover:shadow-card-hover", className)}>
      <CardContent className="p-4">
        <D className="flex items-start gap-3">
          <D className="mt-0.5 rounded-lg bg-primary/10 p-2 text-primary">
            <SeverityIcon severity={insight.severity} />
          </D>
          <D className="min-w-0 flex-1 space-y-2">
            <D className="flex flex-wrap items-center gap-2">
              <p className="font-medium text-foreground">{insight.title}</p>
              <Badge variant={severityVariant(insight.severity)}>{severityLabel(insight.severity)}</Badge>
            </D>
            <p className="text-sm leading-relaxed text-muted-foreground">{insight.message}</p>
            {insight.recommendedAction && (
              <p className="rounded-md border border-primary/20 bg-primary/5 px-3 py-2 text-sm text-foreground">
                <span className="font-medium">O que fazer: </span>
                {insight.recommendedAction}
              </p>
            )}
            <D className="flex flex-wrap items-center justify-between gap-2 pt-1">
              <p className="text-xs text-muted-foreground">
                {new Date(insight.generatedAt).toLocaleString("pt-BR")}
              </p>
              <Button variant="ghost" size="sm" className="h-8 gap-1 px-2" asChild>
                <Link href={href}>
                  Ver o que fazer
                  <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </Button>
            </D>
          </D>
        </D>
      </CardContent>
    </Card>
  );
}

export function InsightList({ insights }: { insights: Insight[] }) {
  if (insights.length === 0) {
    return (
      <p className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
        Nenhuma recomendação no momento. Após a sincronização com o ERP, os insights aparecem aqui.
      </p>
    );
  }

  const D = "div" as keyof JSX.IntrinsicElements;

  return (
    <D className="space-y-3">
      {insights.map((i) => (
        <InsightCard key={`${i.code}-${i.generatedAt}`} insight={i} />
      ))}
    </D>
  );
}
