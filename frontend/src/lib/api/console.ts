import "server-only";

import { cache } from "react";

import { DEFAULT_LANGUAGE, type Language } from "./contract";
import type {
  Capabilities,
  Dashboard,
  EvaluationMetrics,
  ImportFormat,
  ImportResult,
  ScoringRunSummary,
  SeedPreview,
  SeedResult,
} from "./contract";
import { type ApiResult, fail, succeed } from "./failures";
import {
  projectCapabilities,
  projectDashboard,
  projectEvaluationMetrics,
  projectImportResult,
  projectScoringRunSummary,
  projectSeedPreview,
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
/**
 * Memoised for the render pass, because two callers now need it: the screen, which decides whether
 * to offer the demo controls, and {@link deploymentLanguage}, which reads the language out of the
 * same response. Asking twice for one answer would double the round trip on every page — and a
 * test asserts that this route is asked exactly once, which is what caught it.
 */
export const fetchCapabilities = cache(async (): Promise<ApiResult<Capabilities>> => {
  return project(await requestJson({ path: "/api/system/capabilities" }), projectCapabilities);
});

/**
 * The language this deployment writes and renders in.
 *
 * <strong>Read from the API and never from the environment of the Next process.</strong>
 * `SALVO_LANGUAGE` has exactly one reader, the API, which refuses to start on a value it cannot
 * write and publishes what it parsed here. Two independent readers of one variable is a deployment
 * where a misconfigured console renders one language around a paragraph in the other, and nothing
 * anywhere reports a problem.
 *
 * Memoised per render pass, so the layout, the page and a server action inside one request agree
 * and pay for one call between them.
 *
 * A failure falls back to the default rather than refusing the page: the language is a concern of
 * presentation, and a screen that cannot say which language it is in still has to render. The
 * unreadable capabilities are announced on the screen by whoever asked for them.
 */
export const deploymentLanguage = cache(async (): Promise<Language> => {
  return languageOf(await fetchCapabilities());
});

/**
 * The language carried by a capabilities read somebody already did.
 *
 * Three of the four screens ask for the capabilities anyway — they decide whether to offer the demo
 * controls — so they take the language out of that answer instead of asking again. Pure, and
 * therefore observable: `fetchCapabilities` is memoised for the render pass, but memoisation is a
 * property of the framework's request scope and a test cannot see it. One read and one decision is
 * a property of this code, and the import screen asserts it.
 */
export function languageOf(capabilities: ApiResult<Capabilities>): Language {
  return capabilities.ok ? capabilities.value.language : DEFAULT_LANGUAGE;
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

/**
 * What loading the demo corpus would do, asked before anybody presses the button.
 *
 * A database that already holds the previous version of the corpus cannot take the current one:
 * an order is immutable and the two versions share their merchant references, so the load refuses.
 * Reading this when the screen opens is what turns that refusal into a sentence the analyst reads
 * beforehand instead of an error she causes.
 */
export async function fetchSeedPreview(): Promise<ApiResult<SeedPreview>> {
  return project(await requestJson({ path: "/api/demo-data/seed-preview" }), projectSeedPreview);
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
