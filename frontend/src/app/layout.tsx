import type { Metadata } from "next";
import type { ReactNode } from "react";
import { ConsoleHeader } from "@/components/console-header";
import { SharedInstanceNotice } from "@/components/shared-instance-notice";
import { deploymentLanguage, fetchCapabilities, languageOf } from "@/lib/api/console";
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
  // The same read the language already came from — `fetchCapabilities` is memoised for the render
  // pass — so the notice costs no extra round trip. It is here and not in each page because a page
  // added later would ship without it and nothing would look wrong.
  const capabilities = await fetchCapabilities();
  const language = languageOf(capabilities);
  const shared = capabilities.ok && capabilities.value.sharedInstance;

  return (
    <html lang={language}>
      <body className="min-h-screen bg-slate-50 text-slate-900">
        {shared && capabilities.ok && (
          <SharedInstanceNotice language={language} resetMinutes={capabilities.value.resetMinutes} />
        )}
        <ConsoleHeader language={language} isSharedInstance={shared} />
        <main className="mx-auto max-w-6xl px-6 py-10">{children}</main>
      </body>
    </html>
  );
}
