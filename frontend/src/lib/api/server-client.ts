import "server-only";

import { type ApiFailure, type ApiResult, fail, readProblem, succeed } from "./failures";
import { taintApiPayload } from "./taint";

/**
 * The console's only door to the API, and it opens from the server side alone.
 *
 * Three properties of it are load-bearing, and the stage 1 health probe — deleted in this stage —
 * had none of them:
 *
 * - **Absolute URLs.** The `/api/:path*` rewrite in `next.config.ts` only exists for requests that
 *   reach the Next server from a browser. `fetch("/api/alerts")` inside a server component runs in
 *   Node, where a relative URL is a `TypeError`.
 * - **An explicit timeout**, as `AGENTS.md` requires of any external call. Without it a hung API
 *   hangs the server render, and the analyst gets a spinner with no end.
 * - **No caching.** A review has to be visible on the next read; `no-store` keeps the fetch cache
 *   out of the way, and the routes declare themselves dynamic so the build never runs these calls.
 */

const DEFAULT_BASE_URL = "http://127.0.0.1:5100";
const DEFAULT_TIMEOUT_MS = 5_000;

export function apiBaseUrl(): string {
  return process.env.SALVO_API_BASE_URL ?? DEFAULT_BASE_URL;
}

export interface ApiRequest {
  readonly path: string;
  readonly query?: Readonly<Record<string, string | number | undefined>>;
  readonly method?: "GET" | "POST";
  /**
   * A JSON body, or a `FormData` for the multipart import. The two are encoded differently and the
   * distinction is made here rather than at the call sites.
   */
  readonly body?: unknown;
  readonly timeoutMs?: number;
}

/**
 * The default is deliberately short, because every read behind it renders a page an analyst is
 * waiting on. Writes that do real work — an import of ten thousand records, a scoring run over the
 * whole corpus — pass their own, longer value: aborting those at five seconds would abandon a
 * request the API is still committing, and the console would report a timeout for work that
 * succeeded.
 */
function encodeBody(body: unknown): { body: BodyInit; headers?: Record<string, string> } {
  if (body instanceof FormData) {
    // No `Content-Type`: `fetch` has to set it itself so that it carries the multipart boundary.
    return { body };
  }

  return {
    body: JSON.stringify(body),
    headers: { "Content-Type": "application/json" },
  };
}

function buildUrl(request: ApiRequest): URL {
  const url = new URL(request.path, apiBaseUrl());

  for (const [key, value] of Object.entries(request.query ?? {})) {
    if (value !== undefined) {
      url.searchParams.set(key, String(value));
    }
  }

  return url;
}

/**
 * Performs the request and returns the parsed body as `unknown`. Callers project it with a guard;
 * nothing else in the application is allowed to see this value, and it is tainted to make an
 * accidental hand-off to a client component fail loudly instead of silently shipping the payload.
 */
export async function requestJson(request: ApiRequest): Promise<ApiResult<unknown>> {
  const url = buildUrl(request);
  const method = request.method ?? "GET";

  let response: Response;
  try {
    response = await fetch(url, {
      method,
      cache: "no-store",
      signal: AbortSignal.timeout(request.timeoutMs ?? DEFAULT_TIMEOUT_MS),
      ...(request.body === undefined ? {} : encodeBody(request.body)),
    });
  } catch (error) {
    return fail(describeTransportError(error));
  }

  let payload: unknown = null;
  if (response.status !== 204) {
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }
  }

  taintApiPayload(payload);

  if (!response.ok) {
    return fail(readProblem(response.status, payload));
  }

  return succeed(payload);
}

/**
 * `AbortSignal.timeout` aborts with a `TimeoutError`; a user abort would be `AbortError`. Everything
 * else that `fetch` rejects with — refused connection, DNS, TLS — is the API being out of reach, and
 * the distinction matters because the two produce different advice on screen.
 */
function describeTransportError(error: unknown): ApiFailure {
  if (error instanceof DOMException && error.name === "TimeoutError") {
    return { kind: "timeout" };
  }

  if (error instanceof Error && error.name === "TimeoutError") {
    return { kind: "timeout" };
  }

  return { kind: "unreachable" };
}
