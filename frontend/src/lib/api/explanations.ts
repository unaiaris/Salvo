import "server-only";

import type { ExplanationOutcome } from "./contract";
import { type ApiResult, fail, succeed } from "./failures";
import { projectExplanationOutcome } from "./guards";
import { requestJson } from "./server-client";

/**
 * The console's side of the written explanation.
 *
 * There is no read here, and that is the contract rather than an omission: the alert detail carries
 * the explanation, so a separate `GET` would be a second way to ask the same question and a second
 * place for the two answers to disagree. What is left is the one write — ask for one, or ask again
 * for one that failed.
 *
 * Nothing about the text is decided on this side. The API asks the provider, verifies every figure
 * and every rule the answer names against the evaluation, and only then stores it; what comes back
 * here is a row that already passed. A failed row arrives as a `200` with a code, because a provider
 * that invented a figure is an ordinary outcome of asking, not a fault of the request.
 */

/**
 * The provider is given fifteen seconds by `ExplanationOptions.Default`, so a budget of five seconds
 * would report a timeout the API does not have. Twenty leaves room for the round trip on top.
 */
const EXPLANATION_TIMEOUT_MS = 20_000;

export async function requestAlertExplanation(
  alertId: string,
  regenerate: boolean,
): Promise<ApiResult<ExplanationOutcome>> {
  const result = await requestJson({
    path: `/api/alerts/${encodeURIComponent(alertId)}/explanation`,
    method: "POST",
    body: { regenerate },
    timeoutMs: EXPLANATION_TIMEOUT_MS,
  });

  if (!result.ok) {
    return result;
  }

  const projected = projectExplanationOutcome(result.value);

  return projected === null ? fail({ kind: "malformed" }) : succeed(projected);
}
