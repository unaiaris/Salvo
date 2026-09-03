import type {
  AlertDetail,
  Capabilities,
  AlertDivergence,
  AlertEvaluation,
  AlertList,
  AlertListItem,
  AlertOrder,
  AlertReview,
  AlertReviewOutcome,
  AlertSignal,
  AlertSnapshot,
  ConfusionMatrix,
  Dashboard,
  DashboardAmountAtRisk,
  DashboardOpenAlerts,
  DashboardReportedFraud,
  DashboardRiskBucket,
  DashboardScoringRun,
  DashboardSeverityCount,
  DashboardSignal,
  EvaluationMetrics,
  ImportRecordError,
  ImportResult,
  MetricsFigures,
  OrderList,
  ScoringRun,
  ScoringRunSummary,
  SeedResult,
  ThresholdMetrics,
} from "./contract";

/**
 * Runtime guards that **project**: each one builds a new object holding exactly the keys the console
 * knows about, requires the mandatory ones and drops everything else.
 *
 * A predicate — `value is T`, narrowing the very object it was handed — would let an unknown field
 * ride along into the render tree and, from there, into the RSC payload. It would also make a field
 * that stage 7 adds to `AlertDetail` reach the browser before anyone decided to show it. Projection
 * answers both cases: unknown keys never leave this module, and a required key that the API stops
 * sending is a rejection here instead of an `undefined` deep inside a page.
 *
 * A guard returns `null` when the payload does not match; the caller turns that into a `malformed`
 * failure. Nothing here throws.
 */

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === "object" && value !== null && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function text(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}

function nullableText(value: unknown): string | null | undefined {
  if (value === null) {
    return null;
  }

  return typeof value === "string" ? value : undefined;
}

/**
 * An integer arrives either as a JSON number or as a decimal string: ASP.NET Core 10 declares both
 * in the schema. `Number.parseInt` would accept `"12abc"`, so the string form is matched against the
 * same pattern the document publishes.
 */
const INTEGER_PATTERN = /^-?(?:0|[1-9]\d*)$/;

function integer(value: unknown): number | null {
  if (typeof value === "number") {
    return Number.isSafeInteger(value) ? value : null;
  }

  if (typeof value === "string" && INTEGER_PATTERN.test(value)) {
    const parsed = Number(value);
    return Number.isSafeInteger(parsed) ? parsed : null;
  }

  return null;
}

function nullableInteger(value: unknown): number | null | undefined {
  if (value === null) {
    return null;
  }

  return integer(value) ?? undefined;
}

/**
 * A ratio, which the API declares as `decimal` and therefore publishes as `number | string`.
 *
 * Kept as a JavaScript number because everything the console does with one is presentation: a rate
 * is formatted as a percentage and never added to anything. Money never travels this way — it is
 * `amountCents`, an integer — so nothing here decides a figure that has to be exact.
 */
const DECIMAL_PATTERN = /^-?(?:0|[1-9]\d*)(?:\.\d+)?$/;

function decimal(value: unknown): number | null {
  if (typeof value === "number") {
    return Number.isFinite(value) ? value : null;
  }

  if (typeof value === "string" && DECIMAL_PATTERN.test(value)) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  return null;
}

function nullableDecimal(value: unknown): number | null | undefined {
  if (value === null) {
    return null;
  }

  return decimal(value) ?? undefined;
}

/** A `DateOnly`, kept as the `YYYY-MM-DD` string the API sends. */
function calendarDate(value: unknown): string | null {
  return typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : null;
}

function flag(value: unknown): boolean | null {
  return typeof value === "boolean" ? value : null;
}

/** An instant is kept as the ISO string the API sent; only a parseable one is accepted. */
function instant(value: unknown): string | null {
  if (typeof value !== "string" || Number.isNaN(Date.parse(value))) {
    return null;
  }

  return value;
}

function nullableInstant(value: unknown): string | null | undefined {
  if (value === null) {
    return null;
  }

  return instant(value) ?? undefined;
}

