"use client";

import { Area, AreaChart, ResponsiveContainer } from "recharts";

export function MiniSparkline({
  data,
  light,
  positive = true,
}: {
  data: { value: number }[];
  light?: boolean;
  positive?: boolean;
}) {
  const color = light ? "#ffffff" : positive ? "#059669" : "#e11d48";
  const fill = light ? "rgba(255,255,255,0.25)" : positive ? "rgba(5,150,105,0.15)" : "rgba(225,29,72,0.12)";

  return (
    <ResponsiveContainer width="100%" height="100%">
      <AreaChart data={data}>
        <defs>
          <linearGradient id={`spark-${positive}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={fill} />
            <stop offset="100%" stopColor="transparent" />
          </linearGradient>
        </defs>
        <Area
          type="monotone"
          dataKey="value"
          stroke={color}
          strokeWidth={2}
          fill={`url(#spark-${positive})`}
          dot={false}
          isAnimationActive={false}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}
