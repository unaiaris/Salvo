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
export type AlertExternalEvaluation = ApiView<Schemas["AlertExternalEvaluationView"]>;
export type AlertExplanation = ApiView<Schemas["AlertExplanationView"]>;
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

/**
 * The languages this console can compose itself in, mirrored from `ExplanationWireNames`.
 *
 * A closed catalogue rather than the `string` the schema declares, and that is the whole point: a
 * deployment whose API announced a language the console has no dictionary for would otherwise fall
 * back to Spanish in silence and show half a screen nobody asked for. Here the guard refuses the
 * response instead, and the failure is visible.
 */
export const CONSOLE_LANGUAGES = ["es", "pt"] as const;

export type Language = (typeof CONSOLE_LANGUAGES)[number];

/** Spanish, which is the default of the API and the language of the demonstration. */
export const DEFAULT_LANGUAGE: Language = "es";

export function isLanguage(value: string): value is Language {
  return (CONSOLE_LANGUAGES as readonly string[]).includes(value);
}

/**
 * The wire shape with its language narrowed to what the console can actually render.
 *
 * Everything else still comes from the generated schema, so a field renamed or added in the API
 * breaks this type and the guard that has to satisfy it — which is the protection the derivation
 * exists for.
 */
export type Capabilities = Omit<ApiView<Schemas["CapabilitiesResponse"]>, "language"> & {
  readonly language: Language;
};

export type CallbackDelivery = ApiView<Schemas["CallbackDeliverySummary"]>;
export type CorpusExternalEvaluations = ApiView<Schemas["CorpusExternalEvaluationSummary"]>;
export type ExternalEvaluationRequest = ApiView<Schemas["RequestExternalEvaluationResult"]>;

/**
 * Wire values of `ExternalEvaluationStatus`, mirrored from
 * `Salvo.Domain.External.ExternalEvaluationWireNames`.
 *
 * `PENDING` here means waiting for the provider, which is not what `OPEN` means on an alert and not
 * what an unscored order means either. The console names all three differently and never says
 * "pendiente" on its own.
 */
export const EXTERNAL_STATUS = {
  pending: "PENDING",
  approved: "APPROVED",
  denied: "DENIED",
  error: "ERROR",
} as const;

/** Wire values of `ExternalSettlementSource`. */
export const EXTERNAL_SOURCE = {
  sync: "SYNC",
  callback: "CALLBACK",
  reconciliation: "RECONCILIATION",
} as const;

export type ExplanationOutcome = ApiView<Schemas["RequestExplanationResult"]>;

/**
 * Wire values of `ExplanationStatus`, mirrored from
 * `Salvo.Domain.Explanations.ExplanationWireNames`.
 *
 * `PENDING` here is a fourth thing waiting, beside the three the external status already names: an
 * explanation whose provider has been asked and has not answered. The console never renders any of
 * them as "pendiente" on its own.
 *
 * `READY` in particular is load-bearing rather than decorative: it is the status that admits a
 * `summary`, and `projectExplanation` refuses text that arrives with any other. It lives here, with
 * the rest of the wire values, so that the guard and the block that renders the states read the same
 * constant.
 */
export const EXPLANATION_STATUS = {
  pending: "PENDING",
  ready: "READY",
  failed: "FAILED",
} as const;

export type DashboardScoringRun = ApiView<Schemas["DashboardScoringRunView"]>;
export type DashboardSeverityCount = ApiView<Schemas["DashboardSeverityCountView"]>;
export type DashboardOpenAlerts = ApiView<Schemas["DashboardOpenAlertsView"]>;
export type DashboardAmountAtRisk = ApiView<Schemas["DashboardAmountAtRiskView"]>;
export type DashboardReportedFraud = ApiView<Schemas["DashboardReportedFraudView"]>;
export type DashboardRiskBucket = ApiView<Schemas["DashboardRiskBucketView"]>;
export type DashboardSignal = ApiView<Schemas["DashboardSignalView"]>;
export type DashboardExternalDenial = ApiView<Schemas["DashboardExternalDenialView"]>;
export type DashboardExternalDenials = ApiView<Schemas["DashboardExternalDenialsView"]>;
export type Dashboard = ApiView<Schemas["DashboardResult"]>;

export type ConfusionMatrix = ApiView<Schemas["ConfusionMatrixView"]>;
export type MetricsFigures = ApiView<Schemas["EvaluationMetricsView"]>;
export type ThresholdMetrics = ApiView<Schemas["ThresholdMetricsView"]>;
export type EvaluationMetrics = ApiView<Schemas["EvaluationMetricsResult"]>;

export type ImportRecordError = ApiView<Schemas["ImportRecordError"]>;
export type ImportResult = ApiView<Schemas["ImportOrdersResult"]>;
export type ScoringRunSummary = ApiView<Schemas["ScoringRunSummary"]>;
export type SeedResult = ApiView<Schemas["SeedDemoOrdersResult"]>;
export type SeedPreview = ApiView<Schemas["DemoSeedPreviewResult"]>;

/**
 * Why loading the demo corpus would be refused.
 *
 * The console reads it before the button is pressed, so that a database holding the previous
 * version of the corpus is announced rather than discovered.
 */
export const SEED_CONFLICT = {
  previousCorpus: "PREVIOUS_CORPUS",
  importedOrders: "IMPORTED_ORDERS",
} as const;

/**
 * The severity bands the dashboard always shows, in the order it shows them.
 *
 * The API answers with the bands that actually have open alerts, so a band that dropped to zero
 * simply disappears from `bySeverity`. Reading that as "there is no such band" is exactly the wrong
 * conclusion: the demo corpus has no `HIGH` alert at all, and an analyst who never sees the row
 * cannot tell an empty band from a band that does not exist.
 */
export const DASHBOARD_SEVERITY_ORDER = [
  ALERT_SEVERITY.critical,
  ALERT_SEVERITY.high,
  ALERT_SEVERITY.medium,
] as const;

/** `OrderEndpoints.MaximumFileSizeBytes`, mirrored so the form can say the limit before sending. */
export const IMPORT_MAX_FILE_BYTES = 5 * 1024 * 1024;

/** Wire values of the `format` field of `POST /api/order-imports`. */
export const IMPORT_FORMATS = ["CSV", "JSON"] as const;

export type ImportFormat = (typeof IMPORT_FORMATS)[number];
