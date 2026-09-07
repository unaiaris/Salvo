import type { ReactNode } from "react";
import { SeverityBadge } from "@/components/severity-badge";
import {
  DASHBOARD_SEVERITY_ORDER,
  type DashboardAmountAtRisk,
  type DashboardOpenAlerts,
  type DashboardReportedFraud,
  type DashboardExternalDenials,
  type DashboardSignal,
} from "@/lib/api/contract";
import { formatAmount, formatCount, formatDate, formatPercent, ruleLabel } from "@/lib/format";

export function Panel({
  title,
  hint,
  children,
}: {
  readonly title: string;
  readonly hint?: string;
  readonly children: ReactNode;
}) {
  const headingId = `panel-${title.toLowerCase().replaceAll(/[^a-záéíóúñ]+/g, "-")}`;

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
export function OpenAlertsPanel({ openAlerts }: { readonly openAlerts: DashboardOpenAlerts }) {
  const counts = new Map(openAlerts.bySeverity.map((entry) => [entry.severity, entry.alertCount]));

  return (
    <>
      <p className="text-3xl font-semibold tabular-nums text-slate-950">
        {formatCount(openAlerts.total)}
      </p>
      <ul className="mt-4 flex flex-col gap-2">
        {DASHBOARD_SEVERITY_ORDER.map((severity) => {
          const count = counts.get(severity) ?? 0;

          return (
            <li key={severity} className="flex items-center justify-between gap-4">
              <SeverityBadge severity={severity} />
              <span className="text-sm tabular-nums text-slate-800">
                {count === 0 ? "sin alertas" : formatCount(count)}
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
export function AmountAtRiskPanel({ rows }: { readonly rows: readonly DashboardAmountAtRisk[] }) {
  if (rows.length === 0) {
    return <p className="text-sm text-slate-600">No hay ninguna alerta abierta con monto asociado.</p>;
  }

  return (
    <ul className="flex flex-col gap-3">
      {rows.map((row) => (
        <li key={row.currencyCode} className="flex items-baseline justify-between gap-4">
          <span className="text-lg font-semibold tabular-nums text-slate-950">
            {formatAmount(row.amountCents, row.currencyCode)}
          </span>
          <span className="text-xs text-slate-600">
            {row.alertCount === 1 ? "1 alerta" : `${formatCount(row.alertCount)} alertas`}
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
export function ReportedFraudPanel({ rows }: { readonly rows: readonly DashboardReportedFraud[] }) {
  if (rows.length === 0) {
    return (
      <p className="text-sm text-slate-600">
        Todavía nadie marcó una alerta como fraude en esta base.
      </p>
    );
  }

  return (
    <ul className="flex flex-col gap-3">
      {rows.map((row) => (
        <li key={row.currencyCode} className="flex items-baseline justify-between gap-4">
          <span className="text-lg font-semibold tabular-nums text-slate-950">
            {formatAmount(row.amountCents, row.currencyCode)}
          </span>
          <span className="text-xs text-slate-600">
            {row.orderCount === 1 ? "1 pedido" : `${formatCount(row.orderCount)} pedidos`}
          </span>
        </li>
      ))}
    </ul>
  );
}

export function FlagRatePanel({
  flagRate,
  scoredOrders,
}: {
  readonly flagRate: number | null;
  readonly scoredOrders: number;
}) {
  if (flagRate === null) {
    return (
      <p className="text-sm text-slate-600">
        La corrida vigente no cubrió ningún pedido, así que no hay proporción que calcular.
      </p>
    );
  }

  return (
    <>
      <p className="text-3xl font-semibold tabular-nums text-slate-950">
        {formatPercent(flagRate)}
      </p>
      <p className="mt-2 text-sm leading-6 text-slate-600">
        Proporción de los {formatCount(scoredOrders)} pedidos de la corrida vigente que las reglas
        denegaron.
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
export function TopSignalsPanel({ signals }: { readonly signals: readonly DashboardSignal[] }) {
  if (signals.length === 0) {
    return <p className="text-sm text-slate-600">Ninguna alerta abierta, así que ninguna señal.</p>;
  }

  const highest = Math.max(...signals.map((signal) => signal.alertCount), 1);

  return (
    <ul className="flex flex-col gap-3">
      {signals.map((signal) => (
        <li key={signal.rule} className="flex flex-col gap-1">
          <div className="flex items-baseline justify-between gap-4 text-sm">
            <span className="text-slate-800">{ruleLabel(signal.rule)}</span>
            <span className="tabular-nums text-slate-600">{formatCount(signal.alertCount)}</span>
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
}: {
  readonly denials: DashboardExternalDenials;
}) {
  if (denials.total === 0) {
    return (
      <p className="text-sm text-slate-600">
        Ningún pedido denegado por el proveedor quedó fuera de la cola. O nadie pidió todavía la
        evaluación externa, o el proveedor y el motor local coincidieron en todo.
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-slate-700">
        <span className="text-2xl font-semibold tabular-nums text-slate-950">
          {formatCount(denials.total)}
        </span>{" "}
        {denials.total === 1 ? "pedido" : "pedidos"}
        {denials.listed < denials.total
          && ` · se listan los ${formatCount(denials.listed)} más recientes`}
      </p>
      <div className="max-h-80 overflow-y-auto overflow-x-auto rounded-md border border-slate-200">
        <table className="w-full min-w-[34rem] border-collapse text-left text-xs">
          <caption className="sr-only">
            Pedidos denegados por el proveedor externo que no abrieron ninguna alerta local
          </caption>
          <thead className="sticky top-0 bg-slate-50 text-slate-600">
            <tr>
              <th scope="col" className="px-3 py-2 font-medium">Pedido</th>
              <th scope="col" className="px-3 py-2 font-medium">Fecha</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">Monto</th>
              <th scope="col" className="px-3 py-2 font-medium">País</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">Score local</th>
            </tr>
          </thead>
          <tbody>
            {denials.items.map((item) => (
              <tr key={item.merchantReferenceId} className="border-t border-slate-100">
                <th scope="row" className="px-3 py-2 font-normal text-slate-900">
                  {item.merchantReferenceId}
                </th>
                <td className="px-3 py-2 text-slate-700">{formatDate(item.occurredAt)}</td>
                <td className="px-3 py-2 text-right tabular-nums text-slate-700">
                  {formatAmount(item.amountCents, item.currencyCode)}
                </td>
                <td className="px-3 py-2 text-slate-700">{item.countryCode}</td>
                <td className="px-3 py-2 text-right tabular-nums text-slate-700">
                  {item.localRiskScore === null ? "sin puntuar" : formatCount(item.localRiskScore)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
