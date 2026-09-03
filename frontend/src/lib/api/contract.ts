import type { components } from "./schema";

/**
 * The wire type of an integer, narrowed to `number`.
 *
 * ASP.NET Core 10 describes every integer as `type: ["integer", "string"]` so that a large value may
 * travel as a decimal string, which `openapi-typescript` faithfully renders as `number | string`.
 * Carrying that union through the views would push the decision onto every call site; the guards
 * accept both shapes on the wire and hand the rest of the application a plain number.
 */
export type ApiView<T> = [number] extends [T]
  ? Exclude<T, string>
  : T extends object
    ? { readonly [K in keyof T]: ApiView<T[K]> }
    : T;

type Schemas = components["schemas"];

export type AlertSignal = ApiView<Schemas["AlertSignalView"]>;
export type AlertSnapshot = ApiView<Schemas["AlertSnapshotView"]>;
export type AlertEvaluation = ApiView<Schemas["AlertEvaluationView"]>;
export type AlertDivergence = ApiView<Schemas["AlertDivergenceView"]>;
export type AlertOrder = ApiView<Schemas["AlertOrderView"]>;
export type AlertReview = ApiView<Schemas["AlertReviewView"]>;
export type ScoringRun = ApiView<Schemas["ScoringRunReference"]>;
export type AlertListItem = ApiView<Schemas["AlertListItem"]>;
export type AlertList = ApiView<Schemas["ListAlertsResult"]>;
export type AlertDetail = ApiView<Schemas["AlertDetail"]>;
export type AlertReviewOutcome = ApiView<Schemas["AlertReviewResult"]>;
export type OrderList = ApiView<Schemas["ListOrdersResult"]>;

/** Wire values of `AlertStatus`, mirrored from `Salvo.Domain.Alerts.AlertWireNames`. */
export const ALERT_STATUS = {
  open: "OPEN",
  confirmedSafe: "CONFIRMED_SAFE",
  reportedFraud: "REPORTED_FRAUD",
} as const;

/** Wire values of `AlertSeverity`. `HIGH` exists in the policy even when the corpus has none. */
export const ALERT_SEVERITY = {
  medium: "MEDIUM",
  high: "HIGH",
  critical: "CRITICAL",
} as const;

/** Wire values of `AlertSortWireNames`. */
export const ALERT_SORT = {
  createdDesc: "CREATED_DESC",
  scoreDesc: "SCORE_DESC",
} as const;

/**
 * `ListAlertsHandler.MaximumPageSize`. The feed asks for a single page of this size: no ordering of
 * this feed survives a scoring run landing between two pages, so for the size of the MVP corpus the
 * console sidesteps paging entirely rather than reconciling it.
 */
export const ALERT_FEED_PAGE_SIZE = 200;

/** `AlertEndpoints.MaximumNoteLength`. */
export const REVIEW_NOTE_MAX_LENGTH = 2000;
