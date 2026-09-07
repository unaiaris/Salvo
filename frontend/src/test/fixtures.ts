/**
 * Wire payloads, written the way the API writes them and not the way the guards return them.
 *
 * Two deliberate details. Integers are emitted as JSON numbers here even though ASP.NET Core allows
 * the decimal-string form, and a dedicated test covers the string form separately. And every builder
 * takes an `extra` bag: the boundary and guard tests need to inject keys the console has never heard
 * of, which is the only way to prove that projection drops them instead of forwarding them.
 */

export type WirePayload = Record<string, unknown>;

const RUN = { sequence: 3, completedAt: "2026-09-02T21:14:00+00:00" };

/**
 * A signal as `e3-v2` writes it: its fields, and no sentence.
 *
 * Every field of the contract is present, because that is what the API sends — the ones a rule does
 * not use arrive as `null` rather than missing. `wireLegacySignal` is the other half: an evaluation
 * stored by `e3-v1`, which is the snapshot of every alert opened before the engine changed and is
 * never rewritten.
 */
export function wireSignal(overrides: WirePayload = {}): WirePayload {
  return {
    rule: "amount_anomaly",
    weight: 40,
    amountCents: 201111,
    currencyCode: "BRL",
    ratio: 23.2,
    scope: "buyer",
    medianCents: 8685,
    historyCount: 3,
    windowDays: 90,
    orderCount: null,
    windowMinutes: null,
    threshold: null,
    fromCountry: null,
    toCountry: null,
    elapsedMinutes: null,
    bucketStartHour: null,
    bucketEndHour: null,
    timeZoneId: null,
    observedCount: null,
    totalCount: null,
    sharePercent: null,
    country: null,
    habitualCountry: null,
    detail: null,
    ...overrides,
  };
}

/** A signal stored by `e3-v1`: an English sentence and not one field. */
export function wireLegacySignal(overrides: WirePayload = {}): WirePayload {
  return {
    ...wireSignal(),
    amountCents: null,
    currencyCode: null,
    ratio: null,
    scope: null,
    medianCents: null,
    historyCount: null,
    windowDays: null,
    detail: "201111 BRL cents is 23.2x the buyer median 8685 over 3 prior orders in 90 days.",
    ...overrides,
  };
}

export function wireOrder(overrides: WirePayload = {}): WirePayload {
  return {
    id: "1f1b7f3e-0000-4000-8000-000000000001",
    merchantId: "merchant-demo",
    merchantReferenceId: "ORD-1042",
    buyerReferenceId: "buyer-0317",
    occurredAt: "2026-08-14T23:41:00+00:00",
    amountCents: 184_500,
    currencyCode: "UYU",
    countryCode: "UY",
    city: "Montevideo",
    deviceSessionId: "dev-91af",
    ...overrides,
  };
}

export function wireAlertDetail(overrides: WirePayload = {}): WirePayload {
  return {
    id: "2f2b7f3e-0000-4000-8000-000000000002",
    orderId: "1f1b7f3e-0000-4000-8000-000000000001",
    status: "OPEN",
    severity: "CRITICAL",
    alertPolicyVersion: "E4-V1",
    supersedesAlertId: null,
    createdAt: "2026-09-02T21:14:00+00:00",
    reviewedAt: null,
    order: wireOrder(),
    snapshot: {
      evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
      score: 100,
      severity: "CRITICAL",
      signals: [wireSignal(), wireSignal({ rule: "velocity", weight: 25, amountCents: null, currencyCode: null, ratio: null, scope: null, medianCents: null, historyCount: null, windowDays: null, orderCount: 4, windowMinutes: 10, threshold: 4 })],
    },
    // The same evaluation as the snapshot, so it carries the same signals. It used to carry a
    // shorter list under the same identifier, which no API response can produce: an evaluation is
    // identified by a fingerprint over its own score and signals.
    currentEvaluation: {
      evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
      score: 100,
      severity: "CRITICAL",
      isFlagged: true,
      signals: [wireSignal(), wireSignal({ rule: "velocity", weight: 25, amountCents: null, currencyCode: null, ratio: null, scope: null, medianCents: null, historyCount: null, windowDays: null, orderCount: 4, windowMinutes: 10, threshold: 4 })],
      evaluatedAt: "2026-08-31T10:02:00+00:00",
    },
    currentRun: { ...RUN },
    divergence: {
      hasBandDivergence: false,
      snapshotScore: 100,
      snapshotSeverity: "CRITICAL",
      currentScore: 100,
      currentSeverity: "CRITICAL",
    },
    externalEvaluation: wireExternalEvaluation(),

    // Nobody has asked for one, which is the ordinary state. The keys still have to be here: the
    // guard projects a nullable member by telling `null` from a key that is absent, and an absent
    // one makes the whole detail malformed.
    explanation: null,
    currentExplanation: null,
    review: null,
    ...overrides,
  };
}

