import { FailureNotice } from "@/components/failure-notice";
import { Provenance } from "@/components/provenance";
import { fetchOpenAlerts, fetchOrderCount } from "@/lib/api/alerts";
import { deploymentLanguage } from "@/lib/api/console";
import type { Language, ScoringRun } from "@/lib/api/contract";
import { formatting } from "@/lib/format";
import { AlertTable } from "./alert-table";
import {
  NoOpenAlertsEmptyState,
  NoOrdersEmptyState,
  NoScoringRunEmptyState,
} from "./empty-states";

/**
 * Without this the route is prerendered during `next build`, its fetch runs once with no API
 * listening, and whatever it renders then — an error page, most likely — is frozen into static HTML
 * and served from there on. The gate builds with no API running, so this is not a hypothetical.
 */
export const dynamic = "force-dynamic";

export async function generateMetadata() {
  return { title: formatting(await deploymentLanguage()).t.meta.alerts };
}

export default async function AlertsPage() {
  const [language, alerts] = await Promise.all([deploymentLanguage(), fetchOpenAlerts()]);
  const f = formatting(language);

  if (!alerts.ok) {
    return (
      <div className="flex flex-col gap-6">
        <Heading language={language} />
        <FailureNotice failure={alerts.failure} language={language} />
      </div>
    );
  }

  const feed = alerts.value;

  return (
    <div className="flex flex-col gap-6">
      <Heading language={language} />
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <Provenance run={feed.currentRun} language={language} />
        <p className="text-sm text-slate-600">
          {f.t.alertsPage.openCount(feed.totalCount, f.formatCount(feed.totalCount))}
        </p>
      </div>
      {feed.items.length === 0 ? (
        <EmptyFeed currentRun={feed.currentRun} language={language} />
      ) : (
        <AlertTable alerts={feed.items} language={language} />
      )}
    </div>
  );
}

function Heading({ language }: { readonly language: Language }) {
  const { t } = formatting(language);

  return (
    <div className="flex flex-col gap-2">
      <h1 className="text-3xl font-semibold tracking-tight text-slate-950">{t.alertsPage.title}</h1>
      <p className="max-w-3xl text-sm leading-6 text-slate-600">{t.alertsPage.lead}</p>
    </div>
  );
}

/**
 * Which of the three empty states applies is not a question the alert feed can answer on its own:
 * an empty corpus and an unscored corpus both come back as an empty list with no run. The order
 * count settles it, and it is only asked for on this path.
 */
async function EmptyFeed({
  currentRun,
  language,
}: {
  readonly currentRun: ScoringRun | null;
  readonly language: Language;
}) {
  if (currentRun !== null) {
    return <NoOpenAlertsEmptyState run={currentRun} language={language} />;
  }

  const orders = await fetchOrderCount();

  if (!orders.ok) {
    return <FailureNotice failure={orders.failure} language={language} />;
  }

  return orders.value === 0 ? (
    <NoOrdersEmptyState language={language} />
  ) : (
    <NoScoringRunEmptyState orderCount={orders.value} language={language} />
  );
}
