import { EXPLANATION_STATUS, type AlertDetail, type AlertExplanation } from "@/lib/api/contract";
import {
  explanationFailureLabel,
  explanationProviderLabel,
  formatInstant,
  ruleLabel,
} from "@/lib/format";
import { ExplanationActions } from "./explanation-actions";

/**
 * The evaluation of the snapshot, put into words.
 *
 * What is shown is the explanation of the **premise the verdict is being formed on**, never the one
 * of whatever evaluation happens to be current. They describe different moments, and swapping one
 * for the other would let an analyst read a paragraph about an order state nobody decided anything
 * about.
 *
 * The block explains and never advises. There is no recommended action, no badge derived from the
 * severity and no sentence about what to do — decision 53 — because prose sitting inside a block
 * titled "Explicación" is read as the writer's opinion, and the writer here is a provider that is
 * not allowed to have one.
 */
export function ExplanationBlock({ detail }: { readonly detail: AlertDetail }) {
  const explanation = detail.explanation;

  return (
    <section
      aria-labelledby="explanation-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-teal-300 bg-teal-50/40 p-5"
    >
      <div>
        <h2 id="explanation-title" className="text-lg font-semibold text-slate-900">
          Explicación
        </h2>
        <p className="mt-1 text-sm text-slate-600">
          El snapshot que abrió la alerta, contado en palabras. Describe la evaluación: no cambia el
          score, ni la severidad, ni el veredicto.
        </p>
      </div>

      {/*
        The notice precedes the text and never replaces it. An outdated explanation is still the
        record of what could have been read while the verdict was being formed, which is the whole
        reason it is kept; hiding it would destroy that record in the name of tidiness.
      */}
      {explanation?.isOutdated === true && (
        <OutdatedNotice hasCurrent={detail.currentExplanation !== null} />
      )}

      {explanation === null ? <NeverAsked /> : <Written explanation={explanation} />}

      <Actions explanation={explanation} alertId={detail.id} />
    </section>
  );
}

function OutdatedNotice({ hasCurrent }: { readonly hasCurrent: boolean }) {
  return (
    <div
      role="note"
      className="rounded-md border border-amber-400 bg-amber-50 p-3 text-sm leading-6 text-amber-950"
    >
      <p className="font-semibold">Esta explicación describe una evaluación que ya no es la vigente</p>
      <p className="mt-1">
        Se redactó sobre el snapshot con el que se abrió la alerta, y desde entonces el pedido tiene
        otra evaluación. Se conserva porque es el registro de lo que se pudo leer al decidir; las
        señales de ahora están en el bloque «Evaluación vigente».
        {hasCurrent ? " La evaluación vigente ya tiene además su propia explicación escrita." : ""}
      </p>
    </div>
  );
}

function NeverAsked() {
  return (
    <p className="text-sm leading-6 text-slate-700">
      Todavía no se pidió una explicación de esta evaluación. Pedirla no cambia nada del pedido ni de
      la alerta: se le pide el texto al proveedor de explicaciones y se verifica contra la evaluación
      antes de guardarlo.
    </p>
  );
}

function Written({ explanation }: { readonly explanation: AlertExplanation }) {
  if (explanation.status === EXPLANATION_STATUS.ready) {
    return <ReadySummary explanation={explanation} />;
  }

  if (explanation.status === EXPLANATION_STATUS.pending) {
    return (
      <div className="flex flex-col gap-2">
        {/*
          Never "pendiente" on its own: an alert waiting for a verdict, an external evaluation
          waiting for the provider, an order waiting to be scored and this are four different waits.
        */}
        <p className="text-sm font-semibold text-slate-900">Redactando la explicación…</p>
        <p className="text-xs leading-5 text-slate-600">
          Pedida el {formatInstant(explanation.requestedAt)}. Recargá la alerta en unos segundos. Si
          la petición quedó a medias, el próximo pedido retoma la misma fila.
        </p>
      </div>
    );
  }

  return <Failure explanation={explanation} />;
}

function ReadySummary({ explanation }: { readonly explanation: AlertExplanation }) {
  return (
    <div className="flex flex-col gap-2">
      <p className="whitespace-pre-wrap rounded-md border border-teal-200 bg-white p-3 text-sm leading-6 text-slate-900">
        {explanation.summary}
      </p>
      <p className="text-xs leading-5 text-slate-600">
        {/*
          Who wrote it, said plainly. In this build it is a template with no network and no model,
          and an analyst reading a paragraph about her order is entitled to know that before she
          decides how much weight to give it.
        */}
        Redactada por una {explanationProviderLabel(explanation.provider)}, no por un modelo
        {explanation.settledAt === null ? "" : `, el ${formatInstant(explanation.settledAt)}`}.
        Cada cifra y cada regla del texto se verificaron contra esta evaluación antes de guardarlo:
        un texto que no pasa esa comprobación no se guarda ni se muestra.
      </p>
      {explanation.referencedRules.length > 0 && (
        <p className="text-xs leading-5 text-slate-600">
          Reglas citadas: {explanation.referencedRules.map(ruleLabel).join(", ")}.
        </p>
      )}
    </div>
  );
}

function Failure({ explanation }: { readonly explanation: AlertExplanation }) {
  return (
    <div className="flex flex-col gap-2">
      <p className="text-sm font-semibold text-slate-900">
        {explanation.failureCode === null
          ? "La redacción terminó sin texto utilizable"
          : explanationFailureLabel(explanation.failureCode)}
      </p>
      <p className="text-xs leading-5 text-slate-600">
        {explanation.attemptCount === 1
          ? "Se intentó una vez"
          : `Se intentó ${String(explanation.attemptCount)} veces`}
        {explanation.settledAt === null ? "" : `; el último, el ${formatInstant(explanation.settledAt)}`}.{" "}
        {explanation.attemptsExhausted
          ? "Se agotó el presupuesto de intentos, así que no se vuelve a pedir. Un veredicto no necesita explicación para emitirse."
          : "El texto que no se pudo verificar no se guarda ni llega a esta pantalla."}
      </p>
    </div>
  );
}

/**
 * Which of the two questions the button asks, or none at all.
 *
 * Asking again is only ever offered over a failure with attempts left, because that is the only case
 * the API accepts: over a written explanation it refuses — replacing a paragraph somebody may have
 * formed a verdict on is not regenerating it — and over a spent budget it refuses too.
 */
function Actions({
  explanation,
  alertId,
}: {
  readonly explanation: AlertExplanation | null;
  readonly alertId: string;
}) {
  const failedWithAttemptsLeft =
    explanation !== null &&
    explanation.status === EXPLANATION_STATUS.failed &&
    !explanation.attemptsExhausted;

  return (
    <ExplanationActions
      alertId={alertId}
      regenerate={failedWithAttemptsLeft}
      actionLabel={failedWithAttemptsLeft ? "Volver a intentar la explicación" : "Explicar esta evaluación"}
      pendingLabel="Redactando…"
      canAsk={explanation === null || failedWithAttemptsLeft}
    />
  );
}
