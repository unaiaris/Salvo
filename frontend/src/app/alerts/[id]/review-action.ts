"use server";

import { revalidatePath } from "next/cache";
import { submitAlertReview } from "@/lib/api/alerts";
import { describeFailure } from "@/lib/api/messages";
import type { ReviewFormState } from "./review-state";

/**
 * Reviewing an alert, with no optimistic step anywhere.
 *
 * A review can be refused for four different reasons, and a console that painted the verdict as
 * applied before knowing would let an analyst move on believing she decided an order that in fact
 * stayed open. So the action posts, revalidates the routes and lets the page re-read what was
 * actually persisted; what it returns is only the message about the attempt.
 */
export async function reviewAlert(
  previous: ReviewFormState,
  formData: FormData,
): Promise<ReviewFormState> {
  const submissionId = previous.submissionId + 1;
  const alertId = readField(formData, "alertId");
  const newStatus = readField(formData, "newStatus");
  const note = readField(formData, "note");
  const acknowledged = formData.get("acknowledgedDivergence") !== null;
  const explanationId = readField(formData, "explanationId");

  const echo = {
    submittedStatus: newStatus,
    submittedNote: note,
    acknowledged,
    submissionId,
  };

  const result = await submitAlertReview(alertId, {
    newStatus,
    note: note.length === 0 ? null : note,
    acknowledgedDivergence: acknowledged,
    explanationId: explanationId.length === 0 ? null : explanationId,
  });

  if (!result.ok) {
    const message = describeFailure(result.failure);

    return {
      ...echo,
      outcome: "failed",
      title: message.title,
      body: message.body,
      recovery: message.recovery,
      technicalDetail:
        result.failure.kind === "problem" ? (result.failure.detail ?? "") : "",
      isFormError: message.isFormError,
    };
  }

  revalidatePath("/alerts");
  revalidatePath(`/alerts/${alertId}`);

  if (!result.value.applied) {
    return {
      ...echo,
      outcome: "unchanged",
      title: "La alerta ya tenía exactamente este veredicto",
      body: "No se registró una revisión nueva porque el veredicto y la nota guardados coinciden con los que enviaste.",
      recovery: "El veredicto vigente es el que se muestra abajo.",
      technicalDetail: "",
      isFormError: false,
    };
  }

  return {
    ...echo,
    outcome: "applied",
    title: "Veredicto registrado",
    body: "La revisión quedó guardada junto con su auditoría. Un veredicto es definitivo: esta alerta no se reabre.",
    recovery: "",
    technicalDetail: "",
    isFormError: false,
  };
}

function readField(formData: FormData, name: string): string {
  const value = formData.get(name);

  return typeof value === "string" ? value : "";
}
