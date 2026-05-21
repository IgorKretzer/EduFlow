"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { getSidebarCollapsed, setSidebarCollapsed } from "@/lib/shell-storage";

type ShellContextValue = {
  sidebarCollapsed: boolean;
  mobileNavOpen: boolean;
  toggleSidebar: () => void;
  setSidebarCollapsed: (collapsed: boolean) => void;
  toggleMobileNav: () => void;
  closeMobileNav: () => void;
};

const ShellContext = createContext<ShellContextValue | null>(null);

export function ShellProvider({ children }: { children: ReactNode }) {
  const [sidebarCollapsed, setCollapsed] = useState(false);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    setCollapsed(getSidebarCollapsed());
    setHydrated(true);
  }, []);

  const persistCollapsed = useCallback((collapsed: boolean) => {
    setCollapsed(collapsed);
    setSidebarCollapsed(collapsed);
  }, []);

  const toggleSidebar = useCallback(() => {
    setCollapsed((prev) => {
      const next = !prev;
      setSidebarCollapsed(next);
      return next;
    });
  }, []);

  const setSidebarCollapsedState = useCallback((collapsed: boolean) => {
    persistCollapsed(collapsed);
  }, [persistCollapsed]);

  const toggleMobileNav = useCallback(() => {
    setMobileNavOpen((open) => !open);
  }, []);

  const closeMobileNav = useCallback(() => {
    setMobileNavOpen(false);
  }, []);

  const value = useMemo(
    () => ({
      sidebarCollapsed: hydrated ? sidebarCollapsed : false,
      mobileNavOpen,
      toggleSidebar,
      setSidebarCollapsed: setSidebarCollapsedState,
      toggleMobileNav,
      closeMobileNav,
    }),
    [
      hydrated,
      sidebarCollapsed,
      mobileNavOpen,
      toggleSidebar,
      setSidebarCollapsedState,
      toggleMobileNav,
      closeMobileNav,
    ]
  );

  return <ShellContext.Provider value={value}>{children}</ShellContext.Provider>;
}

export function useShell() {
  const ctx = useContext(ShellContext);
  if (!ctx) throw new Error("useShell deve ser usado dentro de ShellProvider");
  return ctx;
}
