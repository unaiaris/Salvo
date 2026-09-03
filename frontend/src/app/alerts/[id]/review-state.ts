import { ALERT_STATUS } from "@/lib/api/contract";

/**
 * The result of a review attempt, flattened to primitives.
 *
 * It is what `useActionState` hands to a client component, so every field here is serialised into
 * the RSC payload. Keeping it primitive is the same rule that governs client component props, for
 * the same reason: whatever shape crosses the boundary crosses it whole.
 *
 * `submittedStatus`, `submittedNote` and `acknowledged` travel back so the form can put the analyst
 * where she was. React 19 resets the uncontrolled fields of a `<form action>` when the action
 * settles, including when it settled on a conflict; without echoing the input, a `409` would silently
 * erase a note she may have spent minutes writing.
 */
export interface ReviewFormState {
  readonly outcome: "idle" | "applied" | "unchanged" | "failed";
  readonly title: string;
  readonly body: string;
  readonly recovery: string;
  /** The API's own `detail`, as secondary information. Empty when there is none. */
  readonly technicalDetail: string;
  readonly isFormError: boolean;
  readonly submittedStatus: string;
  readonly submittedNote: string;
  readonly acknowledged: boolean;
  /**
   * Increments on every settled attempt. The form uses it to tell a new result from a re-render, so
   * it can re-seed its fields exactly once per submission.
   */
  readonly submissionId: number;
}

export const INITIAL_REVIEW_STATE: ReviewFormState = {
  outcome: "idle",
  title: "",
  body: "",
  recovery: "",
  technicalDetail: "",
  isFormError: false,
  submittedStatus: "",
  submittedNote: "",
  acknowledged: false,
  submissionId: 0,
};

export const REVIEW_CHOICES = [
  {
    value: ALERT_STATUS.confirmedSafe,
    label: "Confirmar segura",
    hint: "El pedido no es fraude.",
  },
  {
    value: ALERT_STATUS.reportedFraud,
    label: "Reportar fraude",
    hint: "El pedido es fraude y queda registrado como tal.",
  },
] as const;
