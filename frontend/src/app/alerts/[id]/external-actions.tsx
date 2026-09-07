"use client";

import { useActionState } from "react";
import type { Language } from "@/lib/api/contract";
import { messagesFor } from "@/lib/i18n/dictionary";
import { deliverCallback, requestExternal } from "./external-action";
import { INITIAL_EXTERNAL_STATE } from "./external-state";

/**
 * The two buttons of the external block.
 *
 * Everything they need is a string and a boolean, decided on the server. No `AlertDetail` and no
 * external evaluation object crosses here — the rule of decision 42 — and neither button ever
 * carries a verdict: the request asks the provider, and the delivery asks the API to ask it. What
 * the provider says is never something this component could choose.
 *
 * The wording is read from the dictionary here rather than passed in, for the reason `E7B` found:
 * a label handed down as a prop rides in the RSC payload of every page whether the button renders
 * or not.
 */
export function ExternalActions({
  orderId,
  alertId,
  externalEvaluationId,
  canRequest,
  canDeliver,
  language,
}: {
  readonly orderId: string;
  readonly alertId: string;
  readonly externalEvaluationId: string;
  readonly canRequest: boolean;
  readonly canDeliver: boolean;
  readonly language: Language;
}) {
  const { alertDetail } = messagesFor(language);
  const [requestState, requestAction, requesting] = useActionState(
    requestExternal,
    INITIAL_EXTERNAL_STATE,
  );
  const [deliverState, deliverAction, delivering] = useActionState(
    deliverCallback,
    INITIAL_EXTERNAL_STATE,
  );

  return (
    <div className="flex flex-col gap-3">
      {canRequest && (
        <form action={requestAction}>
          <input type="hidden" name="orderId" value={orderId} />
          <input type="hidden" name="alertId" value={alertId} />
          <button
            type="submit"
            disabled={requesting}
            className="inline-flex rounded-md border border-violet-700 px-4 py-2 text-sm font-semibold text-violet-900 disabled:cursor-not-allowed disabled:border-slate-300 disabled:text-slate-400 hover:bg-violet-700 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-900"
          >
            {requesting ? alertDetail.externalRequestPending : alertDetail.externalRequestButton}
          </button>
        </form>
      )}

      {canDeliver && (
        <form action={deliverAction} className="flex flex-col gap-2">
          <input type="hidden" name="externalEvaluationId" value={externalEvaluationId} />
          <input type="hidden" name="alertId" value={alertId} />
          <button
            type="submit"
            disabled={delivering}
            className="inline-flex rounded-md border border-violet-700 px-4 py-2 text-sm font-semibold text-violet-900 disabled:cursor-not-allowed disabled:border-slate-300 disabled:text-slate-400 hover:bg-violet-700 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-violet-900"
          >
            {delivering ? alertDetail.externalDeliverPending : alertDetail.externalDeliverButton}
          </button>
          <p className="text-xs leading-5 text-slate-600">
            {alertDetail.externalDeliverHint}
          </p>
        </form>
      )}

      <Outcome state={requestState} />
      <Outcome state={deliverState} />
    </div>
  );
}

function Outcome({ state }: { readonly state: typeof INITIAL_EXTERNAL_STATE }) {
  if (state.outcome === "idle") {
    return null;
  }

  const failed = state.outcome === "failed";

  return (
    <div
      key={state.submissionId}
      role="status"
      className={`rounded-md border p-3 text-sm leading-6 ${
        failed ? "border-red-300 bg-red-50 text-red-950" : "border-slate-300 bg-white text-slate-800"
      }`}
    >
      <p className="font-semibold">{state.title}</p>
      <p className="mt-1">{state.body}</p>
      {state.recovery.length > 0 && <p className="mt-1">{state.recovery}</p>}
      {state.technicalDetail.length > 0 && (
        <p className="mt-2 font-mono text-xs text-slate-600">{state.technicalDetail}</p>
      )}
    </div>
  );
}
