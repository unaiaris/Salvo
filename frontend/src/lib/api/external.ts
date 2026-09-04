import "server-only";

import type {
  CallbackDelivery,
  CorpusExternalEvaluations,
  ExternalEvaluationRequest,
} from "./contract";
import { type ApiResult, fail, succeed } from "./failures";
import {
  projectCallbackDelivery,
  projectCorpusExternalEvaluations,
  projectExternalEvaluationRequest,
} from "./guards";
import { requestJson } from "./server-client";

/**
 * The console's side of the external provider.
 *
 * Kept apart from `alerts.ts` and `console.ts` because it is a different conversation: those two
 * read and change what this system decided, and this one asks somebody else what they think.
 *
 * Nothing here carries a secret. The authenticated callback endpoint is for external callers and is
 * never reached from this process — the console asks the API to deliver a callback, and the API asks
 * the provider what it would say. A shared secret in the Next process, or worse in the browser,
 * would be the thing section 10 of the Blueprint forbids.
 */

/**
 * Talking to a provider takes longer than reading a page. Asking for the whole corpus talks to it
 * once per order, so it gets the same budget a scoring run does.
 */
const EXTERNAL_TIMEOUT_MS = 30_000;
const CORPUS_TIMEOUT_MS = 120_000;

function project<T>(result: ApiResult<unknown>, guard: (value: unknown) => T | null): ApiResult<T> {
  if (!result.ok) {
    return result;
  }

  const projected = guard(result.value);

  return projected === null ? fail({ kind: "malformed" }) : succeed(projected);
}

/**
 * Asks the provider about one order.
 *
 * Idempotent at the API: an order that already has an evaluation gets that one back with `applied`
 * false, which is what makes a double click harmless here as well.
 */
export async function requestExternalEvaluation(
  orderId: string,
): Promise<ApiResult<ExternalEvaluationRequest>> {
  const result = await requestJson({
    path: `/api/orders/${encodeURIComponent(orderId)}/external-evaluations`,
    method: "POST",
    body: {},
    timeoutMs: EXTERNAL_TIMEOUT_MS,
  });

  return project(result, projectExternalEvaluationRequest);
}

/**
 * Asks the API to deliver the callback of one evaluation, or of every evaluation still waiting for
 * the provider.
 *
 * Only ever called once `fetchCapabilities` has said the route exists. The body names which
 * evaluation and never what it should say: the verdict comes from the provider.
 */
export async function deliverExternalCallbacks(
  externalEvaluationId: string | null,
): Promise<ApiResult<CallbackDelivery>> {
  const result = await requestJson({
    path: "/api/demo-data/external-callbacks:deliver",
    method: "POST",
    body: { externalEvaluationId },
    timeoutMs: CORPUS_TIMEOUT_MS,
  });

  return project(result, projectCallbackDelivery);
}

/**
 * Asks the provider about every order it has never seen.
 *
 * This is how orders that never produced an alert get an external opinion at all: the alert detail
 * can only reach the ones that did, and stage 6 does not add a screen for the rest.
 */
export async function requestCorpusExternalEvaluations(): Promise<
  ApiResult<CorpusExternalEvaluations>
> {
  const result = await requestJson({
    path: "/api/demo-data/external-evaluations:request",
    method: "POST",
    timeoutMs: CORPUS_TIMEOUT_MS,
  });

  return project(result, projectCorpusExternalEvaluations);
}
