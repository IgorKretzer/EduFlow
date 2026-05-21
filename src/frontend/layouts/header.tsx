"use client";

import { Bell, PanelLeft } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { ThemeToggle } from "@/components/theme/theme-toggle";
import { useAuth } from "@/providers/auth-provider";
import { useShell } from "@/providers/shell-provider";
import { cn } from "@/lib/utils";

type HeaderProps = {
  title?: string;
  subtitle?: string;
};

function isMobileViewport() {
  if (typeof window === "undefined") return false;
  return window.matchMedia("(max-width: 767px)").matches;
}

export function Header({ title, subtitle }: HeaderProps) {
  const { session } = useAuth();
  const { toggleSidebar, toggleMobileNav, sidebarCollapsed } = useShell();

  function handleMenuToggle() {
    if (isMobileViewport()) {
      toggleMobileNav();
    } else {
      toggleSidebar();
    }
  }

  return (
    <header className="z-20 flex h-14 shrink-0 items-center justify-between border-b border-border bg-card px-4 shadow-sm md:px-6">
      <div className="flex min-w-0 items-center gap-3">
        <button
          type="button"
          onClick={handleMenuToggle}
          className="rounded-lg border border-border p-2 text-muted-foreground transition hover:bg-accent hover:text-foreground"
          aria-label={sidebarCollapsed ? "Expandir menu" : "Recolher menu"}
          title={sidebarCollapsed ? "Expandir menu" : "Recolher menu"}
        >
          <PanelLeft className={cn("h-4 w-4 md:transition", sidebarCollapsed && "md:rotate-180")} />
        </button>
        {(title || subtitle) && (
          <div className="min-w-0">
            {title && <p className="truncate text-sm font-semibold">{title}</p>}
            {subtitle && <p className="truncate text-xs text-muted-foreground">{subtitle}</p>}
          </div>
        )}
      </div>
      <div className="flex items-center gap-2 md:gap-3">
        <ThemeToggle />
        <button
          type="button"
          className="relative rounded-lg border border-border p-2 text-muted-foreground transition hover:bg-accent"
          aria-label="Notificações"
        >
          <Bell className="h-4 w-4" />
        </button>
        <div className="hidden items-center gap-2 sm:flex">
          <Badge variant="secondary">{session?.tenantSlug}</Badge>
        </div>
        <div
          className={cn(
            "flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-brand-500 to-sky-500 text-xs font-bold text-white"
          )}
        >
          {(session?.email?.[0] ?? "E").toUpperCase()}
        </div>
      </div>
    </header>
  );
}
