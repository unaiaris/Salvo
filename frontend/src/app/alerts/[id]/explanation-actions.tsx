"use client";

import { useActionState } from "react";
import { explainEvaluation } from "./explanation-action";
import { INITIAL_EXPLANATION_STATE, type ExplanationActionState } from "./explanation-state";

/**
 * The one button of the explanation block.
 *
 * Its props are an id, two flags and two sentences, all decided on the server. No `AlertDetail`, no
 * explanation object and — the point of the whole arrangement — no summary: the text lives in the
 * server-rendered part of the block, and this component could not put a word of it on screen even if
 * somebody asked it to.
 *
 * `regenerate` is a flag rather than a verdict about what should happen. The API decides whether a
 * repeat is a retry, a no-op or a refusal; this only says which of the two questions is being asked.
 */
export function ExplanationActions({
  alertId,
  regenerate,
  actionLabel,
  pendingLabel,
  canAsk,
}: {
  readonly alertId: string;
  readonly regenerate: boolean;
  readonly actionLabel: string;
  readonly pendingLabel: string;
  readonly canAsk: boolean;
}) {
  const [state, action, running] = useActionState(explainEvaluation, INITIAL_EXPLANATION_STATE);

  return (
    <div className="flex flex-col gap-3">
      {canAsk && (
        <form action={action}>
          <input type="hidden" name="alertId" value={alertId} />
          {regenerate && <input type="hidden" name="regenerate" value="true" />}
          <button
            type="submit"
            disabled={running}
            className="inline-flex rounded-md border border-teal-700 px-4 py-2 text-sm font-semibold text-teal-900 disabled:cursor-not-allowed disabled:border-slate-300 disabled:text-slate-400 hover:bg-teal-700 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-teal-900"
          >
            {running ? pendingLabel : actionLabel}
          </button>
        </form>
      )}

      <Outcome state={state} />
    </div>
  );
}

function Outcome({ state }: { readonly state: ExplanationActionState }) {
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
