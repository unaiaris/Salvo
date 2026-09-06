"use server";

import { revalidatePath } from "next/cache";
import { EXPLANATION_STATUS } from "@/lib/api/contract";
import { requestAlertExplanation } from "@/lib/api/explanations";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { explanationFailureLabel } from "@/lib/format";
import type { ExplanationActionState } from "./explanation-state";

/**
 * Asking for the evaluation to be put into words, and asking again when the attempt failed.
 *
 * One action for both, because they are the same request with one flag: the API decides what a
 * repeat means, and it is the only place that can. Nothing here is optimistic — the action posts,
 * revalidates the route and lets the page re-read what was actually stored. A console that painted a
 * paragraph before the verification had run would be showing text the system may have refused.
 */
export async function explainEvaluation(
  previous: ExplanationActionState,
  formData: FormData,
): Promise<ExplanationActionState> {
  const submissionId = previous.submissionId + 1;
  const alertId = readField(formData, "alertId");
  const regenerate = formData.get("regenerate") !== null;

  const result = await requestAlertExplanation(alertId, regenerate);
  if (!result.ok) {
    return failed(result.failure, submissionId);
  }

  revalidatePath(`/alerts/${alertId}`);

  const { applied, explanation } = result.value;

  if (!applied) {
    return {
      outcome: "done",
      title: "Esta evaluación ya tenía su explicación",
      body: "No se le pidió nada al proveedor: la que ya estaba escrita es la que se muestra arriba.",
      recovery: "",
      technicalDetail: "",
      submissionId,
    };
  }

  if (explanation.status === EXPLANATION_STATUS.ready) {
    return {
      outcome: "done",
      title: regenerate ? "Explicación redactada en el nuevo intento" : "Explicación redactada",
      body:
        "El texto quedó guardado junto a la evaluación y ya se muestra arriba. Una explicación " +
        "escrita no se reescribe: si el pedido vuelve a evaluarse, la evaluación nueva lleva la suya.",
      recovery: "",
      technicalDetail: "",
      submissionId,
    };
  }

  // A row that came back without text. The request itself succeeded, which is why this is a `200`
  // and not an error: a provider that invents a figure is an ordinary outcome of asking.
  return {
    outcome: "failed",
    title: "No se pudo redactar la explicación",
    body:
      explanation.failureCode === null
        ? "El proveedor no dejó ningún texto utilizable."
        : `${explanationFailureLabel(explanation.failureCode)}.`,
    recovery: explanation.attemptsExhausted
      ? "Se agotaron los intentos para esta evaluación. El veredicto no necesita una explicación para emitirse."
      : "Podés volver a intentarlo. Un texto que no se pudo verificar no se guarda ni se muestra.",
    technicalDetail: "",
    submissionId,
  };
}

function failed(failure: ApiFailure, submissionId: number): ExplanationActionState {
  const message = describeFailure(failure);

  return {
    outcome: "failed",
    title: message.title,
    body: message.body,
    recovery: message.recovery,
    technicalDetail: failure.kind === "problem" ? (failure.detail ?? "") : "",
    submissionId,
  };
}

function readField(formData: FormData, name: string): string {
  const value = formData.get(name);

  return typeof value === "string" ? value : "";
}
