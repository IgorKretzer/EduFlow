import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type CtaLink = { label: string; href: string };

type ScreenCtaProps = {
  title: string;
  description: string;
  primary: CtaLink;
  secondary?: CtaLink;
  className?: string;
};

export function ScreenCta({ title, description, primary, secondary, className }: ScreenCtaProps) {
  return (
    <div
      className={cn(
        "glass-panel flex flex-col gap-4 rounded-2xl p-5 sm:flex-row sm:items-center sm:justify-between",
        className
      )}
    >
      <div className="min-w-0">
        <p className="font-semibold text-foreground">{title}</p>
        <p className="mt-1 text-sm text-muted-foreground">{description}</p>
      </div>
      <div className="flex shrink-0 flex-wrap gap-2">
        {secondary && (
          <Button variant="outline" size="sm" asChild>
            <Link href={secondary.href}>{secondary.label}</Link>
          </Button>
        )}
        <Button size="sm" asChild>
          <Link href={primary.href}>
            {primary.label}
            <ArrowRight className="ml-2 h-4 w-4" />
          </Link>
        </Button>
      </div>
    </div>
  );
}
