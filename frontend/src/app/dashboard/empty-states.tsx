import Link from "next/link";
import { scoringRunLabel } from "@/components/provenance";
import type { Language, ScoringRun } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * The same three situations the alert feed distinguishes, told from the dashboard's side.
 *
 * An empty dashboard can mean the base has no orders, that it has orders nobody ever scored, or that
 * it was scored and nothing crossed the alerting floor. Only the third is good news. The dashboard
 * can tell them apart without a second request: without a run every order counts as pending, so
 * `ordersPendingScoring` is what separates an empty base from an unscored one.
 */

const CALL_TO_ACTION =
  "inline-flex rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900";

function EmptyState({
  title,
  body,
  action,
}: {
  readonly title: string;
  readonly body: string;
  readonly action?: { readonly href: string; readonly label: string };
}) {
  return (
    <section
      aria-labelledby="dashboard-empty-title"
      className="rounded-lg border border-slate-200 bg-white p-8 text-center"
    >
      <h2 id="dashboard-empty-title" className="text-lg font-semibold text-slate-900">
        {title}
      </h2>
      <p className="mx-auto mt-2 max-w-xl text-sm leading-6 text-slate-600">{body}</p>
      {action !== undefined && (
        <p className="mt-5">
          <Link href={action.href} className={CALL_TO_ACTION}>
            {action.label}
          </Link>
        </p>
      )}
    </section>
  );
}

/** Nothing was ever imported. */
export function NoOrdersDashboard({ language }: { readonly language: Language }) {
  const { t } = formatting(language);

  return (
    <EmptyState
      title={t.dashboard.emptyNoOrdersTitle}
      body={t.dashboard.emptyNoOrdersBody}
      action={{ href: "/import", label: t.common.goToImport }}
    />
  );
}

/** There are orders, and no run ever turned them into evaluations. */
export function NoScoringRunDashboard({
  ordersPendingScoring,
  language,
}: {
  readonly ordersPendingScoring: number;
  readonly language: Language;
}) {
  const f = formatting(language);

  return (
    <EmptyState
      title={f.t.dashboard.emptyNoRunTitle}
      body={f.t.dashboard.emptyNoRunBody(f.formatCount(ordersPendingScoring))}
      action={{ href: "/import", label: f.t.common.runScoring }}
    />
  );
}

/** The corpus was scored and nothing reached the alerting floor. */
export function NoOpenAlertsNotice({
  run,
  language,
}: {
  readonly run: ScoringRun;
  readonly language: Language;
}) {
  const { t } = formatting(language);

  return (
    <section
      aria-labelledby="dashboard-no-alerts"
      className="rounded-lg border border-emerald-200 bg-emerald-50 p-5 text-emerald-950"
    >
      <h2 id="dashboard-no-alerts" className="text-base font-semibold">
        {t.dashboard.emptyNoAlertsTitle}
      </h2>
      <p className="mt-1 text-sm leading-6">
        {t.dashboard.emptyNoAlertsBody(scoringRunLabel(run, language))}
      </p>
    </section>
  );
}
