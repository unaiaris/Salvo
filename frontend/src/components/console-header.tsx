import Link from "next/link";

const LINKS = [
  { href: "/alerts", label: "Alertas" },
  { href: "/import", label: "Importación" },
  { href: "/dashboard", label: "Dashboard" },
] as const;

/**
 * The console frame. `/import` and `/dashboard` are linked before they exist: they are the two other
 * screens of stage 5, and leaving them out of the navigation would hide from the analyst that
 * importing and scoring are part of this tool rather than something someone does for her.
 */
export function ConsoleHeader() {
  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-8 gap-y-2 px-6 py-4">
        <Link
          href="/"
          className="text-lg font-semibold tracking-tight text-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          Salvo
        </Link>
        <nav aria-label="Secciones de la consola">
          <ul className="flex flex-wrap gap-x-6 gap-y-1 text-sm font-medium">
            {LINKS.map((link) => (
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
        <p className="ml-auto text-xs text-slate-500">
          Datos sintéticos · sin autenticación · uso local
        </p>
      </div>
    </header>
  );
}
