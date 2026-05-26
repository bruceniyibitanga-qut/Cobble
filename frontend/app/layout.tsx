import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "QUT Industry Relations Database",
  description: "QUT Professional Services — Industry Relations Database",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body suppressHydrationWarning>{children}</body>
    </html>
  );
}
