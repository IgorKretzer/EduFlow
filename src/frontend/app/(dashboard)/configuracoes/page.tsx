"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { PageHeader } from "@/components/dashboard/page-header";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { SettingsView } from "@/features/settings/settings-view";
import { OpsPanel } from "@/features/settings/ops-panel";
import { Skeleton } from "@/components/ui/skeleton";
import { showOpsPanel } from "@/lib/feature-flags";

function ConfiguracoesContent() {
  const searchParams = useSearchParams();
  const opsTab = showOpsPanel && searchParams.get("tab") === "operacoes";
  const defaultTab = opsTab ? "operacoes" : "erp";

  if (!showOpsPanel) {
    return <SettingsView />;
  }

  return (
    <Tabs defaultValue={defaultTab} className="w-full">
      <TabsList>
        <TabsTrigger value="erp">Integração ERP</TabsTrigger>
        <TabsTrigger value="operacoes">Painel operacional (suporte)</TabsTrigger>
      </TabsList>
      <TabsContent value="erp">
        <SettingsView />
      </TabsContent>
      <TabsContent value="operacoes">
        <OpsPanel />
      </TabsContent>
    </Tabs>
  );
}

export default function ConfiguracoesPage() {
  return (
    <div className="animate-fade-in space-y-6">
      <PageHeader
        breadcrumbs={["EduFlow", "Configurações"]}
        title="Configurações"
        description="Conexão com o Sponte e frequência de atualização dos dados da escola."
      />
      <Suspense fallback={<Skeleton className="h-64 w-full rounded-xl" />}>
        <ConfiguracoesContent />
      </Suspense>
    </div>
  );
}
