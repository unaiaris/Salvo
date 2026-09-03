import Link from "next/link";
import { NoSeverityBadge, SeverityBadge } from "@/components/severity-badge";
import type { AlertListItem } from "@/lib/api/contract";
import { formatAmount, formatDate } from "@/lib/format";

/**
 * The queue itself.
 *
 * Two score columns, not one: the snapshot is the premise the alert was opened on and the current
 * score is what the corpus says now. Collapsing them would hide exactly the movement the analyst has
 * to acknowledge before she can pass a verdict.
 */
export function AlertTable({ alerts }: { readonly alerts: readonly AlertListItem[] }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
      <table className="w-full border-collapse text-left text-sm">
        <caption className="sr-only">
          Alertas abiertas, de mayor a menor score local de la evaluación vigente
        </caption>
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-600">
            <th scope="col" className="px-4 py-3 font-semibold">
              Pedido
            </th>
            <th scope="col" className="px-4 py-3 font-semibold">
              Severidad del snapshot
            </th>
            <th scope="col" className="px-4 py-3 text-right font-semibold">
              Score del snapshot
            </th>
            <th scope="col" className="px-4 py-3 text-right font-semibold">
              Score vigente
            </th>
            <th scope="col" className="px-4 py-3 text-right font-semibold">
              Monto
            </th>
            <th scope="col" className="px-4 py-3 font-semibold">
              Ocurrió
            </th>
          </tr>
        </thead>
        <tbody>
          {alerts.map((alert) => (
            <tr key={alert.id} className="border-b border-slate-100 last:border-b-0">
              <th scope="row" className="px-4 py-3 font-normal">
                <Link
                  href={`/alerts/${alert.id}`}
                  className="font-medium text-slate-900 underline underline-offset-4 hover:text-slate-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
                >
                  {alert.merchantReferenceId}
                </Link>
                <span className="mt-0.5 block text-xs text-slate-500">
                  Comprador {alert.buyerReferenceId} · {alert.countryCode}
                </span>
              </th>
              <td className="px-4 py-3">
                <SeverityBadge severity={alert.severity} />
                {alert.hasBandDivergence && (
                  <span className="mt-1 block text-xs font-medium text-amber-800">
                    La banda vigente cambió
                  </span>
                )}
              </td>
              <td className="px-4 py-3 text-right tabular-nums">{alert.riskScoreSnapshot}</td>
              <td className="px-4 py-3 text-right tabular-nums">
                {alert.currentRiskScore === null ? (
                  <span className="text-slate-500">sin evaluación vigente</span>
                ) : (
                  <>
                    {alert.currentRiskScore}
                    <span className="mt-0.5 block">
                      {alert.currentSeverity === null ? (
                        <NoSeverityBadge />
                      ) : (
                        <SeverityBadge severity={alert.currentSeverity} />
                      )}
                    </span>
                  </>
                )}
              </td>
              <td className="px-4 py-3 text-right tabular-nums">
                {formatAmount(alert.amountCents, alert.currencyCode)}
              </td>
              <td className="px-4 py-3 whitespace-nowrap">{formatDate(alert.occurredAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
