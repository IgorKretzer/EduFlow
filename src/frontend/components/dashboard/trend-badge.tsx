import { Minus, TrendingDown, TrendingUp } from "lucide-react";
import { cn, formatPercentChange } from "@/lib/utils";

type TrendBadgeProps = {
  changePercent?: number | null;
  invertColors?: boolean;
  label?: string;
  className?: string;
};

export function TrendBadge({
  changePercent,
  invertColors = false,
  label = "vs período anterior",
  className,
}: TrendBadgeProps) {
  const text = formatPercentChange(changePercent);
  if (!text) {
    return (
      <span className={cn("inline-flex items-center gap-1 text-xs text-muted-foreground", className)}>
        <Minus className="h-3 w-3" /> sem histórico
      </span>
    );
  }

  const up = (changePercent ?? 0) >= 0;
  const good = invertColors ? !up : up;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-semibold",
        good ? "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200/60" : "bg-rose-50 text-rose-700 ring-1 ring-rose-200/60",
        className
      )}
    >
      {up ? <TrendingUp className="h-3.5 w-3.5" /> : <TrendingDown className="h-3.5 w-3.5" />}
      {text} {label}
    </span>
  );
}
