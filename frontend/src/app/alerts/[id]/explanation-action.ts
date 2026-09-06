"use server";

import { revalidatePath } from "next/cache";
import { EXPLANATION_STATUS } from "@/lib/api/contract";
import { requestAlertExplanation } from "@/lib/api/explanations";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { explanationFailureLabel } from "@/lib/format";
import type { ExplanationActionState, ExplanationAsk } from "./explanation-state";

/**
 * What was stored, said once per question.
 *
 * None of these sentences appears in the legend of the block: the notice is read directly under it,
 * and a sentence read twice is a sentence the reader stops trusting to mean anything. The legend
 * says who wrote the text and what was checked before it was kept; this says what has just changed.
 */
const WRITTEN: Readonly<Record<Exclude<ExplanationAsk, "none">, { title: string; body: string }>> = {
  first: {
    title: "Explicación redactada",
    body:
      "El texto quedó guardado junto a la evaluación y ya se muestra arriba. Una explicación " +
      "escrita no se reescribe: si el pedido vuelve a evaluarse, la evaluación nueva lleva la suya.",
  },
  retry: {
    title: "Explicación redactada en el nuevo intento",
    body:
      "El texto quedó guardado junto a la evaluación y ya se muestra arriba. Una explicación " +
      "escrita no se reescribe: si el pedido vuelve a evaluarse, la evaluación nueva lleva la suya.",
  },
  currentTemplate: {
    title: "Redactada de nuevo con la plantilla vigente",
    body:
      "El texto nuevo se escribió al lado del anterior y es el que se muestra arriba. El anterior " +
      "sigue guardado sin cambios, porque es el registro de lo que se pudo leer mientras se " +
      "formaba el veredicto.",
  },
};

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
      ...WRITTEN[ask],
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
