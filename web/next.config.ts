import type { NextConfig } from "next";

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
    ],
  },
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://127.0.0.1:5100/api/:path*",
      },
    ];
  },
};

export default nextConfig;
