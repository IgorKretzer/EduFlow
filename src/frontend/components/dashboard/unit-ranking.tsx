"use client";

import { memo } from "react";
import Link from "next/link";
import { ChevronRight } from "lucide-react";
import type { DelinquencyByUnit } from "@/types/api";
import { cn } from "@/lib/utils";

type UnitRankingProps = {
  units: DelinquencyByUnit[];
  limit?: number;
  onUnitClick?: (unit: DelinquencyByUnit) => void;
};

export const UnitRanking = memo(function UnitRanking({
  units,
  limit = 5,
  onUnitClick,
}: UnitRankingProps) {
  const sorted = [...units]
    .sort((a, b) => b.delinquencyRate - a.delinquencyRate)
    .slice(0, limit);

  return (
    <ul className="space-y-2">
      {sorted.map((u, i) => {
        const pct = (u.delinquencyRate * 100).toFixed(1);
        const critical = u.delinquencyRate >= 0.5;
        return (
          <li key={u.unitCode}>
            <button
              type="button"
              onClick={() => onUnitClick?.(u)}
              className={cn(
                "group flex w-full items-center gap-3 rounded-xl border border-border/60 bg-card/40 px-4 py-3 text-left transition",
                "hover:border-primary/30 hover:bg-primary/5"
              )}
            >
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-muted text-xs font-bold text-muted-foreground">
                {i + 1}
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{u.unitName}</p>
                <div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-muted">
                  <div
                    className={cn(
                      "h-full rounded-full transition-all",
                      critical ? "bg-rose-500 shadow-[0_0_8px_rgba(244,63,94,0.5)]" : "bg-amber-500"
                    )}
                    style={{ width: `${Math.min(100, u.delinquencyRate * 100)}%` }}
                  ></div>
                </div>
              </div>
              <span
                className={cn(
                  "text-sm font-semibold tabular-nums",
                  critical ? "text-rose-400" : "text-amber-400"
                )}
              >
                {pct}%
              </span>
              <ChevronRight className="h-4 w-4 text-muted-foreground opacity-0 transition group-hover:opacity-100" />
            </button>
          </li>
        );
      })}
      <li>
        <Link
          href="/relatorios"
          className="block py-2 text-center text-xs font-medium text-primary hover:underline"
        >
          Ver ranking completo
        </Link>
      </li>
    </ul>
  );
});
