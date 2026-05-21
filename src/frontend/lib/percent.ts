/** Normaliza percentual vindo da API (0.92, 92 ou 92.5) para exibição 0–100. */
export function normalizePercentValue(value: number): number {
  if (!Number.isFinite(value)) return 0;
  if (value > 1 && value <= 100) return value;
  if (value >= 0 && value <= 1) return value * 100;
  return Math.min(100, Math.max(0, value));
}