/**
 * Projects a member the API declares as nullable. `null` is a value the contract allows, so a failed
 * projection has to report itself as `undefined`; collapsing both onto `null` would make the guard
 * reject the legitimate absence of a current evaluation, a current run or a review.
 */
function projectNullable<T>(
  value: unknown,
  project: (item: unknown) => T | null,
): T | null | undefined {
  if (value === null) {
    return null;
  }

  return project(value) ?? undefined;
}

function projectList<T>(value: unknown, project: (item: unknown) => T | null): readonly T[] | null {
  if (!Array.isArray(value)) {
    return null;
  }

  const projected: T[] = [];
  for (const item of value) {
    const one = project(item);
    if (one === null) {
      return null;
    }

    projected.push(one);
  }

  return projected;
}

export function projectSignal(value: unknown): AlertSignal | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const rule = text(raw.rule);
  const weight = integer(raw.weight);
  const detail = text(raw.detail);

  if (rule === null || weight === null || detail === null) {
    return null;
  }

  return { rule, weight, detail };
}

export function projectScoringRun(value: unknown): ScoringRun | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const sequence = integer(raw.sequence);
  const completedAt = instant(raw.completedAt);

  if (sequence === null || completedAt === null) {
    return null;
  }

  return { sequence, completedAt };
}

export function projectSnapshot(value: unknown): AlertSnapshot | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const evaluationId = text(raw.evaluationId);
  const score = integer(raw.score);
  const severity = text(raw.severity);
  const signals = projectList(raw.signals, projectSignal);

  if (evaluationId === null || score === null || severity === null || signals === null) {
    return null;
  }

  return { evaluationId, score, severity, signals };
}

export function projectEvaluation(value: unknown): AlertEvaluation | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const evaluationId = text(raw.evaluationId);
  const score = integer(raw.score);
  const severity = nullableText(raw.severity);
  const isFlagged = flag(raw.isFlagged);
  const signals = projectList(raw.signals, projectSignal);
  const evaluatedAt = instant(raw.evaluatedAt);

  if (
    evaluationId === null ||
    score === null ||
    severity === undefined ||
    isFlagged === null ||
    signals === null ||
    evaluatedAt === null
  ) {
    return null;
  }

  return { evaluationId, score, severity, isFlagged, signals, evaluatedAt };
}

export function projectDivergence(value: unknown): AlertDivergence | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const hasBandDivergence = flag(raw.hasBandDivergence);
  const snapshotScore = integer(raw.snapshotScore);
  const snapshotSeverity = text(raw.snapshotSeverity);
  const currentScore = nullableInteger(raw.currentScore);
  const currentSeverity = nullableText(raw.currentSeverity);

  if (
    hasBandDivergence === null ||
    snapshotScore === null ||
    snapshotSeverity === null ||
    currentScore === undefined ||
    currentSeverity === undefined
  ) {
    return null;
  }

  return { hasBandDivergence, snapshotScore, snapshotSeverity, currentScore, currentSeverity };
}

export function projectOrder(value: unknown): AlertOrder | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const id = text(raw.id);
  const merchantId = text(raw.merchantId);
  const merchantReferenceId = text(raw.merchantReferenceId);
  const buyerReferenceId = text(raw.buyerReferenceId);
  const occurredAt = instant(raw.occurredAt);
  const amountCents = integer(raw.amountCents);
  const currencyCode = text(raw.currencyCode);
  const countryCode = text(raw.countryCode);
  const city = nullableText(raw.city);
  const deviceSessionId = nullableText(raw.deviceSessionId);

  if (
    id === null ||
    merchantId === null ||
    merchantReferenceId === null ||
    buyerReferenceId === null ||
    occurredAt === null ||
    amountCents === null ||
    currencyCode === null ||
    countryCode === null ||
    city === undefined ||
    deviceSessionId === undefined
  ) {
    return null;
  }

  return {
    id,
    merchantId,
    merchantReferenceId,
    buyerReferenceId,
    occurredAt,
    amountCents,
    currencyCode,
    countryCode,
    city,
    deviceSessionId,
  };
}

