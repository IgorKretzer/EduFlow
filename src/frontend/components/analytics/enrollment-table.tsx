"use client";

import { memo, useCallback } from "react";
import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { RiskBadge } from "@/components/analytics/risk-badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import type { EnrollmentPage, EnrollmentSummary } from "@/types/api";
import { cn } from "@/lib/utils";

const PAGE_SIZES = [10, 25, 50] as const;

type EnrollmentTableProps = {
  page: EnrollmentPage | null;
  loading: boolean;
  search: string;
  pageSize: number;
  onSearchChange: (v: string) => void;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  riskFilter?: string;
  onRiskFilterChange?: (risk: string) => void;
};

function Row({ row }: { row: EnrollmentSummary }) {
  return (
    <tr className="border-t border-border/50 transition hover:bg-muted/30">
      <td className="px-4 py-3">
        <Link
          href={`/risk-enrollments/${encodeURIComponent(row.enrollmentCode)}`}
          className="font-mono text-sm font-semibold text-primary hover:underline"
        >
          {row.enrollmentCode}
        </Link>
      </td>
      <td className="px-4 py-3 text-sm">{row.unitName || "—"}</td>
      <td className="px-4 py-3 text-sm">{row.paymentStatus}</td>
      <td className="px-4 py-3">
        <RiskBadge risk={row.churnRisk} />
      </td>
      <td className="px-4 py-3 text-sm">{row.retentionLevel}</td>
      <td className="px-4 py-3">
        <span
          className={cn(
            "inline-flex rounded-full px-2 py-0.5 text-xs font-bold tabular-nums",
            row.operationalScore < 40
              ? "bg-rose-500/15 text-rose-400"
              : row.operationalScore < 65
                ? "bg-amber-500/15 text-amber-400"
                : "bg-emerald-500/15 text-emerald-400"
          )}
        >
          {row.operationalScore}
        </span>
      </td>
      <td className="max-w-[180px] truncate px-4 py-3 text-xs text-muted-foreground">
        {row.trend}
      </td>
    </tr>
  );
}

const TableRow = memo(Row);

export const EnrollmentTable = memo(function EnrollmentTable({
  page,
  loading,
  search,
  pageSize,
  onSearchChange,
  onPageChange,
  onPageSizeChange,
  riskFilter,
  onRiskFilterChange,
}: EnrollmentTableProps) {
  const items = page?.items ?? [];
  const totalCount = page?.totalCount ?? 0;
  const resolvedPageSize = page?.pageSize ?? pageSize;
  const totalPages = page ? Math.max(1, Math.ceil(totalCount / resolvedPageSize)) : 1;
  const current = page?.page ?? 1;

  const go = useCallback(
    (p: number) => onPageChange(Math.min(totalPages, Math.max(1, p))),
    [onPageChange, totalPages]
  );

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <Input
          placeholder="Buscar matrícula ou unidade…"
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          className="max-w-xs bg-card/60"
        />
        {onRiskFilterChange && (
          <select
            value={riskFilter ?? ""}
            onChange={(e) => onRiskFilterChange(e.target.value)}
            className="h-10 rounded-lg border border-border bg-card/60 px-3 text-sm"
          >
            <option value="">Todos os riscos</option>
            <option value="Alto">Alto</option>
            <option value="Médio">Médio</option>
            <option value="Baixo">Baixo</option>
          </select>
        )}
        <select
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          className="h-10 rounded-lg border border-border bg-card/60 px-3 text-sm"
        >
          {PAGE_SIZES.map((s) => (
            <option key={s} value={s}>
              {s} / página
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-hidden rounded-2xl border border-border/60 bg-card/30 shadow-card backdrop-blur-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-border/60 bg-muted/20 text-xs uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-3">Matrícula</th>
                <th className="px-4 py-3">Unidade</th>
                <th className="px-4 py-3">Financeiro</th>
                <th className="px-4 py-3">Churn</th>
                <th className="px-4 py-3">Retenção</th>
                <th className="px-4 py-3">Score</th>
                <th className="px-4 py-3">Tendência</th>
              </tr>
            </thead>
            <tbody>
              {loading &&
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i}>
                    <td colSpan={7} className="p-0">
                      <Skeleton className="m-2 h-10 w-full" />
                    </td>
                  </tr>
                ))}
              {!loading &&
                items.map((row) => <TableRow key={row.enrollmentCode} row={row} />)}
              {!loading && items.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center text-muted-foreground">
                    Nenhuma matrícula encontrada.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-muted-foreground">
        <span>
          {page
            ? `${totalCount} matrícula(s) — página ${current} de ${totalPages}`
            : "—"}
        </span>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={current <= 1 || loading} onClick={() => go(current - 1)}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={current >= totalPages || loading}
            onClick={() => go(current + 1)}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
});
