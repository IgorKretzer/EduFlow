"use client";

import { useCallback, useEffect, useState } from "react";
import { dashboardService } from "@/services/api/dashboard.service";
import type { DashboardFilter, DashboardSummary } from "@/types/api";

export function useDashboardSummary(filter?: DashboardFilter) {
  const [data, setData] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const summary = await dashboardService.getSummary(filter);
      setData(summary);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao carregar dados");
    } finally {
      setLoading(false);
    }
  }, [filter?.from, filter?.to, filter?.unitId]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, loading, error, reload: load };
}
