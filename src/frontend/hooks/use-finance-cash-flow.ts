"use client";

import { useCallback, useEffect, useState } from "react";
import { financeService } from "@/services/api/finance.service";
import type { DashboardFilter, FinanceCashFlow } from "@/types/api";

export function useFinanceCashFlow(year: number, month: number, filter?: DashboardFilter) {
  const [data, setData] = useState<FinanceCashFlow | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await financeService.getCashFlow(year, month, filter);
      setData(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao carregar fluxo de caixa");
    } finally {
      setLoading(false);
    }
  }, [year, month, filter?.from, filter?.to, filter?.unitId]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, loading, error, reload: load };
}
