import type { ReactNode } from "react";
import { SeverityBadge } from "@/components/severity-badge";
import {
  DASHBOARD_SEVERITY_ORDER,
  type DashboardAmountAtRisk,
  type DashboardOpenAlerts,
  type DashboardReportedFraud,
  type DashboardExternalDenials,
  type DashboardSignal,
  type Language,
} from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * El `id` del encabezado sale de una clave estable y no del título.
 *
 * Se derivaba del texto traducido, quitando todo lo que no fuera `[a-záéíóúñ]`. Eso ata un
 * identificador del documento al idioma del despliegue: en portugués `ção` queda `-o`, así que dos
 * títulos que difieran solo en esos caracteres colapsan en el mismo `id` y entonces un
 * `aria-labelledby` rotula una sección con el título de otra. Hoy no hay colisión —se verificaron
 * los siete paneles y las cuatro secciones en los dos idiomas—, pero es una propiedad que depende de
 * las cadenas y no del código, así que se pierde el día que alguien escribe un título nuevo.
 */
export function Panel({
  id,
  title,
  hint,
  children,
}: {
  /** Clave del panel, en el código y no en el diccionario. No se traduce ni se muestra. */
  readonly id: string;
  readonly title: string;
  readonly hint?: string;
  readonly children: ReactNode;
}) {
  const headingId = `panel-${id}`;

  return (
    <section
      aria-labelledby={headingId}
      className="flex flex-col rounded-lg border border-slate-200 bg-white p-5"
    >
      <h2 id={headingId} className="text-base font-semibold text-slate-900">
        {title}
      </h2>
      {hint !== undefined && <p className="mt-1 text-xs leading-5 text-slate-600">{hint}</p>}
      <div className="mt-4">{children}</div>
    </section>
  );
}

/**
 * Open alerts by band, every band, including the ones sitting at zero.
 *
 * The API reports the bands that have alerts, so a band with none simply does not come back. Showing
 * only what came back would let an analyst read "there is no `ALTA`" where the truth is "there is no
 * `ALTA` right now" — and in the demo corpus that is permanently the case, which makes it exactly
 * the kind of absence that stops being noticed.
 */
export function OpenAlertsPanel({
  openAlerts,
  language,
}: {
  readonly openAlerts: DashboardOpenAlerts;
  readonly language: Language;
}) {
  const counts = new Map(openAlerts.bySeverity.map((entry) => [entry.severity, entry.alertCount]));
  const f = formatting(language);

  return (
    <>
      <p className="text-3xl font-semibold tabular-nums text-slate-950">
        {f.formatCount(openAlerts.total)}
      </p>
      <ul className="mt-4 flex flex-col gap-2">
        {DASHBOARD_SEVERITY_ORDER.map((severity) => {
          const count = counts.get(severity) ?? 0;

          return (
            <li key={severity} className="flex items-center justify-between gap-4">
              <SeverityBadge severity={severity} language={language} />
              <span className="text-sm tabular-nums text-slate-800">
                {count === 0 ? f.t.dashboard.openAlertsNone : f.formatCount(count)}
              </span>
            </li>
          );
        })}
      </ul>
    </>
  );
}

/**
 * Money waiting for a verdict, one row per currency and no total anywhere.
 *
 * Adding BRL, USD and UYU produces a number with no unit; converting them would need a rate source,
 * a reference date and a policy for orders that are months old, none of which this stage has. The
 * shape of the API forces the point — `amountAtRisk` is a list — and the rendering keeps it.
 */
export function AmountAtRiskPanel({
  rows,
  language,
}: {
  readonly rows: readonly DashboardAmountAtRisk[];
  readonly language: Language;
}) {
  const f = formatting(language);

  if (rows.length === 0) {
    return <p className="text-sm text-slate-600">{f.t.dashboard.amountAtRiskNone}</p>;
  }

  return (
    <ul className="flex flex-col gap-3">
      {rows.map((row) => (
        <li key={row.currencyCode} className="flex items-baseline justify-between gap-4">
          <span className="text-lg font-semibold tabular-nums text-slate-950">
            {f.formatAmount(row.amountCents, row.currencyCode)}
          </span>
          <span className="text-xs text-slate-600">
            {f.t.dashboard.alertCount(row.alertCount, f.formatCount(row.alertCount))}
          </span>
        </li>
      ))}
    </ul>
  );
}

/**
 * What an analyst decided was fraud, aggregated by distinct order rather than by alert.
 *
 * An order that was reported in one band can be escalated into a higher one and reported again: two
 * alerts, one order, one loss. Counting alerts would double the money. It is also not ground truth
 * and does not pretend to be — outside a demo corpus nobody knows which orders really were fraud,
 * only what the analyst concluded.
 */
export function ReportedFraudPanel({
  rows,
  language,
}: {
  readonly rows: readonly DashboardReportedFraud[];
  readonly language: Language;
}) {
  const f = formatting(language);

  if (rows.length === 0) {
    return <p className="text-sm text-slate-600">{f.t.dashboard.reportedFraudNone}</p>;
  }

  return (
    <ul className="flex flex-col gap-3">
      {rows.map((row) => (
        <li key={row.currencyCode} className="flex items-baseline justify-between gap-4">
          <span className="text-lg font-semibold tabular-nums text-slate-950">
            {f.formatAmount(row.amountCents, row.currencyCode)}
          </span>
          <span className="text-xs text-slate-600">
            {f.t.dashboard.orderCount(row.orderCount, f.formatCount(row.orderCount))}
          </span>
        </li>
      ))}
    </ul>
  );
}

