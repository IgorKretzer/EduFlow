"use client";

import { PageHeader } from "@/components/dashboard/page-header";
import { InsightsRail } from "@/components/dashboard/insights-rail";
import { InsightList } from "@/components/dashboard/insight-card";
import { EmptyState } from "@/components/dashboard/empty-state";
import { ScreenCta } from "@/components/dashboard/screen-cta";
import { SyncToolbar } from "@/components/dashboard/sync-toolbar";
import { Skeleton } from "@/components/ui/skeleton";
import { useDashboardSummary } from "@/hooks/use-dashboard-summary";
import { insightDeepLink, severityLabel } from "@/utils/insight-labels";
import type { Insight } from "@/types/api";

function groupBySeverity(insights: Insight[]) {
  const urgent = insights.filter((i) => /critical|high/i.test(i.severity));
  const attention = insights.filter((i) => /warning|medium/i.test(i.severity));
  const opportunity = insights.filter((i) => /success|low/i.test(i.severity));
  const other = insights.filter(
    (i) => !urgent.includes(i) && !attention.includes(i) && !opportunity.includes(i)
  );
  return { urgent, attention, opportunity, other };
}

export default function InsightsPage() {
  const { data, loading, error, reload } = useDashboardSummary();

  if (loading) return <Skeleton className="h-96 w-full rounded-xl" />;

  if (error || !data) {
    return (
      <EmptyState
        title="Recomendações indisponíveis"
        description={error ?? "Sincronize os dados da escola para gerar recomendações."}
        actionLabel="Recarregar"
        onAction={reload}
      />
    );
  }

  const groups = groupBySeverity(data.insights);
  const topUrgent = groups.urgent[0] ?? groups.attention[0];

  return (
    <div className="animate-fade-in space-y-8">
      <PageHeader
        breadcrumbs={["EduFlow", "Recomendações"]}
        title="Central de recomendações"
        description="Problema, evidência e o que fazer — com atalho para a tela certa."
        actions={<SyncToolbar onSynced={reload} />}
      />

      {data.insights.length === 0 ? (
        <EmptyState
          title="Nenhuma recomendação ainda"
          description="Após sincronizar alunos e financeiro, o sistema prioriza o que exige ação da direção."
          actionLabel="Ir ao painel executivo"
          onAction={() => (window.location.href = "/")}
        />
      ) : (
        <>
          {topUrgent && (
            <ScreenCta
              title={`Prioridade: ${severityLabel(topUrgent.severity)}`}
              description={topUrgent.recommendedAction ?? topUrgent.message}
              primary={{
                label: "Ver o que fazer",
                href: insightDeepLink(topUrgent),
              }}
            />
          )}

          <div className="grid gap-8 lg:grid-cols-12">
            <div className="lg:col-span-5 lg:min-h-[400px]">
              <InsightsRail insights={data.insights} />
            </div>
            <div className="space-y-6 lg:col-span-7">
              {groups.urgent.length > 0 && (
                <section>
                  <h2 className="mb-3 text-sm font-semibold text-rose-500 dark:text-rose-400">
                    Urgente ({groups.urgent.length})
                  </h2>
                  <InsightList insights={groups.urgent} />
                </section>
              )}
              {groups.attention.length > 0 && (
                <section>
                  <h2 className="mb-3 text-sm font-semibold text-amber-600 dark:text-amber-400">
                    Atenção ({groups.attention.length})
                  </h2>
                  <InsightList insights={groups.attention} />
                </section>
              )}
              {groups.opportunity.length > 0 && (
                <section>
                  <h2 className="mb-3 text-sm font-semibold text-emerald-600 dark:text-emerald-400">
                    Oportunidade ({groups.opportunity.length})
                  </h2>
                  <InsightList insights={groups.opportunity} />
                </section>
              )}
              {groups.other.length > 0 && <InsightList insights={groups.other} />}
            </div>
          </div>

          <ScreenCta
            title="Operação no detalhe"
            description="Converta recomendações em ação nas matrículas ou na área financeira."
            primary={{ label: "Matrículas em risco", href: "/risk-enrollments" }}
            secondary={{ label: "Financeiro", href: "/financeiro" }}
          />
        </>
      )}
    </div>
  );
}
