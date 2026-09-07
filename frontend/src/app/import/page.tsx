import Link from "next/link";
import { FailureNotice } from "@/components/failure-notice";
import type { ApiResult } from "@/lib/api/failures";
import type { SeedPreview } from "@/lib/api/contract";
import { Provenance } from "@/components/provenance";
import { fetchCapabilities, fetchDashboard, fetchSeedPreview } from "@/lib/api/console";
import { IMPORT_MAX_FILE_BYTES, SEED_CONFLICT } from "@/lib/api/contract";
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
  // Asked for only where the route exists, like the quality metrics of the dashboard. It says
  // what loading the corpus would do, so a database that cannot take it is announced here rather
  // than discovered by pressing the button.
  const demoEnabled = capabilities.ok && capabilities.value.demoDataEnabled;
  const seedPreview = demoEnabled ? await fetchSeedPreview() : null;

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

      {demoEnabled && (
        <ActionSection
          title="Corpus de demostración"
          description={
            "Trescientos pedidos sintéticos con sus etiquetas de fraude, pensados para poder medir "
            + "el criterio: incluye fraude que las reglas locales no pueden ver y pedidos legítimos "
            + "que sí marcan. La carga es idempotente: repetirla no duplica nada. Esta sección "
            + "existe solo porque esta instancia se declara de demostración."
          }
        >
          <SeedConflictNotice preview={seedPreview} />
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
 * What loading the corpus would run into, said before the button is pressed.
 *
 * The two causes need different words. An earlier version of this same corpus cannot coexist with
 * the current one — an order is immutable and both versions use the same merchant references — so
 * the answer is a new database. Imported orders that happen to collide are somebody's file, and the
 * answer is to leave the corpus alone.
 *
 * A preview that failed renders nothing: it is a courtesy, and the load itself still refuses with
 * its own message. Announcing "we could not check" would be noise on a screen that has none.
 */
function SeedConflictNotice({ preview }: { readonly preview: ApiResult<SeedPreview> | null }) {
  if (preview === null || !preview.ok || preview.value.conflict === null) {
    return null;
  }

  const previous = preview.value.conflict === SEED_CONFLICT.previousCorpus;

  return (
    <section
      role="status"
      aria-labelledby="seed-conflict-title"
      className="rounded-md border-l-4 border-amber-500 bg-amber-50 p-4 text-sm leading-6 text-amber-950"
    >
      <h3 id="seed-conflict-title" className="font-semibold">
        {previous
          ? "Esta base tiene una versión anterior del corpus de demostración"
          : "Esta base tiene pedidos importados con las mismas referencias"}
      </h3>
      <p className="mt-1">
        {previous
          ? `Cargar la versión ${preview.value.datasetVersion} exige una base nueva: un pedido es `
            + "inmutable, así que las dos versiones no pueden convivir bajo las mismas referencias "
            + "de comercio. La base actual no se toca ni se pierde: deja de ser la de demostración."
          : "Los pedidos que ya están usan las mismas referencias que la fixture y tienen otros "
            + "datos. La carga se cancela entera antes que pisar ninguno."}
      </p>
    </section>
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
