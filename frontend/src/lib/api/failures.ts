/**
 * Why a read or a write against the API did not produce a value.
 *
 * Every failure is a value, never an exception that escapes: a server component that throws renders
 * the framework error page, and the console has to be able to say what happened in Spanish and offer
 * a way forward.
 */
export type ApiFailure =
  | {
      /** The API answered with `application/problem+json`. */
      readonly kind: "problem";
      readonly status: number;
      readonly code: string | null;
      readonly detail: string | null;
    }
  /** The request hit its `AbortSignal.timeout` before the API answered. */
  | { readonly kind: "timeout" }
  /** The API could not be reached at all: wrong base URL, process down, DNS, TLS. */
  | { readonly kind: "unreachable" }
  /** The API answered, but the body does not match the contract the console was built against. */
  | { readonly kind: "malformed" };

export type ApiResult<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly failure: ApiFailure };

export function succeed<T>(value: T): ApiResult<T> {
  return { ok: true, value };
}

export function fail<T>(failure: ApiFailure): ApiResult<T> {
  return { ok: false, failure };
}

/**
 * Reads an `application/problem+json` body.
 *
 * `code` is an extension member, so it is absent from the `ProblemDetails` schema and has to be read
 * off the raw body. Everything is optional on the wire: a proxy or a framework-level failure can
 * answer with a body that carries none of it, and the caller still needs a failure it can render.
 */
export function readProblem(status: number, body: unknown): ApiFailure {
  if (typeof body !== "object" || body === null) {
    return { kind: "problem", status, code: null, detail: null };
  }

  const candidate = body as Record<string, unknown>;

  return {
    kind: "problem",
    status,
    code: typeof candidate.code === "string" ? candidate.code : null,
    detail: typeof candidate.detail === "string" ? candidate.detail : null,
  };
}
