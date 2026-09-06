"use client";

import { useActionState } from "react";
import { explainEvaluation } from "./explanation-action";
import {
  INITIAL_EXPLANATION_STATE,
  type ExplanationActionState,
  type ExplanationAsk,
} from "./explanation-state";

/**
 * What the button says, per question.
 *
 * "Volver a intentar" is not offered for a template that changed: nothing failed, and a reader who
 * is told to retry looks for the error that is not there. Writing the paragraph again with the
 * current template is a different act and says so.
 */
const LABELS: Readonly<Record<Exclude<ExplanationAsk, "none">, string>> = {
  first: "Explicar esta evaluación",
  retry: "Volver a intentar la explicación",
  currentTemplate: "Redactar con la plantilla vigente",
};

/**
 * The one button of the explanation block.
 *
 * Its props are an id, two flags and two sentences, all decided on the server. No `AlertDetail`, no
 * explanation object and — the point of the whole arrangement — no summary: the text lives in the
 * server-rendered part of the block, and this component could not put a word of it on screen even if
 * somebody asked it to.
 *
 * `ask` names the situation rather than the request. The API decides whether a repeat is a retry, a
 * no-op or a refusal; the action is what turns one of these three into the regeneration flag, and
 * only `retry` becomes one.
 *
 * The labels are written here rather than handed down as props, and the smoke is what settled it: a
 * prop is serialised into the RSC payload whether or not anything renders it, so a page that had
 * stopped offering the button still carried its words in the HTML. Wording that belongs to a
 * control belongs in the control — the same place `ExternalActions` keeps its own.
 */
export function ExplanationActions({
  alertId,
  ask,
}: {
  readonly alertId: string;
  readonly ask: ExplanationAsk;
}) {
  const [state, action, running] = useActionState(explainEvaluation, INITIAL_EXPLANATION_STATE);

  return (
    <div className="flex flex-col gap-3">
      {ask !== "none" && (
        <form action={action}>
          <input type="hidden" name="alertId" value={alertId} />
          <input type="hidden" name="ask" value={ask} />
          <button
            type="submit"
            disabled={running}
            className="inline-flex rounded-md border border-teal-700 px-4 py-2 text-sm font-semibold text-teal-900 disabled:cursor-not-allowed disabled:border-slate-300 disabled:text-slate-400 hover:bg-teal-700 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-teal-900"
          >
            {running ? "Redactando…" : LABELS[ask]}
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
