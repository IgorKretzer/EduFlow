"use client";

import { useCallback, useEffect, useState } from "react";
import { financeService } from "@/services/api/finance.service";
import type { DashboardFilter, FinanceReceivables } from "@/types/api";

export function useFinanceReceivables(filter?: DashboardFilter) {
  const [data, setData] = useState<FinanceReceivables | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await financeService.getReceivables(filter);
      setData(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao carregar contas a receber");
    } finally {
      setLoading(false);
    }
  }, [filter?.from, filter?.to, filter?.unitId]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, loading, error, reload: load };
}
