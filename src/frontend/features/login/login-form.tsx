"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { Logo } from "@/components/brand/logo";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { enableDemoLogin } from "@/lib/feature-flags";
import { useAuth } from "@/providers/auth-provider";

export function LoginForm() {
  const { login, bootstrapDemo } = useAuth();
  const [email, setEmail] = useState(enableDemoLogin ? "admin@demo.eduflow" : "");
  const [password, setPassword] = useState(enableDemoLogin ? "Demo@123" : "");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!email.trim() || !password) {
      setError("Informe e-mail e senha.");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      await login(email.trim(), password);
    } catch {
      setError("Credenciais inválidas ou API offline (porta 8080).");
    } finally {
      setLoading(false);
    }
  }

  async function handleDemo() {
    setLoading(true);
    setError(null);
    try {
      await bootstrapDemo();
    } catch {
      setError("Falha ao criar demo. Verifique se a API está rodando.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex w-full flex-col justify-center px-8 py-12 lg:w-1/2 lg:px-16">
      <div className="mx-auto w-full max-w-md animate-fade-in">
        <div className="mb-8 lg:hidden">
          <Logo />
        </div>
        <h1 className="text-2xl font-semibold tracking-tight">Acesso executivo</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Entre com suas credenciais institucionais.
        </p>

        <form onSubmit={handleSubmit} className="mt-8 space-y-4">
          <div className="space-y-2">
            <label htmlFor="email" className="text-sm font-medium">
              E-mail
            </label>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="nome@instituicao.edu.br"
              disabled={loading}
            />
          </div>
          <div className="space-y-2">
            <label htmlFor="password" className="text-sm font-medium">
              Senha
            </label>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={loading}
            />
          </div>
          {error && (
            <p className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
              {error}
            </p>
          )}
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            Entrar
          </Button>
        </form>

        {enableDemoLogin ? (
          <Button
            type="button"
            variant="outline"
            className="mt-4 w-full"
            disabled={loading}
            onClick={handleDemo}
          >
            Criar ambiente demo
          </Button>
        ) : null}
      </div>
    </div>
  );
}