/**
 * A written explanation of the evaluation the snapshot froze.
 *
 * `summary` is present only because `status` says it may be. A payload carrying text on anything
 * but a ready explanation is one the guard refuses, and a test that wants to prove that builds it
 * by overriding `status` here.
 */
export function wireExplanation(overrides: WirePayload = {}): WirePayload {
  return {
    id: "6f6b7f3e-0000-4000-8000-000000000006",
    provider: "MOCK",
    templateVersion: "e7-v1",
    providerVersion: null,
    status: "READY",
    summary: "El pedido obtuvo 100 puntos sobre un umbral de 60.",
    referencedRules: ["amount_anomaly"],
    failureCode: null,
    attemptCount: 1,
    attemptsExhausted: false,
    isOutdated: false,
    writtenByAnotherTemplate: false,
    requestedAt: "2026-09-03T11:00:00+00:00",
    settledAt: "2026-09-03T11:00:01+00:00",
    ...overrides,
  };
}

export function wireAlertListItem(overrides: WirePayload = {}): WirePayload {
  return {
    id: "2f2b7f3e-0000-4000-8000-000000000002",
    orderId: "1f1b7f3e-0000-4000-8000-000000000001",
    merchantReferenceId: "ORD-1042",
    buyerReferenceId: "buyer-0317",
    occurredAt: "2026-08-14T23:41:00+00:00",
    amountCents: 184_500,
    currencyCode: "UYU",
    countryCode: "UY",
    status: "OPEN",
    severity: "CRITICAL",
    riskScoreSnapshot: 100,
    currentRiskScore: 100,
    currentSeverity: "CRITICAL",
    hasBandDivergence: false,
    alertPolicyVersion: "E4-V1",
    supersedesAlertId: null,
    createdAt: "2026-09-02T21:14:00+00:00",
    reviewedAt: null,
    ...overrides,
  };
}

export function wireAlertList(overrides: WirePayload = {}): WirePayload {
  return {
    items: [wireAlertListItem()],
    page: 1,
    pageSize: 200,
    totalCount: 1,
    scoringRunSequence: 3,
    currentRun: { ...RUN },
    ...overrides,
  };
}

/** A JSON response, the shape `fetch` hands back. */
export function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

/** An `application/problem+json` rejection carrying the API's `code` extension. */
export function problemResponse(status: number, code: string, detail: string): Response {
  return new Response(
    JSON.stringify({
      type: "about:blank",
      title: "Alert request rejected",
      status,
      detail,
      code,
    }),
    { status, headers: { "Content-Type": "application/problem+json" } },
  );
}

export function wireCapabilities(overrides: WirePayload = {}): WirePayload {
  return { demoDataEnabled: true, externalCallbackTriggerEnabled: true, ...overrides };
}

/**
 * The provider's opinion on an order. Denied by default and paired with a flagged local evaluation,
 * so the two criteria agree unless a test asks them not to.
 */
export function wireExternalEvaluation(overrides: WirePayload = {}): WirePayload {
  return {
    id: "5f5b7f3e-0000-4000-8000-000000000005",
    provider: "EXTERNAL_MOCK",
    status: "DENIED",
    score: 71,
    errorCode: null,
    lastErrorCode: null,
    settledBy: "CALLBACK",
    requestedAt: "2026-09-03T10:00:00+00:00",
    settledAt: "2026-09-03T10:00:02+00:00",
    hasContradictoryCallback: false,
    ...overrides,
  };
}

