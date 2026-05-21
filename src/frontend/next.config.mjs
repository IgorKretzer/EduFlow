/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  async redirects() {
    return [
      { source: "/settings", destination: "/configuracoes", permanent: true },
      { source: "/painel", destination: "/configuracoes?tab=operacoes", permanent: true },
    ];
  },
};

export default nextConfig;
