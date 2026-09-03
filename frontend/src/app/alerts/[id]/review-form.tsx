"use client";

import { useActionState, useState } from "react";
import { REVIEW_NOTE_MAX_LENGTH } from "@/lib/api/contract";
import { reviewAlert } from "./review-action";
import { INITIAL_REVIEW_STATE, REVIEW_CHOICES, type ReviewFormState } from "./review-state";

/**
 * Every prop here is a primitive, and that is a rule rather than a coincidence.
 *
 * In React Server Components the props of a client component are serialised whole into the RSC
 * payload inside the HTML. Handing this form the `AlertDetail` object would ship the entire object
 * to the browser — every field of it, including the ones nothing renders and the ones a later stage
 * adds. So the server does the reading and the formatting, and what crosses the boundary is an id, a
 * flag and a sentence.
 *
 * The server action is imported rather than passed as a prop, which keeps the props primitive and
 * the alert id travels in a hidden field.
 */
export function ReviewForm({
  alertId,
  requiresAcknowledgement,
  divergenceSummary,
}: {
  readonly alertId: string;
  readonly requiresAcknowledgement: boolean;
  /** Empty unless `requiresAcknowledgement`; the sentence the analyst has to confirm. */
  readonly divergenceSummary: string;
}) {
  const [state, formAction, pending] = useActionState(reviewAlert, INITIAL_REVIEW_STATE);

  return (
    <form action={formAction} className="flex flex-col gap-5">
      <input type="hidden" name="alertId" value={alertId} />
      <ReviewFields
        key={state.submissionId}
        state={state}
        requiresAcknowledgement={requiresAcknowledgement}
        divergenceSummary={divergenceSummary}
        pending={pending}
      />
    </form>
  );
}

/**
 * The fields are controlled, and they are seeded from whatever the last attempt submitted.
 *
 * React 19 resets a `<form action>` when the action settles, error included. Being controlled is not
 * enough on its own: the reset writes the DOM directly, and for a radio or a checkbox React sees no
 * state change afterwards and never writes it back, so the box ends up visually clear while the
 * component still believes it is set. Keying this subtree on `submissionId` sidesteps the whole
 * problem — every settled attempt mounts fresh fields whose initial state is exactly what was sent.
 * A `409` therefore comes back with the note, the verdict and the acknowledgement still in place.
 */
function ReviewFields({
  state,
  requiresAcknowledgement,
  divergenceSummary,
  pending,
}: {
  readonly state: ReviewFormState;
  readonly requiresAcknowledgement: boolean;
  readonly divergenceSummary: string;
  readonly pending: boolean;
}) {
  const [note, setNote] = useState(state.submittedNote);
  const [status, setStatus] = useState(state.submittedStatus);
  const [acknowledged, setAcknowledged] = useState(state.acknowledged);

  const blockedByDivergence = requiresAcknowledgement && !acknowledged;
  const canSubmit = status !== "" && !blockedByDivergence && !pending;

  return (
    <>
      {requiresAcknowledgement && (
        <div className="rounded-lg border-2 border-amber-400 bg-amber-50 p-4">
          <h3 className="text-sm font-semibold text-amber-950">
            El corpus cambió desde que se abrió esta alerta
          </h3>
          <p className="mt-1 text-sm leading-6 text-amber-900">{divergenceSummary}</p>
          <label className="mt-3 flex items-start gap-2 text-sm font-medium text-amber-950">
            <input
              type="checkbox"
              name="acknowledgedDivergence"
              checked={acknowledged}
              onChange={(event) => {
                setAcknowledged(event.target.checked);
              }}
              className="mt-1 size-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-700"
            />
            Leí en qué cambió la evaluación vigente y quiero emitir el veredicto igual.
          </label>
        </div>
      )}

      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-semibold text-slate-900">Veredicto</legend>
        {REVIEW_CHOICES.map((choice) => (
          <label key={choice.value} className="flex items-start gap-2 text-sm text-slate-800">
            <input
              type="radio"
              name="newStatus"
              value={choice.value}
              checked={status === choice.value}
              onChange={() => {
                setStatus(choice.value);
              }}
              className="mt-1 size-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
            />
            <span>
              <span className="font-medium">{choice.label}</span>
              <span className="block text-xs text-slate-600">{choice.hint}</span>
            </span>
          </label>
        ))}
      </fieldset>

      <label className="flex flex-col gap-1 text-sm">
        <span className="font-semibold text-slate-900">Nota de la revisión (opcional)</span>
        <textarea
          name="note"
          rows={4}
          maxLength={REVIEW_NOTE_MAX_LENGTH}
          value={note}
          onChange={(event) => {
            setNote(event.target.value);
          }}
          aria-describedby="note-hint"
          className="rounded-md border border-slate-300 bg-white p-2 text-sm text-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        />
        <span id="note-hint" className="text-xs text-slate-600">
          Hasta {REVIEW_NOTE_MAX_LENGTH} caracteres. Queda en la auditoría junto al veredicto.
        </span>
      </label>

      {blockedByDivergence && (
        <p className="text-sm font-medium text-amber-900">
          Marcá la casilla de arriba para poder enviar el veredicto.
        </p>
      )}

      <div>
        <button
          type="submit"
          disabled={!canSubmit}
          className="inline-flex rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-400 hover:bg-slate-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {pending ? "Registrando…" : "Registrar veredicto"}
        </button>
      </div>

      {state.outcome !== "idle" && <ReviewOutcome state={state} />}
    </>
  );
}

function ReviewOutcome({ state }: { readonly state: ReviewFormState }) {
  const failed = state.outcome === "failed";

  return (
    <section
      role="alert"
      className={
        failed
          ? "rounded-lg border border-rose-300 bg-rose-50 p-4 text-rose-950"
          : "rounded-lg border border-emerald-300 bg-emerald-50 p-4 text-emerald-950"
      }
    >
      <h3 className="text-sm font-semibold">{state.title}</h3>
      <p className="mt-1 text-sm leading-6">{state.body}</p>
      {state.recovery !== "" && (
        <p className="mt-1 text-sm font-medium leading-6">{state.recovery}</p>
      )}
      {state.technicalDetail !== "" && (
        <p className="mt-2 text-xs opacity-80">Detalle técnico de la API: {state.technicalDetail}</p>
      )}
    </section>
  );
}
