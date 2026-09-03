import { severityLabel } from "@/lib/format";

/**
 * Severity in words first, colour second. The band decides whether an alert is looked at now or
 * tomorrow, and that decision cannot rest on telling amber from red.
 */
const STYLES: Readonly<Record<string, string>> = {
  CRITICAL: "border-rose-300 bg-rose-50 text-rose-900",
  HIGH: "border-amber-300 bg-amber-50 text-amber-900",
  MEDIUM: "border-sky-300 bg-sky-50 text-sky-900",
};

export function SeverityBadge({ severity }: { readonly severity: string }) {
  const style = STYLES[severity] ?? "border-slate-300 bg-slate-50 text-slate-800";

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold tracking-wide ${style}`}
    >
      {severityLabel(severity)}
    </span>
  );
}

/** The band an alert no longer has: the current evaluation stopped reaching the alerting floor. */
export function NoSeverityBadge() {
  return (
    <span className="inline-flex items-center rounded-full border border-slate-300 bg-slate-50 px-2.5 py-0.5 text-xs font-semibold tracking-wide text-slate-700">
      SIN BANDA
    </span>
  );
}
