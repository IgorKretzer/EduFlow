"use client";

import { useMemo, useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { CashFlowDayTooltip } from "@/components/finance/cash-flow-day-tooltip";
import type { FinanceCashFlowDay } from "@/types/api";
import { formatCompactCurrency } from "@/lib/format-currency";

const WEEKDAYS = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];

type Props = {
  days: FinanceCashFlowDay[];
  year: number;
  month: number;
  onMonthChange: (year: number, month: number) => void;
};

export function CashFlowCalendar({ days, year, month, onMonthChange }: Props) {
  const [view, setView] = useState<"day" | "week">("day");
  const [hoveredDay, setHoveredDay] = useState<number | null>(null);
  const monthLabel = new Date(year, month, 1).toLocaleDateString("pt-BR", {
    month: "long",
    year: "numeric",
  });

  const leadingBlanks = useMemo(() => new Date(year, month, 1).getDay(), [year, month]);
  const cells = useMemo(() => {
    const byDay = new Map(days.map((d) => [d.day, d]));
    const count = new Date(year, month + 1, 0).getDate();
    return Array.from({ length: count }, (_, i) => byDay.get(i + 1));
  }, [days, year, month]);

  function shiftMonth(delta: number) {
    const d = new Date(year, month + delta, 1);
    onMonthChange(d.getFullYear(), d.getMonth());
  }

  return (
    <div className="rounded-xl border bg-card shadow-card">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b px-4 py-3">
        <div className="flex items-center gap-2">
          <Button variant="outline" size="icon" onClick={() => shiftMonth(-1)} aria-label="Mês anterior">
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button variant="outline" size="icon" onClick={() => shiftMonth(1)} aria-label="Próximo mês">
            <ChevronRight className="h-4 w-4" />
          </Button>
          <h2 className="text-lg font-bold capitalize text-foreground">{monthLabel}</h2>
        </div>
        <div className="flex gap-1">
          <Button
            variant={view === "day" ? "secondary" : "ghost"}
            size="sm"
            onClick={() => setView("day")}
          >
            Dia
          </Button>
          <Button
            variant={view === "week" ? "secondary" : "ghost"}
            size="sm"
            onClick={() => setView("week")}
          >
            Semana
          </Button>
        </div>
      </div>
      <div className="p-4">
        <div className="mb-2 grid grid-cols-7 gap-0">
          {WEEKDAYS.map((w) => (
            <div key={w} className="text-center text-sm font-medium text-muted-foreground">
              {w}
            </div>
          ))}
        </div>
        <div className="grid grid-cols-7 overflow-hidden rounded-md border border-border">
          {Array.from({ length: leadingBlanks }).map((_, i) => (
            <div
              key={`blank-${i}`}
              className="flex h-[4.5rem] flex-col border-l border-t border-border bg-muted/40 p-1"
            />
          ))}
          {cells.map((day, idx) => {
            const dayNum = idx + 1;
            const operational = day?.operationalBalance ?? 0;
            const hasMovement =
              day &&
              (day.inflow > 0 ||
                day.outflow > 0 ||
                Math.abs(operational) > 0.01);
            const positive = operational >= 0;
            const showPill = view === "day" && hasMovement;

            return (
              <button
                key={dayNum}
                type="button"
                className="relative flex h-[4.5rem] flex-col items-start border-l border-t border-border p-1 text-left transition-colors hover:bg-muted/50"
                onMouseEnter={() => day && setHoveredDay(dayNum)}
                onMouseLeave={() => setHoveredDay(null)}
                onFocus={() => day && setHoveredDay(dayNum)}
                onBlur={() => setHoveredDay(null)}
              >
                <span className="text-xs font-medium">{dayNum}</span>
                {showPill && (
                  <span
                    className={`mt-auto w-full truncate rounded-full px-1 py-0.5 text-[10px] font-semibold ${
                      positive
                        ? "bg-sky-100 text-sky-700"
                        : "bg-violet-100 text-violet-700"
                    }`}
                  >
                    {positive ? "" : "-"}
                    {formatCompactCurrency(Math.abs(operational))}
                  </span>
                )}
                {hoveredDay === dayNum && day && <CashFlowDayTooltip day={day} />}
              </button>
            );
          })}
        </div>
        <p className="mt-3 text-[11px] text-muted-foreground">
          Passe o mouse no dia para ver saldo inicial, entradas, saídas e saldo final. Pill = saldo
          operacional do dia.
        </p>
      </div>
    </div>
  );
}
