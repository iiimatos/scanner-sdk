import "./globals.css";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Scanner SDK Playground",
  description: "Development playground for Scanner SDK"
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
