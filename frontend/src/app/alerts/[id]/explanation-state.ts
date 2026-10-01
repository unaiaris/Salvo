/**
 * Which question the button asks, or that there is none to ask.
 *
 * A string rather than a pair of flags, because the questions are not combinations of one another:
 * `first` has no row at all, `retry` reuses the row that failed and is on screen,
 * `currentTemplate` writes the row of the current writer beside a text somebody else wrote, and
 * `retryCurrentWriter` reuses the failed row of the current writer that is shown <em>beside</em>
 * somebody else's text (decision 79). Only the two retries are a regeneration — the API is asked to
 * retake a row — and the action is what turns this into that flag, so no caller can send a
 * regeneration by describing the situation wrongly.
 *
 * `currentTemplate` keeps its name from when the only writer was a template: what it writes is the
 * row of whoever writes today.
 */
export type ExplanationAsk = "none" | "first" | "retry" | "currentTemplate" | "retryCurrentWriter";

/**
 * Who writes today, as the button needs to say it: the template, or a model of Anthropic. The name
 * of the model is never on the button, because the button can appear before any answer named it.
 */
export type ExplanationWriterKind = "template" | "model";

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
