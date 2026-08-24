import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  transpilePackages: ["@scanner-sdk/client", "@scanner-sdk/types"]
};

export default nextConfig;
