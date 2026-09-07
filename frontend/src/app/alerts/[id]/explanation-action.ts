"use server";

import { revalidatePath } from "next/cache";
import { deploymentLanguage } from "@/lib/api/console";
import { EXPLANATION_STATUS, type Language } from "@/lib/api/contract";
import { requestAlertExplanation } from "@/lib/api/explanations";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { formatting } from "@/lib/format";
import type { ExplanationActionState, ExplanationAsk } from "./explanation-state";

/**
 * What was stored, said once per question.
 *
 * None of these sentences appears in the legend of the block: the notice is read directly under it,
 * and a sentence read twice is a sentence the reader stops trusting to mean anything. The legend
 * says who wrote the text and what was checked before it was kept; this says what has just changed.
 */
function written(
  ask: Exclude<ExplanationAsk, "none">,
  language: Language,
): { title: string; body: string } {
  const { outcomes } = formatting(language).t;

  switch (ask) {
    case "first":
      return {
        title: outcomes.explanationWrittenFirstTitle,
        body: outcomes.explanationWrittenBody,
      };
    case "retry":
      return {
        title: outcomes.explanationWrittenRetryTitle,
        body: outcomes.explanationWrittenBody,
      };
    case "currentTemplate":
      return {
        title: outcomes.explanationWrittenCurrentTemplateTitle,
        body: outcomes.explanationWrittenCurrentTemplateBody,
      };
  }
}

/**
 * Asking for the evaluation to be put into words: for the first time, again after a failure, or
 * with the template this deployment writes with today.
 *
 * One action for the three, because they are the same request: the API decides what a repeat means,
 * and it is the only place that can. Only a retry is a regeneration — asking for the current
 * template replaces nothing, it writes the row that template does not have yet — and this is where
 * that translation happens, so the situation the page described cannot arrive as a different
 * request. Nothing here is optimistic — the action posts,
 * revalidates the route and lets the page re-read what was actually stored. A console that painted a
 * paragraph before the verification had run would be showing text the system may have refused.
 */
export async function explainEvaluation(
  previous: ExplanationActionState,
  formData: FormData,
): Promise<ExplanationActionState> {
  const submissionId = previous.submissionId + 1;
  const alertId = readField(formData, "alertId");
  const ask = readAsk(formData);
  const regenerate = ask === "retry";
  const language = await deploymentLanguage();
  const f = formatting(language);

  const result = await requestAlertExplanation(alertId, regenerate);
  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidatePath(`/alerts/${alertId}`);

  const { applied, explanation } = result.value;

  if (!applied) {
    return {
      outcome: "done",
      title: f.t.outcomes.explanationUnchangedTitle,
      body: f.t.outcomes.explanationUnchangedBody,
      recovery: "",
      technicalDetail: "",
      submissionId,
    };
  }

  if (explanation.status === EXPLANATION_STATUS.ready) {
    return {
      outcome: "done",
      ...written(ask, language),
      recovery: "",
      technicalDetail: "",
      submissionId,
    };
  }

  // A row that came back without text. The request itself succeeded, which is why this is a `200`
  // and not an error: a provider that invents a figure is an ordinary outcome of asking.
  return {
    outcome: "failed",
    title: f.t.outcomes.explanationFailedTitle,
    body:
      explanation.failureCode === null
        ? f.t.outcomes.explanationFailedWithoutCode
        : f.t.outcomes.explanationFailedWithCode(
            f.explanationFailureLabel(explanation.failureCode),
          ),
    recovery: explanation.attemptsExhausted
      ? f.t.outcomes.explanationFailedExhausted
      : f.t.outcomes.explanationFailedRetry,
    technicalDetail: "",
    submissionId,
  };
}

function failed(
  failure: ApiFailure,
  language: Language,
  submissionId: number,
): ExplanationActionState {
  const message = describeFailure(failure, language);

  return {
    outcome: "failed",
    title: message.title,
    body: message.body,
    recovery: message.recovery,
    technicalDetail: failure.kind === "problem" ? (failure.detail ?? "") : "",
    submissionId,
  };
}

/**
 * The question the page was showing, narrowed to one this action knows.
 *
 * A form field is a string that arrived over the network, so anything else is read as the plainest
 * of the three. Falling back to a retry would let a malformed submission ask the API to replace a
 * written paragraph.
 */
function readAsk(formData: FormData): Exclude<ExplanationAsk, "none"> {
  const value = readField(formData, "ask");

  return value === "retry" || value === "currentTemplate" ? value : "first";
}

function readField(formData: FormData, name: string): string {
  const value = formData.get(name);

  return typeof value === "string" ? value : "";
}