export function wireDashboard(overrides: WirePayload = {}): WirePayload {
  return {
    scoringRun: { sequence: 3, completedAt: "2026-09-02T21:14:00+00:00", orderCount: 300 },
    ordersPendingScoring: 0,
    openAlerts: {
      total: 18,
      // The API only reports the bands that have alerts: `HIGH` is absent because the demo corpus
      // has none, and the dashboard is what has to show the empty band anyway.
      bySeverity: [
        { severity: "CRITICAL", alertCount: 5 },
        { severity: "MEDIUM", alertCount: 13 },
      ],
    },
    amountAtRisk: [
      { currencyCode: "BRL", amountCents: 1_284_512, alertCount: 6 },
      { currencyCode: "USD", amountCents: 1_142_890, alertCount: 6 },
      { currencyCode: "UYU", amountCents: 1_514_844, alertCount: 6 },
    ],
    reportedFraud: [{ currencyCode: "UYU", amountCents: 402_100, orderCount: 2 }],
    flagRate: 0.06,
    riskOverTime: [
      { weekStart: "2026-08-10", orderCount: 21, flaggedCount: 2 },
      { weekStart: "2026-08-17", orderCount: 18, flaggedCount: 0 },
      { weekStart: "2026-08-24", orderCount: 24, flaggedCount: 5 },
    ],
    topSignals: [
      { rule: "amount_anomaly", alertCount: 18 },
      { rule: "foreign_country", alertCount: 18 },
    ],
    externalDenialsWithoutAlert: {
      total: 2,
      listed: 2,
      items: [
        {
          merchantReferenceId: "ORD_000275",
          occurredAt: "2026-08-18T22:47:00+00:00",
          amountCents: 283_204,
          currencyCode: "UYU",
          countryCode: "UY",
          localRiskScore: 0,
        },
        {
          merchantReferenceId: "ORD_000079",
          occurredAt: "2026-05-30T19:26:00+00:00",
          amountCents: 274,
          currencyCode: "USD",
          countryCode: "AR",
          localRiskScore: null,
        },
      ],
    },
    ...overrides,
  };
}

/** What loading the demo corpus would do. No conflict unless a test asks for one. */
export function wireSeedPreview(overrides: WirePayload = {}): WirePayload {
  return {
    datasetVersion: "2",
    totalOrders: 300,
    ordersToInsert: 300,
    duplicateOrders: 0,
    labelsToInsert: 300,
    conflict: null,
    ...overrides,
  };
}

function wireFigures(overrides: WirePayload = {}): WirePayload {
  return {
    matrix: { truePositives: 6, falsePositives: 0, falseNegatives: 0, trueNegatives: 94 },
    precision: 1,
    recall: 1,
    f1: 1,
    falsePositiveRate: 0,
    flagRate: 0.06,
    ...overrides,
  };
}

export function wireEvaluationMetrics(overrides: WirePayload = {}): WirePayload {
  return {
    scoringRunSequence: 3,
    scoringRunCompletedAt: "2026-09-02T21:14:00+00:00",
    ruleConfigVersion: "e3-v1",
    scoredOrders: 300,
    labeledOrders: 300,
    unlabeledOrders: 0,
    calibrationOrders: 200,
    holdoutOrders: 100,
    calibrationSweep: [
      { threshold: 40, metrics: wireFigures({ precision: "0.75", falsePositiveRate: "0.02" }) },
      { threshold: 60, metrics: wireFigures() },
    ],
    selectedThreshold: { threshold: 60, metrics: wireFigures() },
    holdout: wireFigures(),
    ...overrides,
  };
}

export function wireImportResult(overrides: WirePayload = {}): WirePayload {
  return {
    totalRecords: 12,
    importedCount: 9,
    duplicateCount: 1,
    invalidRecordCount: 2,
    errorsTruncated: false,
    errors: [
      {
        recordNumber: 4,
        lineNumber: 5,
        field: "amountCents",
        code: "OUT_OF_RANGE",
        message: "amountCents must be greater than zero.",
      },
      {
        recordNumber: 11,
        lineNumber: null,
        field: null,
        code: "REFERENCE_CONFLICT",
        message: "The merchant reference already exists with different data.",
      },
    ],
    ...overrides,
  };
}

export function wireScoringRunSummary(overrides: WirePayload = {}): WirePayload {
  return {
    runId: "4f4b7f3e-0000-4000-8000-000000000004",
    sequence: 4,
    ruleConfigVersion: "e3-v1",
    startedAt: "2026-09-03T12:00:00+00:00",
    completedAt: "2026-09-03T12:00:11+00:00",
    orderCount: 300,
    evaluationsCreated: 12,
    evaluationsReused: 288,
    alertsCreated: 2,
    alertsSkippedOpen: 16,
    alertsSkippedReviewed: 0,
    ...overrides,
  };
}

export function wireSeedResult(overrides: WirePayload = {}): WirePayload {
  return {
    datasetVersion: "demo-v1",
    totalOrders: 300,
    insertedOrders: 300,
    duplicateOrders: 0,
    totalLabels: 300,
    insertedLabels: 300,
    fraudLabelCount: 18,
    ...overrides,
  };
}
