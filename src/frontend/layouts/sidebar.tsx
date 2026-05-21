"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  AlertTriangle,
  FileText,
  LayoutDashboard,
  Lightbulb,
  LogOut,
  PanelLeft,
  Settings,
  TrendingDown,
  TrendingUp,
  Wallet,
} from "lucide-react";
import { Logo } from "@/components/brand/logo";
import { cn } from "@/lib/utils";
import { appVersion } from "@/lib/feature-flags";
import { useAuth } from "@/providers/auth-provider";
import { useShell } from "@/providers/shell-provider";

const nav = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/financeiro", label: "Financeiro", icon: Wallet },
  { href: "/retencao", label: "Retenção", icon: TrendingUp },
  { href: "/evasao", label: "Evasão", icon: TrendingDown },
  { href: "/risk-enrollments", label: "Matrículas em Risco", icon: AlertTriangle },
  { href: "/insights", label: "Recomendações", icon: Lightbulb },
  { href: "/relatorios", label: "Relatórios", icon: FileText },
  { href: "/configuracoes", label: "Configurações", icon: Settings },
];

export function Sidebar() {
  const pathname = usePathname();
  const { logout, session } = useAuth();
  const { sidebarCollapsed, toggleSidebar, mobileNavOpen, closeMobileNav } = useShell();

  return (
    <aside
      className={cn(
        "fixed inset-y-0 left-0 z-40 flex h-screen flex-col border-r border-border/60 bg-card shadow-sm",
        "transition-[width,transform] duration-200 ease-out",
        sidebarCollapsed ? "w-[4.25rem]" : "w-64",
        mobileNavOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0"
      )}
    >
      <div
        className={cn(
          "flex shrink-0 items-center border-b border-border",
          sidebarCollapsed ? "flex-col gap-2 px-2 py-3" : "justify-between gap-2 px-4 py-4"
        )}
      >
        <Logo size={sidebarCollapsed ? "sm" : "md"} href="/" />
        <button
          type="button"
          onClick={toggleSidebar}
          className="hidden rounded-lg border border-border p-2 text-muted-foreground transition hover:bg-accent hover:text-foreground md:inline-flex"
          aria-label={sidebarCollapsed ? "Expandir menu" : "Recolher menu"}
          title={sidebarCollapsed ? "Expandir menu" : "Recolher menu"}
        >
          <PanelLeft className={cn("h-4 w-4 transition", sidebarCollapsed && "rotate-180")} />
        </button>
      </div>
      {!sidebarCollapsed && (
        <p className="shrink-0 border-b border-border px-5 pb-3 text-xs text-muted-foreground">
          Inteligência educacional
        </p>
      )}

      <nav className="min-h-0 flex-1 space-y-0.5 overflow-y-auto p-2">
        {nav.map((item) => {
          const active =
            pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              title={sidebarCollapsed ? item.label : undefined}
              onClick={closeMobileNav}
              className={cn(
                "flex items-center gap-3 rounded-lg py-2.5 text-sm font-medium transition-all",
                sidebarCollapsed ? "justify-center px-2" : "px-3",
                active
                  ? "bg-primary text-primary-foreground shadow-sm"
                  : "text-muted-foreground hover:bg-accent hover:text-foreground"
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              {!sidebarCollapsed && <span className="truncate">{item.label}</span>}
            </Link>
          );
        })}
      </nav>

      <div className="shrink-0 border-t border-border p-2">
        {!sidebarCollapsed && (
          <div className="mb-2 rounded-lg bg-muted/50 px-3 py-2">
            <p className="truncate text-xs font-medium text-foreground">{session?.tenantSlug}</p>
            <p className="truncate text-xs text-muted-foreground">{session?.email}</p>
          </div>
        )}
        {!sidebarCollapsed && (
          <p className="mb-2 px-3 text-[10px] uppercase tracking-wide text-muted-foreground/80">
            EduFlow {appVersion}
          </p>
        )}
        <button
          type="button"
          onClick={logout}
          title="Sair"
          className={cn(
            "flex w-full items-center gap-2 rounded-lg py-2 text-sm text-muted-foreground transition hover:bg-accent hover:text-foreground",
            sidebarCollapsed ? "justify-center px-2" : "px-3"
          )}
        >
          <LogOut className="h-4 w-4 shrink-0" />
          {!sidebarCollapsed && "Sair"}
        </button>
      </div>
    </aside>
  );
}
