"use server";

import { revalidatePath } from "next/cache";
import { deploymentLanguage } from "@/lib/api/console";
import { EXTERNAL_STATUS, type Language } from "@/lib/api/contract";
import { deliverExternalCallbacks, requestExternalEvaluation } from "@/lib/api/external";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { formatting } from "@/lib/format";
import type { ExternalActionState } from "./external-state";

/**
 * Asking the provider, and asking the API to deliver what the provider would send.
 *
 * Neither is optimistic. Both post, revalidate the routes that read the evaluation, and let the page
 * re-read what was actually persisted; what comes back here is only the message about the attempt.
 * A console that painted a verdict before knowing would let an analyst decide an order on an opinion
 * nobody had given yet.
 */

function failed(
  failure: ApiFailure,
  language: Language,
  submissionId: number,
): ExternalActionState {
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

function revalidate(alertId: string): void {
  revalidatePath(`/alerts/${alertId}`);
  revalidatePath("/alerts");
}

export async function requestExternal(
  previous: ExternalActionState,
  formData: FormData,
): Promise<ExternalActionState> {
  const submissionId = previous.submissionId + 1;
  const orderId = readField(formData, "orderId");
  const alertId = readField(formData, "alertId");
  const language = await deploymentLanguage();
  const f = formatting(language);

  const result = await requestExternalEvaluation(orderId);
  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidate(alertId);
  const { applied, evaluation } = result.value;
  const waiting = evaluation.status === EXTERNAL_STATUS.pending;

  return {
    outcome: "done",
    title: applied
      ? f.t.outcomes.externalRequestedTitle
      : f.t.outcomes.externalAlreadyTitle,
    body: `${f.externalStatusLabel(evaluation.status)}. ${
      waiting ? f.t.outcomes.externalWaitingBody : f.t.outcomes.externalSettledBody
    }`,
    recovery: applied ? "" : f.t.outcomes.externalAlreadyRecovery,
    technicalDetail: "",
    submissionId,
  };
}

export async function deliverCallback(
  previous: ExternalActionState,
  formData: FormData,
): Promise<ExternalActionState> {
  const submissionId = previous.submissionId + 1;
  const externalEvaluationId = readField(formData, "externalEvaluationId");
  const alertId = readField(formData, "alertId");
  const language = await deploymentLanguage();
  const { outcomes } = formatting(language).t;

  const result = await deliverExternalCallbacks(externalEvaluationId);
  if (!result.ok) {
    return failed(result.failure, language, submissionId);
  }

  revalidate(alertId);
  const delivery = result.value;

  return {
    outcome: "done",
    title:
      delivery.replayed > 0
        ? outcomes.externalCallbackReplayedTitle
        : outcomes.externalCallbackDoneTitle,
    body:
      delivery.replayed > 0
        ? outcomes.externalCallbackReplayedBody
        : outcomes.externalCallbackDoneBody,
    recovery: delivery.settled > 0 ? outcomes.externalCallbackRecovery : "",
    technicalDetail: "",
    submissionId,
  };
}

function readField(formData: FormData, name: string): string {
  const value = formData.get(name);

  return typeof value === "string" ? value : "";
}
