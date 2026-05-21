import { redirect } from "next/navigation";

/** Rota legada — redireciona ao painel executivo (não expõe painel operacional). */
export default function PainelRedirect() {
  redirect("/");
}
