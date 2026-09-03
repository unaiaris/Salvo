import type { ReactNode } from "react";
import { SeverityBadge } from "@/components/severity-badge";
import {
  DASHBOARD_SEVERITY_ORDER,
  type DashboardAmountAtRisk,
  type DashboardOpenAlerts,
  type DashboardReportedFraud,
  type DashboardSignal,
} from "@/lib/api/contract";
import { formatAmount, formatCount, formatPercent, ruleLabel } from "@/lib/format";

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
