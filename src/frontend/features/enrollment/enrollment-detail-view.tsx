"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { ArrowLeft, Clock, ShieldAlert } from "lucide-react";
import { useEffect, useState } from "react";
import { RiskBadge } from "@/components/analytics/risk-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { analyticsService } from "@/services/api/analytics.service";
import type { EnrollmentDetail } from "@/types/api";
import { cn } from "@/lib/utils";

const MiniSparkline = dynamic(
  () => import("@/components/charts/mini-sparkline").then((m) => m.MiniSparkline),
  { ssr: false, loading: () => <Skeleton className="h-10 w-full" /> }
);

export function EnrollmentDetailView({ code }: { code: string }) {
  const [data, setData] = useState<EnrollmentDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    analyticsService
      .getEnrollment(code)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : "Erro"))
      .finally(() => setLoading(false));
  }, [code]);

  if (loading) return <Skeleton className="h-96 w-full rounded-2xl" />;

  if (error || !data) {
    return (
      <Card className="glass-panel">
        <CardContent className="p-8 text-center">
          <p className="text-rose-400">{error ?? "Matrícula não encontrada"}</p>
          <Button className="mt-4" variant="outline" asChild>
            <Link href="/risk-enrollments">Voltar</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  const s = data.summary;
  const financeSpark = data.financialHistory
    .slice()
    .reverse()
    .map((l) => ({ value: l.paidAmount }));

  return (
    <div className="animate-fade-in space-y-8">
      <Button variant="ghost" size="sm" asChild className="-ml-2 text-muted-foreground">
        <Link href="/risk-enrollments">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Matrículas em risco
        </Link>
      </Button>

      <header className="glass-panel flex flex-wrap items-start justify-between gap-6 rounded-2xl p-6">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Visão analítica · LGPD
          </p>
          <h1 className="mt-1 font-mono text-3xl font-bold tracking-tight">{s.enrollmentCode}</h1>
          <p className="mt-1 text-muted-foreground">{s.unitName}</p>
        </div>
        <div className="flex flex-col items-end gap-3">
          <RiskBadge risk={s.churnRisk} />
          <div
            className={cn(
              "flex items-center gap-2 rounded-xl border px-4 py-2",
              s.operationalScore < 40
                ? "border-rose-500/40 bg-rose-500/10 text-rose-300"
                : s.operationalScore < 65
                  ? "border-amber-500/40 bg-amber-500/10 text-amber-300"
                  : "border-emerald-500/40 bg-emerald-500/10 text-emerald-300"
            )}
          >
            <ShieldAlert className="h-4 w-4" />
            <span className="text-sm font-medium">Score operacional</span>
            <span className="text-xl font-bold tabular-nums">{s.operationalScore}</span>
          </div>
        </div>
      </header>

      <section className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {[
          ["Status aluno", s.studentStatus],
          ["Status financeiro", s.paymentStatus],
          ["Retenção", s.retentionLevel],
          ["Tendência", s.trend],
        ].map(([label, value]) => (
          <Card key={label} className="glass-panel border-0">
            <CardHeader className="pb-2">
              <CardTitle className="text-xs font-medium text-muted-foreground">{label}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="font-semibold">{value}</p>
            </CardContent>
          </Card>
        ))}
      </section>

      <section className="grid gap-6 lg:grid-cols-2">
        <Card className="glass-panel border-0">
          <CardHeader>
            <CardTitle className="text-sm">Evolução financeira</CardTitle>
          </CardHeader>
          <CardContent>
            {financeSpark.length > 1 ? (
              <MiniSparkline data={financeSpark} positive />
            ) : (
              <p className="text-sm text-muted-foreground">Histórico insuficiente para gráfico.</p>
            )}
            <p className="mt-4 text-2xl font-bold tabular-nums">
              {s.debtAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
              <span className="ml-2 text-sm font-normal text-muted-foreground">em aberto</span>
            </p>
          </CardContent>
        </Card>

        <Card className="glass-panel border-0">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <Clock className="h-4 w-4" />
              Timeline operacional
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="space-y-4 border-l border-border/60 pl-4">
              {data.financialHistory.slice(0, 6).map((line) => (
                <li key={line.externalId} className="relative">
                  <span className="absolute -left-[21px] top-1 h-2.5 w-2.5 rounded-full bg-primary shadow-[0_0_8px_hsl(var(--primary)/0.6)]" />
                  <p className="text-xs text-muted-foreground">
                    {new Date(line.dueDate).toLocaleDateString("pt-BR")}
                  </p>
                  <p className="text-sm font-medium">{line.paymentStatus}</p>
                  <p className="text-xs text-muted-foreground">
                    Dívida{" "}
                    {line.debtAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                  </p>
                </li>
              ))}
              {s.daysSinceLastPayment != null && (
                <li className="relative text-sm text-amber-400">
                  <span className="absolute -left-[21px] top-1 h-2.5 w-2.5 rounded-full bg-amber-500" />
                  Último vencimento há {s.daysSinceLastPayment} dias
                </li>
              )}
            </ul>
          </CardContent>
        </Card>
      </section>

      <Card className="glass-panel border-0">
        <CardHeader>
          <CardTitle>Parcelas (histórico)</CardTitle>
        </CardHeader>
        <CardContent className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="text-left text-xs text-muted-foreground">
              <tr>
                <th className="pb-2">Ref</th>
                <th className="pb-2">Vencimento</th>
                <th className="pb-2">Dívida</th>
                <th className="pb-2">Pago</th>
                <th className="pb-2">Status</th>
              </tr>
            </thead>
            <tbody>
              {data.financialHistory.map((line) => (
                <tr key={line.externalId} className="border-t border-border/40">
                  <td className="py-2 font-mono text-xs">{line.externalId}</td>
                  <td className="py-2">{new Date(line.dueDate).toLocaleDateString("pt-BR")}</td>
                  <td className="py-2 tabular-nums">
                    {line.debtAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                  </td>
                  <td className="py-2 tabular-nums">
                    {line.paidAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                  </td>
                  <td className="py-2">{line.paymentStatus}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  );
}
