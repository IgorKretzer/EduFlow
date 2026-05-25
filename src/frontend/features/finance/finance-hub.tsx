"use client";

import { useCallback, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Loader2 } from "lucide-react";
import { SyncToolbar } from "@/components/dashboard/sync-toolbar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { CashFlowView } from "@/features/finance/cash-flow-view";
import { FinancialPulseView } from "@/features/finance/financial-pulse-view";
import { ReceivablesView } from "@/features/finance/receivables-view";
import { useFinanceReceivables } from "@/hooks/use-finance-receivables";

const TABS = [
  { id: "pulso", label: "Pulso Financeiro" },
  { id: "recebiveis", label: "Contas a Receber" },
  { id: "fluxo", label: "Fluxo de Caixa" },
] as const;

export type FinanceTab = (typeof TABS)[number]["id"];

export function FinanceHub() {
  const [refreshSignal, setRefreshSignal] = useState(0);
  const { data, loading, error, reload } = useFinanceReceivables();
  const searchParams = useSearchParams();
  const router = useRouter();
  const tabParam = searchParams.get("aba");
  const activeTab: FinanceTab =
    tabParam === "fluxo" || tabParam === "recebiveis" ? tabParam : "pulso";

  const setTab = useCallback(
    (value: string) => {
      const params = new URLSearchParams(searchParams.toString());
      if (value === "pulso") params.delete("aba");
      else params.set("aba", value);
      const q = params.toString();
      router.replace(q ? `/financeiro?${q}` : "/financeiro", { scroll: false });
    },
    [router, searchParams]
  );

  const onSynced = useCallback(() => {
    reload();
    setRefreshSignal((n) => n + 1);
  }, [reload]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Financeiro</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Visão da saúde financeira com base nas parcelas sincronizadas do ERP: vencidos, a
            vencer e recebimentos por data de vencimento.
          </p>
        </div>
        <SyncToolbar onSynced={onSynced} />
      </div>

      {error && (
        <p className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </p>
      )}

      <Tabs value={activeTab} onValueChange={setTab}>
        <TabsList>
          {TABS.map((t) => (
            <TabsTrigger key={t.id} value={t.id}>
              {t.label}
            </TabsTrigger>
          ))}
        </TabsList>

        <TabsContent value="pulso">
          {loading || !data ? (
            <div className="flex items-center justify-center gap-2 py-24 text-muted-foreground">
              <Loader2 className="h-5 w-5 animate-spin" />
              Carregando pulso financeiro...
            </div>
          ) : (
            <FinancialPulseView receivables={data} refreshSignal={refreshSignal} />
          )}
        </TabsContent>

        <TabsContent value="recebiveis">
          {loading || !data ? (
            <div className="flex items-center justify-center gap-2 py-24 text-muted-foreground">
              <Loader2 className="h-5 w-5 animate-spin" />
              Carregando contas a receber…
            </div>
          ) : (
            <ReceivablesView data={data} />
          )}
        </TabsContent>

        <TabsContent value="fluxo">
          <CashFlowView refreshSignal={refreshSignal} />
        </TabsContent>
      </Tabs>

      {!loading && data && activeTab === "recebiveis" && (
        <p className="text-center text-[11px] text-muted-foreground">
          Dados de {data.summary.peopleCount} matrícula(s) com parcelas em aberto no staging
          financeiro.
        </p>
      )}
    </div>
  );
}