export function projectReview(value: unknown): AlertReview | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const id = text(raw.id);
  const previousStatus = text(raw.previousStatus);
  const newStatus = text(raw.newStatus);
  const note = nullableText(raw.note);
  const reviewedAt = instant(raw.reviewedAt);

  if (
    id === null ||
    previousStatus === null ||
    newStatus === null ||
    note === undefined ||
    reviewedAt === null
  ) {
    return null;
  }

  return { id, previousStatus, newStatus, note, reviewedAt };
}

export function projectAlertListItem(value: unknown): AlertListItem | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const id = text(raw.id);
  const orderId = text(raw.orderId);
  const merchantReferenceId = text(raw.merchantReferenceId);
  const buyerReferenceId = text(raw.buyerReferenceId);
  const occurredAt = instant(raw.occurredAt);
  const amountCents = integer(raw.amountCents);
  const currencyCode = text(raw.currencyCode);
  const countryCode = text(raw.countryCode);
  const status = text(raw.status);
  const severity = text(raw.severity);
  const riskScoreSnapshot = integer(raw.riskScoreSnapshot);
  const currentRiskScore = nullableInteger(raw.currentRiskScore);
  const currentSeverity = nullableText(raw.currentSeverity);
  const hasBandDivergence = flag(raw.hasBandDivergence);
  const alertPolicyVersion = text(raw.alertPolicyVersion);
  const supersedesAlertId = nullableText(raw.supersedesAlertId);
  const createdAt = instant(raw.createdAt);
  const reviewedAt = nullableInstant(raw.reviewedAt);

  if (
    id === null ||
    orderId === null ||
    merchantReferenceId === null ||
    buyerReferenceId === null ||
    occurredAt === null ||
    amountCents === null ||
    currencyCode === null ||
    countryCode === null ||
    status === null ||
    severity === null ||
    riskScoreSnapshot === null ||
    currentRiskScore === undefined ||
    currentSeverity === undefined ||
    hasBandDivergence === null ||
    alertPolicyVersion === null ||
    supersedesAlertId === undefined ||
    createdAt === null ||
    reviewedAt === undefined
  ) {
    return null;
  }

  return {
    id,
    orderId,
    merchantReferenceId,
    buyerReferenceId,
    occurredAt,
    amountCents,
    currencyCode,
    countryCode,
    status,
    severity,
    riskScoreSnapshot,
    currentRiskScore,
    currentSeverity,
    hasBandDivergence,
    alertPolicyVersion,
    supersedesAlertId,
    createdAt,
    reviewedAt,
  };
}

export function projectAlertList(value: unknown): AlertList | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const items = projectList(raw.items, projectAlertListItem);
  const page = integer(raw.page);
  const pageSize = integer(raw.pageSize);
  const totalCount = integer(raw.totalCount);
  const scoringRunSequence = nullableInteger(raw.scoringRunSequence);
  const currentRun = projectNullable(raw.currentRun, projectScoringRun);

  if (
    items === null ||
    page === null ||
    pageSize === null ||
    totalCount === null ||
    scoringRunSequence === undefined ||
    currentRun === undefined
  ) {
    return null;
  }

  return { items, page, pageSize, totalCount, scoringRunSequence, currentRun };
}

