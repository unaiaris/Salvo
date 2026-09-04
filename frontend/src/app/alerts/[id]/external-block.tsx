import {
  EXTERNAL_STATUS,
  type AlertDetail,
  type AlertEvaluation,
  type AlertExternalEvaluation,
} from "@/lib/api/contract";
import {
  externalErrorLabel,
  externalSourceLabel,
  externalStatusLabel,
  formatInstant,
  providerLabel,
} from "@/lib/format";
import { ExternalActions } from "./external-actions";

/**
 * The provider's opinion, as a third block beside the snapshot and the current evaluation.
 *
 * The two blocks that were here already describe two moments of the same criterion. This one
 * describes a different criterion altogether, so it never blends with them: no combined verdict, and
 * no arithmetic between the two scores. "Externo 45 contra local 60" means nothing — they are
 * different scales of different systems — so what is contrasted is the two verdicts, each labelled
 * with where it came from.
 */
export function ExternalEvaluationBlock({
  detail,
  triggerEnabled,
}: {
  readonly detail: AlertDetail;
  readonly triggerEnabled: boolean;
}) {
  const external = detail.externalEvaluation;

  return (
    <section
      aria-labelledby="external-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-violet-300 bg-violet-50/40 p-5"
    >
      <div>
        <h2 id="external-title" className="text-lg font-semibold text-slate-900">
          Evaluación externa
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          {external === null
            ? "Una segunda opinión, de un proveedor antifraude externo. Todavía no se pidió ninguna para este pedido."
            : `${providerLabel(external.provider)}. El proveedor opina; la decisión sigue siendo del comercio.`}
        </p>
      </div>

      {external !== null && <ExternalState external={external} />}
      {external !== null && (
        <Divergence external={external} current={detail.currentEvaluation} />
      )}
      {external?.hasContradictoryCallback === true && <Contradiction />}

      <ExternalActions
        orderId={detail.orderId}
        alertId={detail.id}
        externalEvaluationId={external === null ? "" : external.id}
        canRequest={external === null}
        canDeliver={triggerEnabled && external?.status === EXTERNAL_STATUS.pending}
      />
    </section>
  );
}

function ExternalState({ external }: { readonly external: AlertExternalEvaluation }) {
  return (
    <div className="flex flex-col gap-2">
      <p className="text-lg font-semibold text-slate-900">
        {externalStatusLabel(external.status)}
      </p>
      <p className="text-xs leading-5 text-slate-600">
        Pedida el {formatInstant(external.requestedAt)}
        {external.settledAt !== null && external.settledBy !== null
          ? `. Respondida el ${formatInstant(external.settledAt)}, ${externalSourceLabel(external.settledBy)}.`
          : "."}
      </p>
      {external.score !== null && (
        <p className="text-xs leading-5 text-slate-600">
          Score del proveedor: <span className="tabular-nums">{external.score}</span>. Está en la
          escala del proveedor y no se compara con el score local, que es otra escala de otro
          sistema.
        </p>
      )}
      {external.errorCode !== null && (
        <p className="text-sm leading-6 text-slate-800">{externalErrorLabel(external.errorCode)}.</p>
      )}
      {external.lastErrorCode !== null && external.status === EXTERNAL_STATUS.pending && (
        <p className="text-sm leading-6 text-amber-900">
          Último intento fallido: {externalErrorLabel(external.lastErrorCode).toLowerCase()}. La
          evaluación sigue esperando al proveedor: un intento que no llegó a respuesta no es un
          veredicto.
        </p>
      )}
    </div>
  );
}

/**
 * The two opinions side by side when they disagree, each with its provenance and neither combined
 * into a single answer.
 */
function Divergence({
  external,
  current,
}: {
  readonly external: AlertExternalEvaluation;
  readonly current: AlertEvaluation | null;
}) {
  if (current === null) {
    return null;
  }

  const externalVerdict = verdictOf(external.status);
  if (externalVerdict === null || externalVerdict === current.isFlagged) {
    return null;
  }

  return (
    <div
      role="note"
      className="rounded-md border border-violet-400 bg-white p-3 text-sm leading-6 text-slate-800"
    >
      <p className="font-semibold text-slate-900">Los dos criterios no coinciden</p>
      <ul className="mt-2 flex flex-col gap-1">
        <li>
          <span className="font-medium">Motor local:</span>{" "}
          {current.isFlagged
            ? "marcó el pedido por encima del umbral."
            : "dejó el pedido por debajo del umbral."}{" "}
          Reglas deterministas sobre la historia del comprador.
        </li>
        <li>
          <span className="font-medium">Proveedor externo:</span>{" "}
          {externalVerdict ? "denegó el pedido." : "aprobó el pedido."}{" "}
          {external.settledBy === null
            ? ""
            : `Llegó ${externalSourceLabel(external.settledBy)}.`}
        </li>
      </ul>
      <p className="mt-2">
        No se combinan en un veredicto único ni se comparan sus scores. La discrepancia es
        información para quien revisa, no una operación aritmética.
      </p>
    </div>
  );
}

function Contradiction() {
  return (
    <p
      role="status"
      className="rounded-md border border-amber-400 bg-amber-50 p-3 text-sm leading-6 text-amber-950"
    >
      El proveedor envió un veredicto contradictorio: después de responder, mandó otro distinto sobre
      la misma evaluación. Vale el primero —una evaluación con veredicto no se reabre— y la
      contradicción queda registrada en vez de descartarse en silencio.
    </p>
  );
}

/**
 * The external verdict as the same boolean the local one uses: denied is "this looks like fraud".
 * `PENDING` and `ERROR` are not verdicts and have nothing to disagree with.
 */
function verdictOf(status: string): boolean | null {
  if (status === EXTERNAL_STATUS.denied) {
    return true;
  }

  return status === EXTERNAL_STATUS.approved ? false : null;
}