export function FlagRatePanel({
  flagRate,
  scoredOrders,
  language,
}: {
  readonly flagRate: number | null;
  readonly scoredOrders: number;
  readonly language: Language;
}) {
  const f = formatting(language);

  if (flagRate === null) {
    return <p className="text-sm text-slate-600">{f.t.dashboard.flagRateNone}</p>;
  }

  return (
    <>
      <p className="text-3xl font-semibold tabular-nums text-slate-950">
        {f.formatPercent(flagRate)}
      </p>
      <p className="mt-2 text-sm leading-6 text-slate-600">
        {f.t.dashboard.flagRateHint(f.formatCount(scoredOrders))}
      </p>
    </>
  );
}

/**
 * Which rules are actually opening alerts.
 *
 * Counted over the snapshots of the open alerts, not over every current evaluation: a rule that
 * fires at twenty points on sixteen orders and never reaches the alerting floor is noise on this
 * panel, and it would be the tallest bar on it.
 */
export function TopSignalsPanel({
  signals,
  language,
}: {
  readonly signals: readonly DashboardSignal[];
  readonly language: Language;
}) {
  const f = formatting(language);

  if (signals.length === 0) {
    return <p className="text-sm text-slate-600">{f.t.dashboard.topSignalsNone}</p>;
  }

  const highest = Math.max(...signals.map((signal) => signal.alertCount), 1);

  return (
    <ul className="flex flex-col gap-3">
      {signals.map((signal) => (
        <li key={signal.rule} className="flex flex-col gap-1">
          <div className="flex items-baseline justify-between gap-4 text-sm">
            <span className="text-slate-800">{f.ruleLabel(signal.rule)}</span>
            <span className="tabular-nums text-slate-600">{f.formatCount(signal.alertCount)}</span>
          </div>
          <div className="h-1.5 w-full rounded-full bg-slate-100">
            <div
              className="h-1.5 rounded-full bg-slate-700"
              style={{ width: `${String((signal.alertCount / highest) * 100)}%` }}
            />
          </div>
        </li>
      ))}
    </ul>
  );
}

/**
 * Orders an external provider denied that never opened a local alert.
 *
 * This is the only place in the console where an order without an alert can be seen at all. The
 * provider's opinion otherwise lives inside the detail of an alert, and an order the rules never
 * flagged has no detail to open — so before this panel existed, "there is fraud the local engine
 * cannot see and a provider can" was a claim the product made in prose and could not show.
 *
 * It is a reading and nothing else: no verdict, no action, no state. Judging one of these orders
 * would mean opening an alert on it, and nothing here does that.
 */
export function ExternalDenialsPanel({
  denials,
  language,
}: {
  readonly denials: DashboardExternalDenials;
  readonly language: Language;
}) {
  const f = formatting(language);

  if (denials.total === 0) {
    return <p className="text-sm text-slate-600">{f.t.dashboard.denialsNone}</p>;
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-slate-700">
        <span className="text-2xl font-semibold tabular-nums text-slate-950">
          {f.formatCount(denials.total)}
        </span>{" "}
        {f.t.dashboard.denialsOrderWord(denials.total)}
        {denials.listed < denials.total
          && f.t.dashboard.denialsListed(f.formatCount(denials.listed))}
      </p>
      <div className="max-h-80 overflow-y-auto overflow-x-auto rounded-md border border-slate-200">
        <table className="w-full min-w-[34rem] border-collapse text-left text-xs">
          <caption className="sr-only">{f.t.dashboard.denialsCaption}</caption>
          <thead className="sticky top-0 bg-slate-50 text-slate-600">
            <tr>
              <th scope="col" className="px-3 py-2 font-medium">
                {f.t.dashboard.denialsColumnOrder}
              </th>
              <th scope="col" className="px-3 py-2 font-medium">
                {f.t.dashboard.denialsColumnDate}
              </th>
              <th scope="col" className="px-3 py-2 text-right font-medium">
                {f.t.dashboard.denialsColumnAmount}
              </th>
              <th scope="col" className="px-3 py-2 font-medium">
                {f.t.dashboard.denialsColumnCountry}
              </th>
              <th scope="col" className="px-3 py-2 text-right font-medium">
                {f.t.dashboard.denialsColumnScore}
              </th>
            </tr>
          </thead>
          <tbody>
            {denials.items.map((item) => (
              <tr key={item.merchantReferenceId} className="border-t border-slate-100">
                <th scope="row" className="px-3 py-2 font-normal text-slate-900">
                  {item.merchantReferenceId}
                </th>
                <td className="px-3 py-2 text-slate-700">{f.formatDate(item.occurredAt)}</td>
                <td className="px-3 py-2 text-right tabular-nums text-slate-700">
                  {f.formatAmount(item.amountCents, item.currencyCode)}
                </td>
                <td className="px-3 py-2 text-slate-700">{item.countryCode}</td>
                <td className="px-3 py-2 text-right tabular-nums text-slate-700">
                  {item.localRiskScore === null
                    ? f.t.dashboard.denialsUnscored
                    : f.formatCount(item.localRiskScore)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
