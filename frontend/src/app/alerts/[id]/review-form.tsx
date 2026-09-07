"use client";

import { useActionState, useState } from "react";
import { ALERT_STATUS, REVIEW_NOTE_MAX_LENGTH, type Language } from "@/lib/api/contract";
import { messagesFor, type Dictionary } from "@/lib/i18n/dictionary";
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
  explanationId,
  requiresAcknowledgement,
  divergenceSummary,
  language,
}: {
  readonly alertId: string;
  /**
   * The explanation on screen while this verdict is formed, or empty when there is none.
   *
   * It travels as a hidden field and nothing else: the note is never seeded with the summary, and
   * the form has no way to read a word of it. Decision D10 records what could have been read; it
   * does not put the provider's prose under a human signature.
   */
  readonly explanationId: string;
  readonly requiresAcknowledgement: boolean;
  /** Empty unless `requiresAcknowledgement`; the sentence the analyst has to confirm. */
  readonly divergenceSummary: string;
  readonly language: Language;
}) {
  const [state, formAction, pending] = useActionState(reviewAlert, INITIAL_REVIEW_STATE);

  return (
    <form action={formAction} className="flex flex-col gap-5">
      <input type="hidden" name="alertId" value={alertId} />
      <input type="hidden" name="explanationId" value={explanationId} />
      <ReviewFields
        key={state.submissionId}
        state={state}
        requiresAcknowledgement={requiresAcknowledgement}
        divergenceSummary={divergenceSummary}
        pending={pending}
        t={messagesFor(language)}
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
  t,
}: {
  readonly state: ReviewFormState;
  readonly requiresAcknowledgement: boolean;
  readonly divergenceSummary: string;
  readonly pending: boolean;
  readonly t: Dictionary;
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
            {t.alertDetail.reviewAcknowledgeTitle}
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
            {t.alertDetail.reviewAcknowledgeLabel}
          </label>
        </div>
      )}

      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-semibold text-slate-900">
          {t.alertDetail.reviewLegend}
        </legend>
        {REVIEW_CHOICES.map((choice) => {
          const safe = choice === ALERT_STATUS.confirmedSafe;

          return (
            <label key={choice} className="flex items-start gap-2 text-sm text-slate-800">
              <input
                type="radio"
                name="newStatus"
                value={choice}
                checked={status === choice}
                onChange={() => {
                  setStatus(choice);
                }}
                className="mt-1 size-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
              />
              <span>
                <span className="font-medium">
                  {safe ? t.alertDetail.reviewSafeLabel : t.alertDetail.reviewFraudLabel}
                </span>
                <span className="block text-xs text-slate-600">
                  {safe ? t.alertDetail.reviewSafeHint : t.alertDetail.reviewFraudHint}
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <label className="flex flex-col gap-1 text-sm">
        <span className="font-semibold text-slate-900">{t.alertDetail.reviewNoteLabel}</span>
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
          {t.alertDetail.reviewNoteHint(String(REVIEW_NOTE_MAX_LENGTH))}
        </span>
      </label>

      {blockedByDivergence && (
        <p className="text-sm font-medium text-amber-900">{t.alertDetail.reviewBlocked}</p>
      )}

      <div>
        <button
          type="submit"
          disabled={!canSubmit}
          className="inline-flex rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-400 hover:bg-slate-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {pending ? t.alertDetail.reviewSubmitPending : t.alertDetail.reviewSubmit}
        </button>
      </div>

      {state.outcome !== "idle" && <ReviewOutcome state={state} t={t} />}
    </>
  );
}

function ReviewOutcome({
  state,
  t,
}: {
  readonly state: ReviewFormState;
  readonly t: Dictionary;
}) {
  const failed = state.outcome === "failed";

  /*
    El rol depende de cómo salió la acción, y no es un detalle de estilo.

    `alert` es asertivo: interrumpe lo que el lector esté diciendo en ese momento. Es lo correcto
    para un fallo, que hay que oír antes de seguir, y es de más para un éxito, que solo hay que
    saber. Un éxito va en `status`, que espera a que el lector termine la frase en curso.
  */
  return (
    <section
      role={failed ? "alert" : "status"}
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
        <p className="mt-2 text-xs opacity-80">
          {t.common.technicalDetail(state.technicalDetail)}
        </p>
      )}
    </section>
  );
}
