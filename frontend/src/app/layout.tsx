import type { Metadata } from "next";
import type { ReactNode } from "react";
import { ConsoleHeader } from "@/components/console-header";
import "./globals.css";

export const metadata: Metadata = {
  title: "Salvo",
  description: "Consola antifraude B2B con scoring determinista y auditable.",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="es">
      <body className="min-h-screen bg-slate-50 text-slate-900">
        <ConsoleHeader />
        <main className="mx-auto max-w-6xl px-6 py-10">{children}</main>
      </body>
    </html>
  );
}
