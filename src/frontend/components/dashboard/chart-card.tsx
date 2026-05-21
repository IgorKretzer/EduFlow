import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

type ChartCardProps = {
  title: string;
  description?: string;
  children: React.ReactNode;
  className?: string;
  contentClassName?: string;
  action?: React.ReactNode;
  variant?: "default" | "hero";
};

export function ChartCard({
  title,
  description,
  children,
  className,
  contentClassName,
  action,
  variant = "default",
}: ChartCardProps) {
  return (
    <Card
      className={cn(
        "border-0 glass-panel shadow-card",
        variant === "hero" && "ring-1 ring-primary/10",
        className
      )}
    >
      <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
        <div className="min-w-0 pr-2">
          <CardTitle
            className={cn(
              variant === "hero" ? "text-lg font-semibold tracking-tight" : "text-base"
            )}
          >
            {title}
          </CardTitle>
          {description && (
            <CardDescription className={cn("mt-1 line-clamp-2", variant === "hero" && "text-sm")}>
              {description}
            </CardDescription>
          )}
        </div>
        {action}
      </CardHeader>
      <CardContent
        className={cn(
          variant === "hero" ? "min-h-[280px] pb-5" : "min-h-[240px] pb-4",
          contentClassName
        )}
      >
        {children}
      </CardContent>
    </Card>
  );
}
