import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.ts");

const isProduction = process.env.NODE_ENV === "production";

const nextConfig: NextConfig = {
  output: "standalone",
  images: {
    formats: ["image/webp", "image/avif"],
    deviceSizes: [640, 750, 828, 1080, 1200, 1920],
    imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
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
        hostname: "mohyun.com",
      },
      {
        protocol: "http",
        hostname: "mohyun.com",
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
    if (isProduction) {
      return [];
    }
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
