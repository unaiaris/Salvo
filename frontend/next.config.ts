import type { NextConfig } from "next";

const apiBaseUrl = process.env.SALVO_API_BASE_URL ?? "http://127.0.0.1:5100";

const nextConfig: NextConfig = {
  experimental: {
    taint: true,
  },
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiBaseUrl}/:path*`,
      },
    ];
  },
};

export default nextConfig;
