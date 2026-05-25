"use client";

import { useCallback, useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import { PageHeader } from "@/components/dashboard/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { erpService } from "@/services/api/erp.service";
import type { ErpConfig, SyncSchedulerStatus } from "@/types/api";

const DEFAULT_SPONTE_ENDPOINT = "https://api.sponteeducacional.net.br/WSAPIEdu.asmx";
const DEFAULT_OPENAPI_ENDPOINT = "https://api.exemplo.com/v1";

type ErpProvider = "sponte" | "openapi";

export function SettingsView() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [scheduler, setScheduler] = useState<SyncSchedulerStatus | null>(null);
  const [providerKey, setProviderKey] = useState<ErpProvider>("sponte");
  const [endpointUrl, setEndpointUrl] = useState(DEFAULT_SPONTE_ENDPOINT);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [pageSize, setPageSize] = useState(100);
  const [syncEnabled, setSyncEnabled] = useState(true);
  const [syncIntervalMinutes, setSyncIntervalMinutes] = useState(15);
  const [searchStudents, setSearchStudents] = useState("Situacao=2|TOP=100");
  const [searchFinancial, setSearchFinancial] = useState("TOP=100");
  const [searchContracts, setSearchContracts] = useState("Situacao=2|TOP=100");
  const [lastSync, setLastSync] = useState<ErpConfig | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const sched = await erpService.getScheduler();
      setScheduler(sched);
      try {
        const config = await erpService.getConfig();
        setLastSync(config);
        const provider = (config.providerKey === "openapi" ? "openapi" : "sponte") as ErpProvider;
        setProviderKey(provider);
        setEndpointUrl(
          config.endpointUrl ||
            (provider === "openapi" ? DEFAULT_OPENAPI_ENDPOINT : DEFAULT_SPONTE_ENDPOINT)
        );
        setUsername(config.username);
        setPageSize(config.pageSize);
        setSyncEnabled(config.syncEnabled);
        setSyncIntervalMinutes(config.syncIntervalMinutes);
        if (config.searchParametersStudents) setSearchStudents(config.searchParametersStudents);
        if (config.searchParametersFinancial) setSearchFinancial(config.searchParametersFinancial);
        if (config.searchParametersContracts) setSearchContracts(config.searchParametersContracts);
      } catch {
        setLastSync(null);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao carregar");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setMessage(null);
    setError(null);
    try {
      const saved = await erpService.saveConfig({
        providerKey,
        endpointUrl,
        username,
        password: password.trim() || undefined,
        pageSize,
        syncEnabled,
        syncIntervalMinutes,
        searchParametersStudents: searchStudents,
        searchParametersFinancial: searchFinancial,
        searchParametersContracts: searchContracts,
      });
      setLastSync(saved);
      setPassword("");
      setMessage("Configuração salva com sucesso.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha ao salvar");
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return <p className="text-sm text-muted-foreground">Carregando…</p>;
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {scheduler && (
        <Card>
          <CardContent className="p-4 text-sm">
            <p className="font-medium text-primary">Sincronização automática</p>
            <p className="mt-1 text-muted-foreground">
              {scheduler.schedulerEnabled
                ? `Ativa — verificação a cada ${scheduler.defaultIntervalMinutes} minutos.`
                : "Desativada no servidor."}
            </p>
          </CardContent>
        </Card>
      )}

      {lastSync?.lastSyncAtUtc && (
        <Card>
          <CardContent className="p-4 text-sm">
            <p className="text-muted-foreground">Último sync</p>
            <p className="mt-1 font-medium">
              {new Date(lastSync.lastSyncAtUtc).toLocaleString("pt-BR")} — {lastSync.lastSyncStatus}
            </p>
          </CardContent>
        </Card>
      )}

      <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border bg-card p-6 shadow-card">
        <Field label="Sistema de origem">
          <select
            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={providerKey}
            onChange={(e) => {
              const next = e.target.value as ErpProvider;
              setProviderKey(next);
              if (next === "openapi") {
                setEndpointUrl(DEFAULT_OPENAPI_ENDPOINT);
                if (searchStudents.includes("Situacao=")) setSearchStudents("/students?limit=100");
                if (!searchFinancial.startsWith("/")) setSearchFinancial("/financial?limit=100");
                if (!searchContracts.startsWith("/")) setSearchContracts("/contracts?limit=100");
              } else {
                setEndpointUrl(DEFAULT_SPONTE_ENDPOINT);
                if (searchStudents.startsWith("/")) setSearchStudents("Situacao=2|TOP=100");
                if (searchFinancial.startsWith("/")) setSearchFinancial("TOP=100");
                if (searchContracts.startsWith("/")) setSearchContracts("Situacao=2|TOP=100");
              }
            }}
          >
            <option value="sponte">Sponte (SOAP)</option>
            <option value="openapi">API REST / OpenAPI</option>
          </select>
        </Field>
        <Field
          label={
            providerKey === "sponte"
              ? "Endereço da integração Sponte"
              : "URL base da API REST"
          }
        >
          <Input value={endpointUrl} onChange={(e) => setEndpointUrl(e.target.value)} required />
        </Field>
        <Field
          label={
            providerKey === "sponte"
              ? "Código cliente (nCodigoCliente)"
              : "Autenticação (vazio = Bearer; ApiKey = header X-Api-Key; ou nome do header)"
          }
        >
          <Input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required={providerKey === "sponte"}
            placeholder={providerKey === "openapi" ? "Opcional" : undefined}
          />
        </Field>
        <Field
          label={
            providerKey === "sponte"
              ? `Token (sToken)${lastSync?.hasPassword ? " — vazio mantém atual" : ""}`
              : `Token / chave API${lastSync?.hasPassword ? " — vazio mantém atual" : ""}`
          }
        >
          <Input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder={lastSync?.hasPassword ? "••••••••" : "Obrigatório"}
          />
        </Field>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Page size">
            <Input
              type="number"
              min={10}
              max={500}
              value={pageSize}
              onChange={(e) => setPageSize(Number(e.target.value))}
            />
          </Field>
          <Field label="Intervalo (min)">
            <Input
              type="number"
              min={5}
              value={syncIntervalMinutes}
              onChange={(e) => setSyncIntervalMinutes(Number(e.target.value))}
            />
          </Field>
        </div>
        <Field
          label={
            providerKey === "sponte"
              ? "Parâmetros — alunos / GetAlunos"
              : "Caminho REST — alunos"
          }
        >
          <Input
            value={searchStudents}
            onChange={(e) => setSearchStudents(e.target.value)}
            className="font-mono text-xs"
            placeholder={providerKey === "sponte" ? "Situacao=2|TOP=150" : "/students?limit=150"}
          />
        </Field>
        <Field
          label={
            providerKey === "sponte"
              ? "Parâmetros — financeiro / GetFinanceiro"
              : "Caminho REST — financeiro"
          }
        >
          <Input
            value={searchFinancial}
            onChange={(e) => setSearchFinancial(e.target.value)}
            className="font-mono text-xs"
            placeholder={providerKey === "sponte" ? "TOP=150" : "/financial?limit=150"}
          />
        </Field>
        <Field
          label={
            providerKey === "sponte"
              ? "Parâmetros — matrículas / GetMatriculas"
              : "Caminho REST — matrículas"
          }
        >
          <Input
            value={searchContracts}
            onChange={(e) => setSearchContracts(e.target.value)}
            className="font-mono text-xs"
            placeholder={providerKey === "sponte" ? "Situacao=2|TOP=150" : "/contracts?limit=150"}
          />
        </Field>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={syncEnabled} onChange={(e) => setSyncEnabled(e.target.checked)} />
          Sincronização automática habilitada
        </label>
        {error && <p className="text-sm text-rose-600">{error}</p>}
        {message && <p className="text-sm text-emerald-600">{message}</p>}
        <Button type="submit" disabled={saving}>
          {saving && <Loader2 className="h-4 w-4 animate-spin" />}
          Salvar integração
        </Button>
      </form>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block space-y-1.5">
      <span className="text-sm font-medium text-muted-foreground">{label}</span>
      {children}
    </label>
  );
}
