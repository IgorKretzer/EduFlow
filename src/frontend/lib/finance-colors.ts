export const VIOLET_PALETTE = ["#c4b5fd", "#a78bfa", "#8b5cf6", "#7c3aed", "#6d28d9"];
export const SKY_PALETTE = ["#7dd3fc", "#38bdf8", "#0ea5e9", "#0284c7", "#0369a1"];

export function paletteColor(palette: string[], index: number): string {
  return palette[index % palette.length] ?? palette[0];
}
