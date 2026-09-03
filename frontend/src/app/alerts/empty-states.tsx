import Link from "next/link";
import type { ScoringRun } from "@/lib/api/contract";
import { scoringRunLabel } from "@/components/provenance";

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
export function NoOrdersEmptyState() {
  return (
    <EmptyState
      title="Todavía no hay pedidos"
      body="La base está vacía, así que no hay nada que puntuar ni nada que revisar. Importá un archivo CSV o JSON, o cargá el corpus de demostración."
      action={{ href: "/import", label: "Ir a importación" }}
    />
  );
}

/** There are orders, but no run ever turned them into evaluations. */
export function NoScoringRunEmptyState({ orderCount }: { readonly orderCount: number }) {
  return (
    <EmptyState
      title="Hay pedidos importados y ninguna corrida de scoring"
      body={`Los ${String(orderCount)} pedidos de la base todavía no se puntuaron, así que no existen evaluaciones ni alertas. La cola se llena recién después de ejecutar una corrida.`}
      action={{ href: "/import", label: "Ejecutar scoring" }}
    />
  );
}

/** The corpus was scored and nothing reached the alerting floor. */
export function NoOpenAlertsEmptyState({ run }: { readonly run: ScoringRun }) {
  return (
    <EmptyState
      title="No hay alertas abiertas"
      body={`La ${scoringRunLabel(run)} no dejó ninguna alerta pendiente de revisión: ningún pedido alcanzó el umbral, o todas las alertas abiertas ya tienen veredicto.`}
    />
  );
}
