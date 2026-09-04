import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import { Provenance } from "@/components/provenance";
import { fetchCapabilities, fetchDashboard } from "@/lib/api/console";
import { IMPORT_MAX_FILE_BYTES } from "@/lib/api/contract";
import { formatCount } from "@/lib/format";
import { ActionSection } from "./action-section";
import {
  DeliverCallbacksButton,
  RequestCorpusExternalButton,
  RunScoringButton,
  SeedDemoButton,
} from "./corpus-actions";
import { ImportForm } from "./import-form";

/**
 * Same reason as every other screen that reads the API: prerendering this route during `next build`
 * would run its fetches with nothing listening and freeze the result into static HTML.
 */
export const dynamic = "force-dynamic";

export const metadata = { title: "Importación · Salvo" };

const MAX_FILE_MIB = IMPORT_MAX_FILE_BYTES / (1024 * 1024);

/**
 * Where the corpus comes from and where it gets processed.
 *
 * The page is built around the fact that those are two different steps. `POST /api/order-imports`
 * writes orders and produces nothing else; `POST /api/risk-evaluations:run` is what turns them into
 * evaluations and alerts. Stage 5's design calls this out as decision 39 because the earlier draft
 * of the interface had no screen that ran the scoring at all, which left an analyst importing a file
 * and then waiting forever for a queue that nothing was going to fill.
 */
export default async function ImportPage() {
  const [capabilities, dashboard] = await Promise.all([fetchCapabilities(), fetchDashboard()]);

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-semibold tracking-tight text-slate-950">
          Importación y scoring
        </h1>
        <p className="max-w-3xl text-sm leading-6 text-slate-600">
          Importar escribe pedidos y nada más. Las evaluaciones y las alertas las produce una corrida
          de scoring, que se ejecuta desde acá: hasta que no la corras, la cola de alertas y el
          dashboard siguen mostrando el estado de la corrida anterior.
        </p>
      </div>

      {!capabilities.ok && <FailureNotice failure={capabilities.failure} />}

      {dashboard.ok ? (
        <CorpusStatus
          run={dashboard.value.scoringRun}
          ordersPendingScoring={dashboard.value.ordersPendingScoring}
        />
      ) : (
        <FailureNotice failure={dashboard.failure} />
      )}

      {capabilities.ok && capabilities.value.demoDataEnabled && (
        <ActionSection
          title="Corpus de demostración"
          description={
            "Trescientos pedidos sintéticos con sus etiquetas de fraude, pensados para poder medir "
            + "el criterio. La carga es idempotente: repetirla no duplica nada. Esta sección existe "
            + "solo porque esta instancia se declara de demostración."
          }
        >
          <SeedDemoButton />
        </ActionSection>
      )}

      <ActionSection
        title="Importar un archivo"
        description={
          "CSV o JSON. La validación es estricta por registro y la escritura atómica por archivo: "
          + "los registros rechazados se listan uno por uno y no se escribe ninguno de ellos."
        }
      >
        <ImportForm maxFileMib={MAX_FILE_MIB} />
      </ActionSection>

      <ActionSection
        title="Ejecutar scoring"
        description={
          "Evalúa el corpus completo en orden temporal, con el baseline construido solo con la "
          + "historia anterior a cada pedido, y abre las alertas que correspondan. Es idempotente: "
          + "una evaluación cuyo resultado no cambió se reusa en vez de duplicarse, y una alerta ya "
          + "abierta o ya revisada no se vuelve a abrir."
        }
      >
        <RunScoringButton />
      </ActionSection>

      {capabilities.ok && capabilities.value.externalCallbackTriggerEnabled && (
        <ActionSection
          title="Proveedor antifraude externo"
          description={
            "Una segunda opinión sobre cada pedido, de un proveedor externo simulado. El detalle de "
            + "una alerta permite pedirla de a un pedido; acá se pide para el corpus entero, que es "
            + "lo único que alcanza a los pedidos que nunca abrieron una alerta. Entregar los "
            + "callbacks simula la respuesta que el proveedor mandaría por su cuenta: quien lo pulsa "
            + "elige qué evaluación, nunca qué responde el proveedor. Repetirlo no repite efectos."
          }
        >
          <div className="flex flex-col gap-6">
            <RequestCorpusExternalButton />
            <DeliverCallbacksButton />
          </div>
        </ActionSection>
      )}
    </div>
  );
}

/**
 * What the console is currently working from: which run, and how much of the corpus it does not
 * cover. `ordersPendingScoring` is the number that turns "the queue looks empty" into "nobody has
 * scored these yet", so it is stated here rather than left to be inferred.
 */
function CorpusStatus({
  run,
  ordersPendingScoring,
}: {
  readonly run: { readonly sequence: number; readonly completedAt: string } | null;
  readonly ordersPendingScoring: number;
}) {
  return (
    <section
      aria-labelledby="corpus-status"
      className="rounded-lg border border-slate-200 bg-white p-5"
    >
      <h2 id="corpus-status" className="text-base font-semibold text-slate-900">
        Estado del corpus
      </h2>
      <div className="mt-2">
        <Provenance run={run} />
      </div>
      {ordersPendingScoring > 0 ? (
        <p className="mt-2 text-sm font-medium leading-6 text-amber-900">
          {ordersPendingScoring === 1
            ? "Hay 1 pedido sin puntuar por la corrida vigente."
            : `Hay ${formatCount(ordersPendingScoring)} pedidos sin puntuar por la corrida vigente.`}{" "}
          No aparecen en el dashboard ni pueden generar alertas hasta que ejecutes una corrida.
        </p>
      ) : (
        <p className="mt-2 text-sm leading-6 text-slate-600">
          {run === null
            ? "No hay pedidos sin puntuar porque todavía no hay ninguno en la base."
            : "Todos los pedidos de la base están cubiertos por la corrida vigente."}
        </p>
      )}
      <p className="mt-3 text-sm">
        <Link
          href="/dashboard"
          className="font-medium text-slate-900 underline underline-offset-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          Ver el dashboard
        </Link>
      </p>
    </section>
  );
}
