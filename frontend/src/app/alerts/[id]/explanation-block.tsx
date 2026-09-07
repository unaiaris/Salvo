import {
  EXPLANATION_STATUS,
  type AlertDetail,
  type AlertExplanation,
  type Language,
} from "@/lib/api/contract";
import { formatting, type Formatting } from "@/lib/format";
import { ExplanationActions } from "./explanation-actions";
import type { ExplanationAsk } from "./explanation-state";

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
 *
 * The paragraph itself arrives already written, in the language of the deployment, because the
 * backend composed it and stored it that way. This block does not translate it and could not: a
 * stored explanation is a record, and a record translated on the way to the screen is a record
 * nobody wrote.
 */
export function ExplanationBlock({
  detail,
  language,
}: {
  readonly detail: AlertDetail;
  readonly language: Language;
}) {
  const explanation = detail.explanation;
  const f = formatting(language);

  return (
    <section
      aria-labelledby="explanation-title"
      className="flex flex-col gap-3 rounded-lg border-2 border-teal-300 bg-teal-50/40 p-5"
    >
      <div>
        <h2 id="explanation-title" className="text-lg font-semibold text-slate-900">
          {f.t.alertDetail.explanationTitle}
        </h2>
        <p className="mt-1 text-sm text-slate-600">{f.t.alertDetail.explanationLead}</p>
      </div>

      {/*
        The notice precedes the text and never replaces it. An outdated explanation is still the
        record of what could have been read while the verdict was being formed, which is the whole
        reason it is kept; hiding it would destroy that record in the name of tidiness.
      */}
      {explanation?.isOutdated === true && (
        <OutdatedNotice hasCurrent={detail.currentExplanation !== null} f={f} />
      )}

      {explanation === null ? <NeverAsked f={f} /> : <Written explanation={explanation} f={f} />}

      <ExplanationActions alertId={detail.id} ask={askOf(explanation)} language={language} />
    </section>
  );
}

function OutdatedNotice({ hasCurrent, f }: { readonly hasCurrent: boolean; readonly f: Formatting }) {
  return (
    <div
      role="note"
      className="rounded-md border border-amber-400 bg-amber-50 p-3 text-sm leading-6 text-amber-950"
    >
      <p className="font-semibold">{f.t.alertDetail.explanationOutdatedTitle}</p>
      <p className="mt-1">
        {f.t.alertDetail.explanationOutdatedBody}
        {hasCurrent ? f.t.alertDetail.explanationOutdatedHasCurrent : ""}
      </p>
    </div>
  );
}

function NeverAsked({ f }: { readonly f: Formatting }) {
  return (
    <p className="text-sm leading-6 text-slate-700">{f.t.alertDetail.explanationNeverAsked}</p>
  );
}

function Written({
  explanation,
  f,
}: {
  readonly explanation: AlertExplanation;
  readonly f: Formatting;
}) {
  if (explanation.status === EXPLANATION_STATUS.ready) {
    return <ReadySummary explanation={explanation} f={f} />;
  }

  if (explanation.status === EXPLANATION_STATUS.pending) {
    return (
      <div className="flex flex-col gap-2">
        {/*
          Never "pendiente" on its own: an alert waiting for a verdict, an external evaluation
          waiting for the provider, an order waiting to be scored and this are four different waits.
        */}
        <p className="text-sm font-semibold text-slate-900">{f.t.alertDetail.explanationWriting}</p>
        <p className="text-xs leading-5 text-slate-600">
          {f.t.alertDetail.explanationWritingHint(f.formatInstant(explanation.requestedAt))}
        </p>
      </div>
    );
  }

  return <Failure explanation={explanation} f={f} />;
}

function ReadySummary({
  explanation,
  f,
}: {
  readonly explanation: AlertExplanation;
  readonly f: Formatting;
}) {
  return (
    <div className="flex flex-col gap-2">
      <p className="whitespace-pre-wrap rounded-md border border-teal-200 bg-white p-3 text-sm leading-6 text-slate-900">
        {explanation.summary}
      </p>
      <p className="text-xs leading-5 text-slate-600">
        {/*
          Who wrote it and with which template, said plainly. In this build it is a template with no
          network and no model, and an analyst reading a paragraph about her order is entitled to
          know that before she decides how much weight to give it. The version belongs in the same
          sentence rather than in a badge: it explains the button below, and on its own it would be
          an alarm about a change of wording.

          One expression rather than three, because adjacent expressions are separate text nodes in
          the server-rendered HTML and the smoke reads that HTML, not the text content of a DOM.
        */}
        {f.t.alertDetail.explanationWrittenBy(
          f.explanationProviderLabel(explanation.provider),
          explanation.templateVersion,
          explanation.settledAt === null
            ? ""
            : f.t.alertDetail.explanationWrittenAt(f.formatInstant(explanation.settledAt)),
        )}{" "}
        {f.t.alertDetail.explanationVerified}
      </p>
      {explanation.referencedRules.length > 0 && (
        <p className="text-xs leading-5 text-slate-600">
          {f.t.alertDetail.explanationCitedRules(
            explanation.referencedRules.map((rule) => f.ruleLabel(rule)).join(", "),
          )}
        </p>
      )}
    </div>
  );
}

function Failure({
  explanation,
  f,
}: {
  readonly explanation: AlertExplanation;
  readonly f: Formatting;
}) {
  return (
    <div className="flex flex-col gap-2">
      <p className="text-sm font-semibold text-slate-900">
        {explanation.failureCode === null
          ? f.t.alertDetail.explanationFailedWithoutCode
          : f.explanationFailureLabel(explanation.failureCode)}
      </p>
      <p className="text-xs leading-5 text-slate-600">
        {f.t.alertDetail.explanationAttempts(
          explanation.attemptCount,
          f.formatCount(explanation.attemptCount),
        )}
        {explanation.settledAt === null
          ? ""
          : f.t.alertDetail.explanationLastAttempt(f.formatInstant(explanation.settledAt))}
        {". "}
        {explanation.attemptsExhausted
          ? f.t.alertDetail.explanationExhausted
          : f.t.alertDetail.explanationNotStored}
      </p>
    </div>
  );
}

/**
 * Which of the three questions the button asks, or none at all.
 *
 * Over an explanation written by the template this deployment writes with, there is nothing to ask:
 * the API refuses to replace a paragraph somebody may have formed a verdict on, and it refuses a
 * spent budget too. The two cases that remain are a row that failed with attempts left, and a row a
 * previous template wrote — which is not a replacement at all. The row the current template would
 * own does not exist, so asking creates it beside the old one, and the old one stays exactly as it
 * was.
 *
 * The order of the checks is the point. A row from another template is offered the current template
 * whatever its status, because its status describes a row the request will not touch.
 */
function askOf(explanation: AlertExplanation | null): ExplanationAsk {
  if (explanation === null) {
    return "first";
  }

  if (explanation.writtenByAnotherTemplate) {
    return "currentTemplate";
  }

  return explanation.status === EXPLANATION_STATUS.failed && !explanation.attemptsExhausted
    ? "retry"
    : "none";
}
