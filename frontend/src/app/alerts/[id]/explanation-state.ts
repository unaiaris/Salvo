/**
 * The result of asking for an explanation, flattened to primitives.
 *
 * `useActionState` hands this to a client component, so every field is serialised into the RSC
 * payload exactly as a prop would be — the rule of decision 42 is about what crosses the boundary,
 * not about how it got there.
 *
 * The summary is deliberately not one of these fields. It is already on the page, read back from the
 * API after the write, and echoing it through the action would put the provider's prose into a
 * second place that could disagree with the first.
 */
export interface ExplanationActionState {
  /**
   * `failed` covers two different things on purpose: a request the API refused, and a redaction that
   * came back without usable text. They are different facts about the system and the wording says
   * which is which, but for the analyst reading the block both are "there is no explanation yet".
   */
  readonly outcome: "idle" | "done" | "failed";
  readonly title: string;
  readonly body: string;
  readonly recovery: string;
  /** The API's own `detail`, as secondary information. Empty when there is none. */
  readonly technicalDetail: string;
  /** Increments on every settled attempt, so a repeat is announced again. */
  readonly submissionId: number;
}

export const INITIAL_EXPLANATION_STATE: ExplanationActionState = {
  outcome: "idle",
  title: "",
  body: "",
  recovery: "",
  technicalDetail: "",
  submissionId: 0,
};
