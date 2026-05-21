"use client";

import { useRequireAuth } from "@/providers/auth-provider";
import { ShellProvider, useShell } from "@/providers/shell-provider";
import { Header } from "@/layouts/header";
import { Sidebar } from "@/layouts/sidebar";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

function ShellLayout({ children }: { children: React.ReactNode }) {
  const { mobileNavOpen, closeMobileNav, sidebarCollapsed } = useShell();

  return (
    <div className="flex h-screen overflow-hidden bg-background">
      {mobileNavOpen && (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-black/50 backdrop-blur-sm md:hidden"
          aria-label="Fechar menu"
          onClick={closeMobileNav}
        />
      )}

      <div
        aria-hidden
        className={cn(
          "hidden shrink-0 transition-[width] duration-200 ease-out md:block",
          sidebarCollapsed ? "w-[4.25rem]" : "w-64"
        )}
      />

      <Sidebar />

      <div className="flex min-h-0 min-w-0 flex-1 flex-col">
        <Header />
        <main className="min-h-0 w-full flex-1 overflow-y-auto overflow-x-clip p-4 md:p-6 lg:p-8">
          <div className="w-full">{children}</div>
        </main>
      </div>
    </div>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const { session, loading } = useRequireAuth();

  if (loading || !session) {
    return (
      <div className="flex min-h-screen">
        <Skeleton className="hidden h-screen w-64 shrink-0 md:block" />
        <div className="flex min-w-0 flex-1 flex-col gap-4 p-8">
          <Skeleton className="h-10 w-full max-w-lg" />
          <div className="grid gap-4 md:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-28 rounded-xl" />
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <ShellProvider>
      <ShellLayout>{children}</ShellLayout>
    </ShellProvider>
  );
}
