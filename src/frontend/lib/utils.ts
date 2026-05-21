import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import { normalizePercentValue } from "@/lib/percent";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatKpiValue(value: number, format: string): string {
  switch (format) {
    case "currency":
      return value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
    case "percent":
      return `${normalizePercentValue(value).toFixed(1)}%`;
    case "integer":
      return Math.round(value).toLocaleString("pt-BR");
    default:
      return value.toLocaleString("pt-BR", { maximumFractionDigits: 2 });
  }
}

export function formatPercentChange(change?: number | null): string | null {
  if (change === undefined || change === null) return null;
  const sign = change >= 0 ? "+" : "";
  return `${sign}${change.toFixed(1)}%`;
}
