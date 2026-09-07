import Link from "next/link";
import { scoringRunLabel } from "@/components/provenance";
import type { Language, ScoringRun } from "@/lib/api/contract";
import { formatting } from "@/lib/format";

/**
 * Three empty feeds, three different situations, three different instructions.
 *
 * An empty queue can mean the corpus is empty, that it was imported and never scored, or that it was
 * scored and nothing crossed the alerting floor. Only the last one is good news, and collapsing them
 * into "no hay alertas" would leave the analyst waiting for a queue that nothing is going to fill.
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
      aria-labelledby="empty-title"
      className="rounded-lg border border-slate-200 bg-white p-8 text-center"
    >
      <h2 id="empty-title" className="text-lg font-semibold text-slate-900">
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
export function NoOrdersEmptyState({ language }: { readonly language: Language }) {
  const { t } = formatting(language);

  return (
    <EmptyState
      title={t.alertsPage.emptyNoOrdersTitle}
      body={t.alertsPage.emptyNoOrdersBody}
      action={{ href: "/import", label: t.common.goToImport }}
    />
  );
}

/** There are orders, but no run ever turned them into evaluations. */
export function NoScoringRunEmptyState({
  orderCount,
  language,
}: {
  readonly orderCount: number;
  readonly language: Language;
}) {
  const f = formatting(language);

  return (
    <EmptyState
      title={f.t.alertsPage.emptyNoRunTitle}
      body={f.t.alertsPage.emptyNoRunBody(f.formatCount(orderCount))}
      action={{ href: "/import", label: f.t.common.runScoring }}
    />
  );
}

/** The corpus was scored and nothing reached the alerting floor. */
export function NoOpenAlertsEmptyState({
  run,
  language,
}: {
  readonly run: ScoringRun;
  readonly language: Language;
}) {
  const { t } = formatting(language);

  return (
    <EmptyState
      title={t.alertsPage.emptyNoAlertsTitle}
      body={t.alertsPage.emptyNoAlertsBody(scoringRunLabel(run, language))}
    />
  );
}
