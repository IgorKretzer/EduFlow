import { memo } from "react";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

type RiskBadgeProps = {
  risk: string;
  className?: string;
};

export const RiskBadge = memo(function RiskBadge({ risk, className }: RiskBadgeProps) {
  const variant =
    risk === "Alto" ? "danger" : risk === "Médio" ? "warning" : ("success" as const);

  return (
    <Badge variant={variant} className={cn("font-semibold tracking-wide", className)}>
      {risk}
    </Badge>
  );
});
