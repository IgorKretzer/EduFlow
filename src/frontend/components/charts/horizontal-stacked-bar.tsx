"use client";

import { formatCurrency } from "@/lib/format-currency";

export type StackedBarItem = {
  id: string;
  label: string;
  amount: number;
  color: string;
};

type Props = {
  items: StackedBarItem[];
  title?: string;
};

export function HorizontalStackedBar({ items, title }: Props) {
  const total = items.reduce((s, i) => s + i.amount, 0);

  return (
    <div className="rounded-xl border bg-card shadow-card">
      {title && (
        <div className="border-b px-4 py-3">
          <h3 className="text-base font-semibold text-muted-foreground">{title}</h3>
        </div>
      )}
      <div className="p-4">
        {total <= 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Sem dados no período.</p>
        ) : (
          <>
            <div className="flex h-3 overflow-hidden rounded-full">
              {items.map((item) => (
                <div
                  key={item.id}
                  className="h-full min-w-[2px] transition-all"
                  style={{
                    width: `${(item.amount / total) * 100}%`,
                    backgroundColor: item.color,
                  }}
                  title={`${item.label}: ${formatCurrency(item.amount)}`}
                />
              ))}
            </div>
            <ul className="mt-4 space-y-2">
              {items.map((item) => (
                <li key={item.id} className="flex items-center justify-between gap-2 text-sm">
                  <span className="flex items-center gap-2">
                    <span
                      className="inline-block h-2.5 w-2.5 shrink-0 rounded-full"
                      style={{ backgroundColor: item.color }}
                    />
                    <span className="text-muted-foreground">{item.label}</span>
                  </span>
                  <span className="tabular-nums font-medium">{formatCurrency(item.amount)}</span>
                </li>
              ))}
            </ul>
          </>
        )}
      </div>
    </div>
  );
}