export function projectAlertDetail(value: unknown): AlertDetail | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const id = text(raw.id);
  const orderId = text(raw.orderId);
  const status = text(raw.status);
  const severity = text(raw.severity);
  const alertPolicyVersion = text(raw.alertPolicyVersion);
  const supersedesAlertId = nullableText(raw.supersedesAlertId);
  const createdAt = instant(raw.createdAt);
  const reviewedAt = nullableInstant(raw.reviewedAt);
  const order = projectOrder(raw.order);
  const snapshot = projectSnapshot(raw.snapshot);
  const currentEvaluation = projectNullable(raw.currentEvaluation, projectEvaluation);
  const currentRun = projectNullable(raw.currentRun, projectScoringRun);
  const divergence = projectDivergence(raw.divergence);
  const review = projectNullable(raw.review, projectReview);

  if (
    id === null ||
    orderId === null ||
    status === null ||
    severity === null ||
    alertPolicyVersion === null ||
    supersedesAlertId === undefined ||
    createdAt === null ||
    reviewedAt === undefined ||
    order === null ||
    snapshot === null ||
    currentEvaluation === undefined ||
    currentRun === undefined ||
    divergence === null ||
    review === undefined
  ) {
    return null;
  }

  return {
    id,
    orderId,
    status,
    severity,
    alertPolicyVersion,
    supersedesAlertId,
    createdAt,
    reviewedAt,
    order,
    snapshot,
    currentEvaluation,
    currentRun,
    divergence,
    review,
  };
}

export function projectReviewOutcome(value: unknown): AlertReviewOutcome | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const applied = flag(raw.applied);
  const alert = projectAlertDetail(raw.alert);

  if (applied === null || alert === null) {
    return null;
  }

  return { applied, alert };
}

/**
 * Only `totalCount` is projected: the feed reads this endpoint to tell "no orders at all" from
 * "orders that were never scored", and has no use for the page of orders itself.
 */
export function projectOrderCount(value: unknown): Pick<OrderList, "totalCount"> | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const totalCount = integer(raw.totalCount);

  return totalCount === null ? null : { totalCount };
}

export function projectCapabilities(value: unknown): Capabilities | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const demoDataEnabled = flag(raw.demoDataEnabled);

  return demoDataEnabled === null ? null : { demoDataEnabled };
}

function projectDashboardRun(value: unknown): DashboardScoringRun | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const sequence = integer(raw.sequence);
  const completedAt = instant(raw.completedAt);
  const orderCount = integer(raw.orderCount);

  if (sequence === null || completedAt === null || orderCount === null) {
    return null;
  }

  return { sequence, completedAt, orderCount };
}

function projectSeverityCount(value: unknown): DashboardSeverityCount | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const severity = text(raw.severity);
  const alertCount = integer(raw.alertCount);

  if (severity === null || alertCount === null) {
    return null;
  }

  return { severity, alertCount };
}

function projectOpenAlerts(value: unknown): DashboardOpenAlerts | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const total = integer(raw.total);
  const bySeverity = projectList(raw.bySeverity, projectSeverityCount);

  if (total === null || bySeverity === null) {
    return null;
  }

  return { total, bySeverity };
}

function projectAmountAtRisk(value: unknown): DashboardAmountAtRisk | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const currencyCode = text(raw.currencyCode);
  const amountCents = integer(raw.amountCents);
  const alertCount = integer(raw.alertCount);

  if (currencyCode === null || amountCents === null || alertCount === null) {
    return null;
  }

  return { currencyCode, amountCents, alertCount };
}

function projectReportedFraud(value: unknown): DashboardReportedFraud | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const currencyCode = text(raw.currencyCode);
  const amountCents = integer(raw.amountCents);
  const orderCount = integer(raw.orderCount);

  if (currencyCode === null || amountCents === null || orderCount === null) {
    return null;
  }

  return { currencyCode, amountCents, orderCount };
}

function projectRiskBucket(value: unknown): DashboardRiskBucket | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const weekStart = calendarDate(raw.weekStart);
  const orderCount = integer(raw.orderCount);
  const flaggedCount = integer(raw.flaggedCount);

  if (weekStart === null || orderCount === null || flaggedCount === null) {
    return null;
  }

  return { weekStart, orderCount, flaggedCount };
}

