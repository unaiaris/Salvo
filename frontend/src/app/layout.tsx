import type { Metadata } from "next";
import type { ReactNode } from "react";
import { ConsoleHeader } from "@/components/console-header";
import { deploymentLanguage } from "@/lib/api/console";
import { formatting } from "@/lib/format";
import "./globals.css";

/**
 * The frame of the console, and the one place `<html lang>` is decided.
 *
 * <h3>The cost of reading the language here, paid with the eyes open</h3>
 *
 * This layout wraps every route, so asking the API for the deployment language makes the whole
 * console dynamic — `/` included, which used to be prerendered because it holds no data. The
 * alternative was to let the Next process read `SALVO_LANGUAGE` for itself, and that breaks the
 * single origin the API exists to be: two readers of one variable is a console in one language
 * around a paragraph in the other, with nothing to report it. Between a static entry page and a
 * language that cannot disagree with itself, the language wins. Five routes of an internal console
 * lose no CDN history worth having.
 *
 * `lang` is not cosmetic either: a screen reader pronounces Portuguese with Spanish phonetics
 * without it, which is where the language of the deployment and the accessibility pass meet.
 */
export async function generateMetadata(): Promise<Metadata> {
  const { t } = formatting(await deploymentLanguage());

  return { title: t.meta.title, description: t.meta.description };
}

export default async function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  const language = await deploymentLanguage();

  return (
    <html lang={language}>
      <body className="min-h-screen bg-slate-50 text-slate-900">
        <ConsoleHeader language={language} />
        <main className="mx-auto max-w-6xl px-6 py-10">{children}</main>
      </body>
    </html>
  );
}
