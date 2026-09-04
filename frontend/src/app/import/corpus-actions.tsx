"use client";

import { useActionState } from "react";
import {
  deliverAllCallbacks,
  executeScoringRun,
  requestCorpusExternal,
  seedDemoCorpus,
} from "./actions";
import { ActionOutcome } from "./action-outcome";
import { INITIAL_ACTION_STATE } from "./action-state";

/**
 * The two actions that take no input: load the demo corpus, and run the scoring.
 *
 * Both are plain buttons over a server action with no form data, so neither receives anything about
 * the corpus. Whether the demo button exists at all is decided on the server from
 * `GET /api/system/capabilities`; this component is not rendered when the deployment says no.
 */
export function SeedDemoButton() {
  const [state, formAction, pending] = useActionState(seedDemoCorpus, INITIAL_ACTION_STATE);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div>
        <button
          type="submit"
          disabled={pending}
          className="inline-flex rounded-md border border-slate-900 px-4 py-2 text-sm font-semibold text-slate-900 disabled:cursor-not-allowed disabled:border-slate-300 disabled:text-slate-400 hover:bg-slate-900 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {pending ? "Cargando corpus…" : "Cargar corpus de demostración"}
        </button>
      </div>
      <ActionOutcome state={state} />
    </form>
  );
}

/**
 * The step that produces evaluations and alerts.
 *
 * Nothing else triggers it: not the import, not opening the feed, not opening the dashboard. That is
 * the whole reason this button exists on screen — decision 39 — and the surrounding copy says it in
 * as many words, because a console where importing silently did the scoring would be a console that
 * hides when the corpus was last read.
 */
export function RunScoringButton() {
  const [state, formAction, pending] = useActionState(executeScoringRun, INITIAL_ACTION_STATE);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div>
        <button
          type="submit"
          disabled={pending}
          className="inline-flex rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-400 hover:bg-slate-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {pending ? "Ejecutando corrida…" : "Ejecutar scoring"}
        </button>
      </div>
      {pending && (
        <p className="text-sm text-slate-600">
          La corrida evalúa el corpus en orden temporal. En un corpus de trescientos pedidos tarda
          unos segundos.
        </p>
      )}
      <ActionOutcome state={state} />
    </form>
  );
}

/**
 * The two demo actions of the external provider.
 *
 * They exist here rather than on an orders screen because most orders never produce an alert, and
 * the alert detail is the only other place an evaluation can be asked for. Neither button composes a
 * callback or holds a secret: the first asks the API to consult the provider, and the second asks
 * the API to deliver what the provider would have sent.
 */
export function RequestCorpusExternalButton() {
  const [state, formAction, pending] = useActionState(requestCorpusExternal, INITIAL_ACTION_STATE);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div>
        <button
          type="submit"
          disabled={pending}
          className="inline-flex rounded-md border border-violet-700 px-4 py-2 text-sm font-semibold text-violet-900 disabled:cursor-not-allowed disabled:border-slate-300 disabled:text-slate-400 hover:bg-violet-700 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-900"
        >
          {pending ? "Consultando al proveedor…" : "Solicitar evaluación externa del corpus"}
        </button>
      </div>
      {pending && (
        <p className="text-sm text-slate-600">
          Se consulta pedido por pedido, reservando la fila antes de llamar. En un corpus de
          trescientos pedidos tarda unos segundos.
        </p>
      )}
      <ActionOutcome state={state} />
    </form>
  );
}

export function DeliverCallbacksButton() {
  const [state, formAction, pending] = useActionState(deliverAllCallbacks, INITIAL_ACTION_STATE);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div>
        <button
          type="submit"
          disabled={pending}
          className="inline-flex rounded-md border border-violet-700 px-4 py-2 text-sm font-semibold text-violet-900 disabled:cursor-not-allowed disabled:border-slate-300 disabled:text-slate-400 hover:bg-violet-700 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-900"
        >
          {pending ? "Entregando callbacks…" : "Entregar los callbacks del proveedor"}
        </button>
      </div>
      <ActionOutcome state={state} />
    </form>
  );
}