function projectDashboardSignal(value: unknown): DashboardSignal | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const rule = text(raw.rule);
  const alertCount = integer(raw.alertCount);

  if (rule === null || alertCount === null) {
    return null;
  }

  return { rule, alertCount };
}

export function projectDashboard(value: unknown): Dashboard | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const scoringRun = projectNullable(raw.scoringRun, projectDashboardRun);
  const ordersPendingScoring = integer(raw.ordersPendingScoring);
  const openAlerts = projectOpenAlerts(raw.openAlerts);
  const amountAtRisk = projectList(raw.amountAtRisk, projectAmountAtRisk);
  const reportedFraud = projectList(raw.reportedFraud, projectReportedFraud);
  const flagRate = nullableDecimal(raw.flagRate);
  const riskOverTime = projectList(raw.riskOverTime, projectRiskBucket);
  const topSignals = projectList(raw.topSignals, projectDashboardSignal);

  if (
    scoringRun === undefined ||
    ordersPendingScoring === null ||
    openAlerts === null ||
    amountAtRisk === null ||
    reportedFraud === null ||
    flagRate === undefined ||
    riskOverTime === null ||
    topSignals === null
  ) {
    return null;
  }

  return {
    scoringRun,
    ordersPendingScoring,
    openAlerts,
    amountAtRisk,
    reportedFraud,
    flagRate,
    riskOverTime,
    topSignals,
  };
}

function projectMatrix(value: unknown): ConfusionMatrix | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const truePositives = integer(raw.truePositives);
  const falsePositives = integer(raw.falsePositives);
  const falseNegatives = integer(raw.falseNegatives);
  const trueNegatives = integer(raw.trueNegatives);

  if (
    truePositives === null ||
    falsePositives === null ||
    falseNegatives === null ||
    trueNegatives === null
  ) {
    return null;
  }

  return { truePositives, falsePositives, falseNegatives, trueNegatives };
}

function projectFigures(value: unknown): MetricsFigures | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const matrix = projectMatrix(raw.matrix);
  const precision = nullableDecimal(raw.precision);
  const recall = nullableDecimal(raw.recall);
  const f1 = nullableDecimal(raw.f1);
  const falsePositiveRate = nullableDecimal(raw.falsePositiveRate);
  const flagRate = nullableDecimal(raw.flagRate);

  if (
    matrix === null ||
    precision === undefined ||
    recall === undefined ||
    f1 === undefined ||
    falsePositiveRate === undefined ||
    flagRate === undefined
  ) {
    return null;
  }

  return { matrix, precision, recall, f1, falsePositiveRate, flagRate };
}

function projectThreshold(value: unknown): ThresholdMetrics | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const threshold = integer(raw.threshold);
  const metrics = projectFigures(raw.metrics);

  if (threshold === null || metrics === null) {
    return null;
  }

  return { threshold, metrics };
}

export function projectEvaluationMetrics(value: unknown): EvaluationMetrics | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const scoringRunSequence = integer(raw.scoringRunSequence);
  const scoringRunCompletedAt = instant(raw.scoringRunCompletedAt);
  const ruleConfigVersion = text(raw.ruleConfigVersion);
  const scoredOrders = integer(raw.scoredOrders);
  const labeledOrders = integer(raw.labeledOrders);
  const unlabeledOrders = integer(raw.unlabeledOrders);
  const calibrationOrders = integer(raw.calibrationOrders);
  const holdoutOrders = integer(raw.holdoutOrders);
  const calibrationSweep = projectList(raw.calibrationSweep, projectThreshold);
  const selectedThreshold = projectThreshold(raw.selectedThreshold);
  const holdout = projectFigures(raw.holdout);

  if (
    scoringRunSequence === null ||
    scoringRunCompletedAt === null ||
    ruleConfigVersion === null ||
    scoredOrders === null ||
    labeledOrders === null ||
    unlabeledOrders === null ||
    calibrationOrders === null ||
    holdoutOrders === null ||
    calibrationSweep === null ||
    selectedThreshold === null ||
    holdout === null
  ) {
    return null;
  }

  return {
    scoringRunSequence,
    scoringRunCompletedAt,
    ruleConfigVersion,
    scoredOrders,
    labeledOrders,
    unlabeledOrders,
    calibrationOrders,
    holdoutOrders,
    calibrationSweep,
    selectedThreshold,
    holdout,
  };
}

