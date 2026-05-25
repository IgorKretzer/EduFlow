"use client";

import { useCallback, useEffect, useState } from "react";
import { financeService } from "@/services/api/finance.service";
import type { DashboardFilter, FinancePulse } from "@/types/api";

export type FinanceDataBasis = "cash" | "due" | "competence";

export function useFinancePulse(
  year: number,
  month: number,
  dataBasis: FinanceDataBasis,
  filter?: DashboardFilter
) {
  const [data, setData] = useState<FinancePulse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await financeService.getPulse(year, month, dataBasis, filter);
      setData(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao carregar pulso financeiro");
    } finally {
      setLoading(false);
    }
  }, [year, month, dataBasis, filter?.from, filter?.to, filter?.unitId]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, loading, error, reload: load };
}
