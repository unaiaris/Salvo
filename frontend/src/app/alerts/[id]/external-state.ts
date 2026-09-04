/**
 * The result of an attempt on the external block, flattened to primitives.
 *
 * `useActionState` hands this to a client component, so it is serialised into the RSC payload
 * exactly like a prop would be: the rule of decision 42 is about what crosses the boundary, not
 * about how it got there. That is why the provider's answer arrives already written as a sentence —
 * the server owns the wording, the client owns the box it goes in.
 */
export interface ExternalActionState {
  readonly outcome: "idle" | "done" | "failed";
  readonly title: string;
  readonly body: string;
  readonly recovery: string;
  /** The API's own `detail`, as secondary information. Empty when there is none. */
  readonly technicalDetail: string;
  /** Increments on every settled attempt, so a repeat is announced again. */
  readonly submissionId: number;
}

export const INITIAL_EXTERNAL_STATE: ExternalActionState = {
  outcome: "idle",
  title: "",
  body: "",
  recovery: "",
  technicalDetail: "",
  submissionId: 0,
};
