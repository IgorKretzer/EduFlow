import { BarChart3, Shield, Sparkles } from "lucide-react";
import { Logo } from "@/components/brand/logo";

export function LoginHero() {
  return (
    <div className="relative hidden flex-col justify-between overflow-hidden bg-gradient-to-br from-brand-700 via-brand-600 to-sky-500 p-10 text-white lg:flex lg:w-1/2">
      <div className="absolute inset-0 surface-grid opacity-20" />
      <div className="relative z-10">
        <div className="rounded-lg bg-white/95 px-3 py-2 inline-block">
          <Logo href="/" />
        </div>
        <p className="mt-8 max-w-md text-3xl font-semibold leading-tight">
          Inteligência que conecta sua escola.
        </p>
        <p className="mt-4 max-w-sm text-sm text-blue-100">
          Transforme dados do ERP em decisões executivas — receita, retenção, evasão e risco em um
          só lugar.
        </p>
      </div>

      <div className="relative z-10 space-y-4">
        {[
          { icon: BarChart3, text: "Dashboards estilo Power BI, prontos para gestão." },
          { icon: Shield, text: "Arquitetura multi-tenant e integração Sponte." },
          { icon: Sparkles, text: "Insights automáticos para diretoria e operações." },
        ].map(({ icon: Icon, text }) => (
          <div key={text} className="flex items-center gap-3 text-sm text-blue-50">
            <div className="rounded-lg bg-white/15 p-2">
              <Icon className="h-4 w-4" />
            </div>
            {text}
          </div>
        ))}
      </div>

      <div className="relative z-10 mt-8 rounded-2xl border border-white/20 bg-white/10 p-4 shadow-2xl backdrop-blur">
        <p className="text-xs font-medium uppercase tracking-wider text-blue-100">Preview executivo</p>
        <div className="mt-3 grid grid-cols-3 gap-2">
          {["Receita", "Churn", "Retenção"].map((label) => (
            <div key={label} className="rounded-lg bg-white/15 p-3">
              <p className="text-[10px] text-blue-100">{label}</p>
              <p className="mt-1 text-lg font-semibold">+12%</p>
            </div>
          ))}
        </div>
        <div className="mt-3 h-24 rounded-lg bg-gradient-to-t from-white/20 to-transparent" />
      </div>
    </div>
  );
}
