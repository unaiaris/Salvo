import Link from "next/link";
import { scoringRunLabel } from "@/components/provenance";
import type { ScoringRun } from "@/lib/api/contract";
import { formatCount } from "@/lib/format";

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
export function NoOrdersDashboard() {
  return (
    <EmptyState
      title="Todavía no hay pedidos"
      body="La base está vacía: no hay monto en riesgo, ni tasa de marcado, ni semanas que dibujar. Importá un archivo CSV o JSON, o cargá el corpus de demostración."
      action={{ href: "/import", label: "Ir a importación" }}
    />
  );
}

/** There are orders, and no run ever turned them into evaluations. */
export function NoScoringRunDashboard({ ordersPendingScoring }: { readonly ordersPendingScoring: number }) {
  return (
    <EmptyState
      title="Hay pedidos importados y ninguna corrida de scoring"
      body={`Los ${formatCount(ordersPendingScoring)} pedidos de la base no tienen evaluación, así que el dashboard no tiene nada que resumir: sin corrida no hay score, ni marcado, ni alertas. Nada de esto se calcula solo.`}
      action={{ href: "/import", label: "Ejecutar scoring" }}
    />
  );
}

/** The corpus was scored and nothing reached the alerting floor. */
export function NoOpenAlertsNotice({ run }: { readonly run: ScoringRun }) {
  return (
    <section
      aria-labelledby="dashboard-no-alerts"
      className="rounded-lg border border-emerald-200 bg-emerald-50 p-5 text-emerald-950"
    >
      <h2 id="dashboard-no-alerts" className="text-base font-semibold">
        No hay alertas abiertas
      </h2>
      <p className="mt-1 text-sm leading-6">
        La {scoringRunLabel(run)} no dejó nada pendiente de revisión: ningún pedido alcanzó el
        umbral, o todas las alertas que se abrieron ya tienen veredicto. Las cifras de riesgo de
        abajo se calculan igual sobre la corrida vigente.
      </p>
    </section>
  );
}
