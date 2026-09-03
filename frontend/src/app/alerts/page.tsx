import { FailureNotice } from "@/components/failure-notice";
import { Provenance } from "@/components/provenance";
import { fetchOpenAlerts, fetchOrderCount } from "@/lib/api/alerts";
import type { ScoringRun } from "@/lib/api/contract";
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

export const metadata = { title: "Alertas · Salvo" };

export default async function AlertsPage() {
  const alerts = await fetchOpenAlerts();

  if (!alerts.ok) {
    return (
      <div className="flex flex-col gap-6">
        <Heading />
        <FailureNotice failure={alerts.failure} />
      </div>
    );
  }

  const feed = alerts.value;

  return (
    <div className="flex flex-col gap-6">
      <Heading />
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <Provenance run={feed.currentRun} />
        <p className="text-sm text-slate-600">
          {feed.totalCount === 1
            ? "1 alerta abierta"
            : `${String(feed.totalCount)} alertas abiertas`}
        </p>
      </div>
      {feed.items.length === 0 ? (
        <EmptyFeed currentRun={feed.currentRun} />
      ) : (
        <AlertTable alerts={feed.items} />
      )}
    </div>
  );
}

function Heading() {
  return (
    <div className="flex flex-col gap-2">
      <h1 className="text-3xl font-semibold tracking-tight text-slate-950">Cola de alertas</h1>
      <p className="max-w-3xl text-sm leading-6 text-slate-600">
        Alertas abiertas, de mayor a menor score local de la evaluación vigente. La severidad de la
        tabla es la del snapshot con el que se abrió cada alerta; el score vigente es lo que dice el
        corpus ahora.
      </p>
    </div>
  );
}

/**
 * Which of the three empty states applies is not a question the alert feed can answer on its own:
 * an empty corpus and an unscored corpus both come back as an empty list with no run. The order
 * count settles it, and it is only asked for on this path.
 */
async function EmptyFeed({ currentRun }: { readonly currentRun: ScoringRun | null }) {
  if (currentRun !== null) {
    return <NoOpenAlertsEmptyState run={currentRun} />;
  }

  const orders = await fetchOrderCount();

  if (!orders.ok) {
    return <FailureNotice failure={orders.failure} />;
  }

  return orders.value === 0 ? (
    <NoOrdersEmptyState />
  ) : (
    <NoScoringRunEmptyState orderCount={orders.value} />
  );
}
