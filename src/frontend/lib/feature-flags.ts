/** Painel RabbitMQ/filas — apenas dev/suporte (NEXT_PUBLIC_SHOW_OPS_PANEL=true). */
export const showOpsPanel = process.env.NEXT_PUBLIC_SHOW_OPS_PANEL === "true";

/** Botão "Criar ambiente demo" — só em dev local (nunca na Vercel de piloto). */
export const enableDemoLogin = process.env.NEXT_PUBLIC_ENABLE_DEMO_LOGIN === "true";

/** Versão exibida no menu (1.0.0-beta). */
export const appVersion =
  process.env.NEXT_PUBLIC_APP_VERSION ?? "1.0.0-beta";
