/**
 * The result of an attempt on this page, flattened to primitives.
 *
 * Three different actions live on `/import` and all three report through this one shape, because
 * what they have to say has the same structure: it worked and here is what changed, or it did not
 * and here is why and what to do next.
 *
 * Every field is primitive on purpose. `useActionState` hands this object to a client component, so
 * it is serialised into the RSC payload exactly like a prop would be — the rule of decision 42 is
 * about what crosses the boundary, not about how it got there. That is also why the per-row errors
 * of an import arrive already written out as sentences: the server owns the wording, the client owns
 * the list.
 */
export interface ActionState {
  readonly outcome: "idle" | "done" | "failed";
  readonly title: string;
  readonly body: string;
  readonly recovery: string;
  /** The API's own `detail`, as secondary information. Empty when there is none. */
  readonly technicalDetail: string;
  /** The figures of a successful attempt, each already formatted. */
  readonly facts: readonly string[];
  /** One sentence per rejected record of an import. Empty for every other action. */
  readonly recordErrors: readonly string[];
  /**
   * Whether the API stopped listing rejected records. It caps the list at a thousand, and an
   * analyst who is not told will read the shortest list as the whole truth.
   */
  readonly errorsTruncated: boolean;
  /** Increments on every settled attempt, so the outcome can be re-announced on a repeat. */
  readonly submissionId: number;
}

export const INITIAL_ACTION_STATE: ActionState = {
  outcome: "idle",
  title: "",
  body: "",
  recovery: "",
  technicalDetail: "",
  facts: [],
  recordErrors: [],
  errorsTruncated: false,
  submissionId: 0,
};
