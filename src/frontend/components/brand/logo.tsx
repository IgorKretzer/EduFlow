import Image from "next/image";
import Link from "next/link";
import { cn } from "@/lib/utils";

type LogoProps = {
  className?: string;
  href?: string;
  size?: "sm" | "md" | "lg";
};

const heights = { sm: 28, md: 36, lg: 44 };

export function Logo({ className, href = "/", size = "md" }: LogoProps) {
  const img = (
    <Image
      src="/logo.svg"
      alt="EduFlow"
      width={160}
      height={heights[size]}
      className={cn("h-auto w-auto", className)}
      priority
    />
  );

  if (href) {
    return (
      <Link href={href} className="inline-flex items-center">
        {img}
      </Link>
    );
  }

  return img;
}
