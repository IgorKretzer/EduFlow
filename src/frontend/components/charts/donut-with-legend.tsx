"use client";

import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import { formatCurrency } from "@/lib/format-currency";

export type DonutSlice = {
  id: string;
  label: string;
  value: number;
  color: string;
  secondaryValue?: number;
};

type Props = {
  data: DonutSlice[];
  title?: string;
  centerLabel?: string;
  height?: number;
};

export function DonutWithLegend({ data, title, centerLabel, height = 260 }: Props) {
  const total = data.reduce((s, d) => s + d.value, 0);

  return (
    <div className="rounded-xl border bg-card shadow-card">
      {title && (
        <div className="border-b px-4 py-3">
          <h3 className="text-base font-semibold text-muted-foreground">{title}</h3>
        </div>
      )}
      <div className="p-4">
        <div style={{ height }} className="w-full min-w-0">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={data}
                dataKey="value"
                nameKey="label"
                innerRadius="58%"
                outerRadius="88%"
                paddingAngle={2}
                stroke="#fff"
                strokeWidth={3}
              >
                {data.map((entry) => (
                  <Cell key={entry.id} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value: number, name: string) => [
                  formatCurrency(Number(value)),
                  name,
                ]}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>
        <div className="mt-3 flex flex-wrap items-center justify-center gap-x-3 gap-y-2 border-t pt-3">
          {data.map((slice) => (
            <div key={slice.id} className="flex items-center gap-2">
              <span
                className="h-3 w-3 shrink-0 rounded-sm"
                style={{ backgroundColor: slice.color }}
              />
              <div>
                <p className="text-[11px] text-muted-foreground">{slice.label}</p>
                <p className="text-xs font-semibold tabular-nums text-foreground">
                  {formatCurrency(slice.value)}
                </p>
                {slice.secondaryValue !== undefined && (
                  <p className="text-[10px] tabular-nums text-muted-foreground">
                    {formatCurrency(slice.secondaryValue)}
                  </p>
                )}
              </div>
            </div>
          ))}
        </div>
        {centerLabel && (
          <p className="mt-2 text-center text-xs text-muted-foreground">{centerLabel}</p>
        )}
        {total > 0 && !centerLabel && (
          <p className="mt-2 text-center text-xs text-muted-foreground">
            Total: {formatCurrency(total)}
          </p>
        )}
      </div>
    </div>
  );
}
