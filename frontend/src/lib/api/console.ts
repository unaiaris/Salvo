import "server-only";

import type {
  Capabilities,
  Dashboard,
  EvaluationMetrics,
  ImportFormat,
  ImportResult,
  ScoringRunSummary,
  SeedResult,
} from "./contract";
import { type ApiResult, fail, succeed } from "./failures";
import {
  projectCapabilities,
  projectDashboard,
  projectEvaluationMetrics,
  projectImportResult,
  projectScoringRunSummary,
  projectSeedResult,
} from "./guards";
import { requestJson } from "./server-client";

/**
 * The reads and writes behind `/import` and `/dashboard`.
 *
 * Kept apart from `alerts.ts` for the same reason the endpoints are apart: this module is where the
 * console changes the corpus, and the alert module is where it reads and reviews it.
 */

/**
 * Work that touches every order needs more than the five seconds a page read gets. An import of ten
 * thousand records parses and writes them in one transaction, and a scoring run evaluates the whole
 * corpus in order; aborting either one mid-flight would leave the analyst reading "tardó demasiado"
 * about a request the API went on to commit.
 */
const IMPORT_TIMEOUT_MS = 60_000;
const SCORING_TIMEOUT_MS = 120_000;

function project<T>(result: ApiResult<unknown>, guard: (value: unknown) => T | null): ApiResult<T> {
  if (!result.ok) {
    return result;
  }

  const projected = guard(result.value);

  return projected === null ? fail({ kind: "malformed" }) : succeed(projected);
}

/**
 * What this backend can do.
 *
 * `DemoData:Enabled` is runtime configuration of the API and the frontend build is identical either
 * way, so the console asks instead of probing: a 404 on the seed route is indistinguishable from a
 * misspelled path or an API that is down.
 */
export async function fetchCapabilities(): Promise<ApiResult<Capabilities>> {
  return project(await requestJson({ path: "/api/system/capabilities" }), projectCapabilities);
}

export async function fetchDashboard(): Promise<ApiResult<Dashboard>> {
  return project(await requestJson({ path: "/api/dashboard" }), projectDashboard);
}

/**
 * The quality surface. Only ever called once `fetchCapabilities` has said the route exists: calling
 * it otherwise would turn a deliberate configuration into a 404 the console would have to guess at.
 */
export async function fetchEvaluationMetrics(): Promise<ApiResult<EvaluationMetrics>> {
  return project(await requestJson({ path: "/api/evaluation-metrics" }), projectEvaluationMetrics);
}

export async function importOrders(
  file: File,
  format: ImportFormat,
): Promise<ApiResult<ImportResult>> {
  const form = new FormData();
  form.set("file", file, file.name);
  form.set("format", format);

  const result = await requestJson({
    path: "/api/order-imports",
    method: "POST",
    body: form,
    timeoutMs: IMPORT_TIMEOUT_MS,
  });

  return project(result, projectImportResult);
}

export async function seedDemoOrders(): Promise<ApiResult<SeedResult>> {
  const result = await requestJson({
    path: "/api/demo-data/seed",
    method: "POST",
    timeoutMs: IMPORT_TIMEOUT_MS,
  });

  return project(result, projectSeedResult);
}

/**
 * The step that turns imported orders into evaluations and alerts.
 *
 * Nothing else in the console performs it, and no read triggers it: importing does not process, and
 * a corpus that was never scored stays invisible to the feed and to the dashboard until somebody
 * asks for this.
 */
export async function runScoring(): Promise<ApiResult<ScoringRunSummary>> {
  const result = await requestJson({
    path: "/api/risk-evaluations:run",
    method: "POST",
    timeoutMs: SCORING_TIMEOUT_MS,
  });

  return project(result, projectScoringRunSummary);
}