function projectImportError(value: unknown): ImportRecordError | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const recordNumber = integer(raw.recordNumber);
  const lineNumber = nullableInteger(raw.lineNumber);
  const field = nullableText(raw.field);
  const code = text(raw.code);
  const message = text(raw.message);

  if (
    recordNumber === null ||
    lineNumber === undefined ||
    field === undefined ||
    code === null ||
    message === null
  ) {
    return null;
  }

  return { recordNumber, lineNumber, field, code, message };
}

export function projectImportResult(value: unknown): ImportResult | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const totalRecords = integer(raw.totalRecords);
  const importedCount = integer(raw.importedCount);
  const duplicateCount = integer(raw.duplicateCount);
  const invalidRecordCount = integer(raw.invalidRecordCount);
  const errorsTruncated = flag(raw.errorsTruncated);
  const errors = projectList(raw.errors, projectImportError);

  if (
    totalRecords === null ||
    importedCount === null ||
    duplicateCount === null ||
    invalidRecordCount === null ||
    errorsTruncated === null ||
    errors === null
  ) {
    return null;
  }

  return { totalRecords, importedCount, duplicateCount, invalidRecordCount, errorsTruncated, errors };
}

export function projectScoringRunSummary(value: unknown): ScoringRunSummary | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const runId = text(raw.runId);
  const sequence = integer(raw.sequence);
  const ruleConfigVersion = text(raw.ruleConfigVersion);
  const startedAt = instant(raw.startedAt);
  const completedAt = instant(raw.completedAt);
  const orderCount = integer(raw.orderCount);
  const evaluationsCreated = integer(raw.evaluationsCreated);
  const evaluationsReused = integer(raw.evaluationsReused);
  const alertsCreated = integer(raw.alertsCreated);
  const alertsSkippedOpen = integer(raw.alertsSkippedOpen);
  const alertsSkippedReviewed = integer(raw.alertsSkippedReviewed);

  if (
    runId === null ||
    sequence === null ||
    ruleConfigVersion === null ||
    startedAt === null ||
    completedAt === null ||
    orderCount === null ||
    evaluationsCreated === null ||
    evaluationsReused === null ||
    alertsCreated === null ||
    alertsSkippedOpen === null ||
    alertsSkippedReviewed === null
  ) {
    return null;
  }

  return {
    runId,
    sequence,
    ruleConfigVersion,
    startedAt,
    completedAt,
    orderCount,
    evaluationsCreated,
    evaluationsReused,
    alertsCreated,
    alertsSkippedOpen,
    alertsSkippedReviewed,
  };
}

export function projectSeedResult(value: unknown): SeedResult | null {
  const raw = asRecord(value);
  if (raw === null) {
    return null;
  }

  const datasetVersion = text(raw.datasetVersion);
  const totalOrders = integer(raw.totalOrders);
  const insertedOrders = integer(raw.insertedOrders);
  const duplicateOrders = integer(raw.duplicateOrders);
  const totalLabels = integer(raw.totalLabels);
  const insertedLabels = integer(raw.insertedLabels);
  const fraudLabelCount = integer(raw.fraudLabelCount);

  if (
    datasetVersion === null ||
    totalOrders === null ||
    insertedOrders === null ||
    duplicateOrders === null ||
    totalLabels === null ||
    insertedLabels === null ||
    fraudLabelCount === null
  ) {
    return null;
  }

  return {
    datasetVersion,
    totalOrders,
    insertedOrders,
    duplicateOrders,
    totalLabels,
    insertedLabels,
    fraudLabelCount,
  };
}
