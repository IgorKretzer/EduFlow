"use client";

import { useState } from "react";
import { Loader2, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { syncService, type SyncEntityType } from "@/services/api/sync.service";

type SyncToolbarProps = {
  onSynced?: () => void;
};

export function SyncToolbar({ onSynced }: SyncToolbarProps) {
  const [loading, setLoading] = useState<SyncEntityType | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  async function run(type: SyncEntityType) {
    setLoading(type);
    setMessage(null);
    try {
      const r = await syncService.trigger(type);
      const labels: Record<SyncEntityType, string> = {
        students: "Alunos",
        financial: "Financeiro (receber)",
        payables: "Contas a pagar",
        categories: "Categorias",
        "financial-full": "Financeiro completo",
        contracts: "Matrículas",
      };
      const ok = r.status === "completed";
      const detail = r.errorMessage ? ` — ${r.errorMessage}` : "";
      setMessage(
        ok
          ? `${labels[type]}: ${r.recordsProcessed} registro(s) importados`
          : `${labels[type]}: ${r.status}${detail}`
      );
      if (ok) setTimeout(() => onSynced?.(), 2000);
    } catch (e) {
      const msg = e instanceof Error ? e.message : "Falha no sync";
      setMessage(
        msg.includes("Tempo esgotado")
          ? `${msg} — o sync com o ERP pode levar alguns minutos; aguarde ou tente só "Financeiro (receber)".`
          : msg
      );
    } finally {
      setLoading(null);
    }
  }

  return (
    <div className="flex flex-col items-end gap-2">
      <div className="flex flex-wrap gap-2">
        {(
          [
            ["students", "Alunos"],
            ["financial-full", "Financeiro"],
            ["contracts", "Matrículas"],
          ] as const
        ).map(([type, label]) => (
          <Button
            key={type}
            variant={type === "contracts" ? "default" : "outline"}
            size="sm"
            disabled={!!loading}
            onClick={() => run(type)}
          >
            {loading === type ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4" />
            )}
            Sync {label}
          </Button>
        ))}
      </div>
      {message && <p className="text-xs text-muted-foreground">{message}</p>}
    </div>
  );
}
