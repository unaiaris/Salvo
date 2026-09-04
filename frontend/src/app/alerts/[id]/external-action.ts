"use server";

import { revalidatePath } from "next/cache";
import { EXTERNAL_STATUS } from "@/lib/api/contract";
import { deliverExternalCallbacks, requestExternalEvaluation } from "@/lib/api/external";
import type { ApiFailure } from "@/lib/api/failures";
import { describeFailure } from "@/lib/api/messages";
import { externalStatusLabel } from "@/lib/format";
import type { ExternalActionState } from "./external-state";

/**
 * Asking the provider, and asking the API to deliver what the provider would send.
 *
 * Neither is optimistic. Both post, revalidate the routes that read the evaluation, and let the page
 * re-read what was actually persisted; what comes back here is only the message about the attempt.
 * A console that painted a verdict before knowing would let an analyst decide an order on an opinion
 * nobody had given yet.
 */

function failed(failure: ApiFailure, submissionId: number): ExternalActionState {
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

  const result = await requestExternalEvaluation(orderId);
  if (!result.ok) {
    return failed(result.failure, submissionId);
  }

  revalidate(alertId);
  const { applied, evaluation } = result.value;
  const waiting = evaluation.status === EXTERNAL_STATUS.pending;

  return {
    outcome: "done",
    title: applied
      ? "Evaluación externa solicitada"
      : "Este pedido ya tenía una evaluación externa",
    body: `${externalStatusLabel(evaluation.status)}. ${
      waiting
        ? "El proveedor aceptó la consulta y todavía no decidió: la respuesta va a llegar por callback, o al reconciliar."
        : "El veredicto del proveedor queda registrado junto al criterio local, sin combinarse con él."
    }`,
    recovery: applied
      ? ""
      : "Pedirla de nuevo no crea una segunda evaluación del lado del proveedor.",
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

  const result = await deliverExternalCallbacks(externalEvaluationId);
  if (!result.ok) {
    return failed(result.failure, submissionId);
  }

  revalidate(alertId);
  const delivery = result.value;

  return {
    outcome: "done",
    title: delivery.replayed > 0 ? "Ese callback ya se había recibido" : "Callback entregado",
    body:
      delivery.replayed > 0
        ? "El mensaje es idéntico a uno ya registrado, así que no se repitió ningún efecto: se anotó que el proveedor lo volvió a enviar y nada más."
        : "El callback entró por el mismo camino que usaría el proveedor: mismo recibo, misma deduplicación, mismas reglas de transición.",
    recovery: delivery.settled > 0 ? "El estado del proveedor ya figura arriba." : "",
    technicalDetail: "",
    submissionId,
  };
}

function readField(formData: FormData, name: string): string {
  const value = formData.get(name);

  return typeof value === "string" ? value : "";
}
