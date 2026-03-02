import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.ts");

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [
      {
        protocol: "https",
        hostname: "catholia.co.kr",
      },
      {
        protocol: "http",
        hostname: "catholia.co.kr",
      },
      {
        protocol: "https",
        hostname: "*.syndock.co.kr",
      },
      {
        protocol: "http",
        hostname: "*.syndock.co.kr",
      },
      {
        protocol: "http",
        hostname: "localhost",
      },
      {
        protocol: "http",
        hostname: "127.0.0.1",
      },
    ],
  },
  async rewrites() {
    return [
      {
        source: "/api/hubs/:path*",
        destination: "http://127.0.0.1:5100/api/hubs/:path*",
      },
      {
        source: "/api/:path*",
        destination: "http://127.0.0.1:5100/api/:path*",
      },
    ];
  },
};

export default withNextIntl(nextConfig);
