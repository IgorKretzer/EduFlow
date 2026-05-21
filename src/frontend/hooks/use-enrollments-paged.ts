"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { analyticsService } from "@/services/api/analytics.service";
import type { EnrollmentQuery } from "@/services/api/enrollment-query";
import type { EnrollmentPage } from "@/types/api";
import { useDebounce } from "@/hooks/use-debounce";

const cache = new Map<string, { at: number; data: EnrollmentPage }>();
const CACHE_TTL_MS = 30_000;
const CACHE_VERSION = "v2";

function cacheKey(params: EnrollmentQuery) {
  return `${CACHE_VERSION}:${JSON.stringify(params)}`;
}

export function useEnrollmentsPaged(params: EnrollmentQuery) {
  const debouncedSearch = useDebounce(params.search ?? "", 400);
  const query: EnrollmentQuery = { ...params, search: debouncedSearch || undefined };

  const [data, setData] = useState<EnrollmentPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const load = useCallback(async () => {
    const key = cacheKey(query);
    const hit = cache.get(key);
    if (hit && Date.now() - hit.at < CACHE_TTL_MS) {
      setData(hit.data);
      setLoading(false);
      return;
    }

    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;

    setLoading(true);
    setError(null);
    try {
      const page = await analyticsService.listEnrollments(query);
      if (ac.signal.aborted) return;
      cache.set(key, { at: Date.now(), data: page });
      setData(page);
    } catch (e) {
      if (ac.signal.aborted) return;
      setError(e instanceof Error ? e.message : "Erro ao carregar matrículas");
    } finally {
      if (!ac.signal.aborted) setLoading(false);
    }
  }, [
    query.page,
    query.pageSize,
    query.search,
    query.unitCode,
    query.risk,
    query.riskOnly,
  ]);

  useEffect(() => {
    load();
    return () => abortRef.current?.abort();
  }, [load]);

  return { data, loading, error, reload: load };
}
