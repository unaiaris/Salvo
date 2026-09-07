import Link from "next/link";
import { NoSeverityBadge, SeverityBadge } from "@/components/severity-badge";
import type { AlertListItem, Language } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * The queue itself.
 *
 * Two score columns, not one: the snapshot is the premise the alert was opened on and the current
 * score is what the corpus says now. Collapsing them would hide exactly the movement the analyst has
 * to acknowledge before she can pass a verdict.
 */
export function AlertTable({
  alerts,
  language,
}: {
  readonly alerts: readonly AlertListItem[];
  readonly language: Language;
}) {
  const f = formatting(language);
  const { t } = f;

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
      <table className="w-full border-collapse text-left text-sm">
        <caption className="sr-only">{t.alertsPage.tableCaption}</caption>
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-600">
            <th scope="col" className="px-4 py-3 font-semibold">
              {t.alertsPage.columnOrder}
            </th>
            <th scope="col" className="px-4 py-3 font-semibold">
              {t.alertsPage.columnSnapshotSeverity}
            </th>
            <th scope="col" className="px-4 py-3 text-right font-semibold">
              {t.alertsPage.columnSnapshotScore}
            </th>
            <th scope="col" className="px-4 py-3 text-right font-semibold">
              {t.alertsPage.columnCurrentScore}
            </th>
            <th scope="col" className="px-4 py-3 text-right font-semibold">
              {t.alertsPage.columnAmount}
            </th>
            <th scope="col" className="px-4 py-3 font-semibold">
              {t.alertsPage.columnOccurred}
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
                  {t.alertsPage.buyerAndCountry(alert.buyerReferenceId, alert.countryCode)}
                </span>
              </th>
              <td className="px-4 py-3">
                <SeverityBadge severity={alert.severity} language={language} />
                {alert.hasBandDivergence && (
                  <span className="mt-1 block text-xs font-medium text-amber-800">
                    {t.alertsPage.bandChanged}
                  </span>
                )}
              </td>
              <td className="px-4 py-3 text-right tabular-nums">{alert.riskScoreSnapshot}</td>
              <td className="px-4 py-3 text-right tabular-nums">
                {alert.currentRiskScore === null ? (
                  <span className="text-slate-500">{t.alertsPage.noCurrentEvaluation}</span>
                ) : (
                  <>
                    {alert.currentRiskScore}
                    <span className="mt-0.5 block">
                      {alert.currentSeverity === null ? (
                        <NoSeverityBadge language={language} />
                      ) : (
                        <SeverityBadge severity={alert.currentSeverity} language={language} />
                      )}
                    </span>
                  </>
                )}
              </td>
              <td className="px-4 py-3 text-right tabular-nums">
                {f.formatAmount(alert.amountCents, alert.currencyCode)}
              </td>
              <td className="px-4 py-3 whitespace-nowrap">{f.formatDate(alert.occurredAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
