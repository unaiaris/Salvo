import Link from "next/link";
import type { Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * The console frame. `/import` and `/dashboard` sit in the navigation next to the queue because
 * importing and scoring are part of this tool rather than something someone does for the analyst.
 * They were linked here before either screen existed, for that reason; both have existed since
 * stage 5.
 */
export function ConsoleHeader({ language }: { readonly language: Language }) {
  const { t } = formatting(language);
  const links = [
    { href: "/alerts", label: t.nav.alerts },
    { href: "/import", label: t.nav.import },
    { href: "/dashboard", label: t.nav.dashboard },
  ];

  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-8 gap-y-2 px-6 py-4">
        <Link
          href="/"
          className="text-lg font-semibold tracking-tight text-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {t.nav.brand}
        </Link>
        <nav aria-label={t.nav.label}>
          <ul className="flex flex-wrap gap-x-6 gap-y-1 text-sm font-medium">
            {links.map((link) => (
              <li key={link.href}>
                <Link
                  href={link.href}
                  className="text-slate-700 underline-offset-4 hover:text-slate-950 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
                >
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
        <p className="ml-auto text-xs text-slate-500">{t.nav.disclaimer}</p>
      </div>
    </header>
  );
}
