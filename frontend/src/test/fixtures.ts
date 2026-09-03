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

export function wireSignal(overrides: WirePayload = {}): WirePayload {
  return {
    rule: "amount_anomaly",
    weight: 40,
    detail: "El monto es 6,2 veces la mediana del comprador.",
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
      signals: [wireSignal(), wireSignal({ rule: "velocity", weight: 25, detail: "4 pedidos en 1 h." })],
    },
    currentEvaluation: {
      evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
      score: 100,
      severity: "CRITICAL",
      isFlagged: true,
      signals: [wireSignal()],
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
    review: null,
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
