"use client";

import { useCallback, useEffect, useState } from "react";
import { opsService } from "@/services/api/ops.service";
import type { OpsStatus } from "@/types/api";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

function statusBadge(status: string) {
  if (status === "healthy") return "success" as const;
  if (status === "degraded") return "warning" as const;
  return "danger" as const;
}

export function OpsPanel() {
  const [data, setData] = useState<OpsStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setData(await opsService.getStatus());
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro");
    }
  }, []);

  useEffect(() => {
    load();
    const id = setInterval(load, 5000);
    return () => clearInterval(id);
  }, [load]);

  if (error) return <p className="text-sm text-rose-600">{error}</p>;
  if (!data) return <Skeleton className="h-64 w-full rounded-xl" />;

  return (
    <div className="space-y-6">
      <p className="rounded-lg border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-muted-foreground">
        Painel técnico para suporte — não exibido para gestores em produção.
      </p>
      <p className="text-xs text-muted-foreground">
        Atualizado: {new Date(data.checkedAtUtc).toLocaleString("pt-BR")}
      </p>
      <div className="grid gap-3 md:grid-cols-2">
        {data.components.map((c) => (
          <div key={c.id} className="rounded-lg border bg-card p-4 shadow-sm">
            <div className="flex items-center justify-between gap-2">
              <p className="font-medium text-sm">{c.name}</p>
              <Badge variant={statusBadge(c.status)}>{c.status}</Badge>
            </div>
            <p className="mt-2 text-xs text-muted-foreground">{c.detail}</p>
          </div>
        ))}
      </div>
      <div className="overflow-hidden rounded-xl border">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 text-left text-xs text-muted-foreground">
            <tr>
              <th className="px-4 py-2">Fila</th>
              <th className="px-4 py-2">Msgs</th>
              <th className="px-4 py-2">Consumidores</th>
            </tr>
          </thead>
          <tbody>
            {data.queues.map((q) => (
              <tr key={q.name} className="border-t">
                <td className="px-4 py-2 font-mono">{q.name}</td>
                <td className="px-4 py-2">{q.messages}</td>
                <td className={cn("px-4 py-2", q.consumers === 0 && "text-rose-600")}>
                  {q.consumers}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex flex-wrap gap-2">
        {data.links.map((l) => (
          <a
            key={l.url}
            href={l.url}
            target="_blank"
            rel="noopener noreferrer"
            className="rounded-md border px-3 py-1.5 text-xs text-primary hover:bg-accent"
          >
            {l.label} ↗
          </a>
        ))}
      </div>
    </div>
  );
}
