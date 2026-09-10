import "server-only";

import { COLD_START_GRACE_MS, COLD_START_RETRY_GAP_MS, isSharedInstance } from "./deployment";
import { type ApiFailure, type ApiResult, fail, readProblem, succeed } from "./failures";
import { taintApiPayload } from "./taint";

/**
 * The console's only door to the API, and it opens from the server side alone.
 *
 * Three properties of it are load-bearing, and the stage 1 health probe — deleted in this stage —
 * had none of them:
 *
 * - **Absolute URLs.** `fetch("/api/alerts")` inside a server component runs in Node, where a
 *   relative URL is a `TypeError`: there is no origin to resolve it against. Until stage 10 a
 *   rewrite in `next.config.ts` mapped `/api/:path*` to the API, and this comment named it as the
 *   thing absolute URLs were not relying on. It was deleted — it served no request this client
 *   ever made, and it published the whole API on the public port — so the reason is now simpler
 *   than it was: an absolute URL is the only kind that works here, and nothing forwards a relative
 *   one on this client's behalf.
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
  const first = await attempt(request);

  if (!shouldWaitForColdStart(request, first)) {
    return first;
  }

  return retryWhileStarting(request, first);
}

/**
 * Si este fallo es «la API todavía no está» y no «la API está rota».
 *
 * Los dos casos no se pueden distinguir desde acá, y no hace falta: en la instancia compartida
 * terminan igual. O la API está terminando de arrancar, o el supervisor del punto de entrada va a
 * matar el contenedor y la plataforma lo va a rehacer. En los dos la respuesta honesta es esperar
 * un momento.
 *
 * Tres condiciones, y las tres son necesarias:
 *
 * - **Solo en la instancia compartida.** En una máquina de desarrollo una API que no contesta es
 *   una avería y quien la corre necesita enterarse ya, no dentro de medio minuto.
 * - **Solo en `GET`.** `AGENTS.md` pide que los reintentos existan únicamente donde sean
 *   semánticamente seguros. Una lectura es idempotente; reintentar la importación de un archivo o
 *   la emisión de un veredicto podría duplicar un efecto que la API ya aplicó y cuya respuesta se
 *   perdió.
 * - **Solo por transporte.** Un `problem` es la API contestando, y una respuesta que no cumple el
 *   contrato no mejora porque se la vuelva a pedir.
 */
function shouldWaitForColdStart(request: ApiRequest, result: ApiResult<unknown>): boolean {
  if (result.ok || (request.method ?? "GET") !== "GET") {
    return false;
  }

  const kind = result.failure.kind;

  return (kind === "timeout" || kind === "unreachable") && isSharedInstance();
}

/**
 * Vuelve a pedir mientras la API termina de arrancar, hasta agotar el presupuesto.
 *
 * El presupuesto se cuenta sobre el reloj y no en número de intentos, porque cada intento consume
 * su propio plazo y un contador de intentos escondería cuánto llega a esperar el visitante de
 * verdad. Al agotarse devuelve **el último fallo**, no el primero: si la API pasó de inalcanzable a
 * lenta, lo segundo describe mejor en qué estado quedó.
 */
async function retryWhileStarting(
  request: ApiRequest,
  first: ApiResult<unknown>,
): Promise<ApiResult<unknown>> {
  const deadline = Date.now() + COLD_START_GRACE_MS;
  let last = first;

  while (Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, COLD_START_RETRY_GAP_MS));

    last = await attempt(request);

    if (!shouldWaitForColdStart(request, last)) {
      return last;
    }
  }

  return last;
}

async function attempt(request: ApiRequest): Promise<ApiResult<unknown>> {
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
