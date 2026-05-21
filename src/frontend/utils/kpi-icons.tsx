import {
  DollarSign,
  GraduationCap,
  Percent,
  TrendingDown,
  TrendingUp,
  Users,
  type LucideIcon,
} from "lucide-react";

export function getKpiIcon(key: string): LucideIcon {
  const k = key.toLowerCase();
  if (k.includes("revenue") || k.includes("receita")) return DollarSign;
  if (k.includes("churn") || k.includes("evas")) return TrendingDown;
  if (k.includes("retention") || k.includes("retenc")) return TrendingUp;
  if (k.includes("delinq") || k.includes("inadimpl")) return Percent;
  if (k.includes("student") || k.includes("aluno")) return Users;
  if (k.includes("growth") || k.includes("cresc")) return TrendingUp;
  return GraduationCap;
}
