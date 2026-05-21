import { Suspense } from "react";
import { FinanceHub } from "@/features/finance/finance-hub";

export default function FinanceiroPage() {
  return (
    <Suspense
      fallback={
        <p className="py-12 text-center text-sm text-muted-foreground">Carregando financeiro…</p>
      }
    >
      <FinanceHub />
    </Suspense>
  );
}
