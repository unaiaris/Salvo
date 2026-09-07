import { describe, expect, it } from "vitest";

import type { ApiFailure } from "./failures";
import { describeFailure, isKnownFailureCode, knownFailureCodes } from "./messages";
import { messagesFor } from "@/lib/i18n/dictionary";
import { CONSOLE_LANGUAGES } from "./contract";

/**
 * Every code the API can answer with has its own message, and none of them falls back to the generic
 * one. The generic text exists for a code this console has never seen — a future stage, a proxy —
 * and reaching it for a known code would mean the analyst reads "algo salió mal" where the API told
 * her exactly what happened.
 */

/** Every `code` the alert endpoints can emit, read off `AlertEndpoints.cs`. */
const ALERT_CODES = [
  "ALERT_NOT_FOUND",
  "INVALID_STATUS",
  "NOTE_TOO_LONG",
  "ALERT_ALREADY_REVIEWED",
  "ALERT_REVIEW_NOTE_CONFLICT",
  "ALERT_DIVERGENCE_NOT_ACKNOWLEDGED",
  "ALERT_REVIEW_CONFLICT",
  "INVALID_SORT",
  "INVALID_SEVERITY",
  "INVALID_PAGE",
  "INVALID_PAGE_SIZE",
] as const;

/**
 * What `/import` and `/dashboard` can be answered with: the transport-level rejections of
 * `OrderEndpoints.cs`, the document-level ones every parser under `Salvo.Infrastructure/Imports`
 * throws, and the two conflicts of `RiskEvaluationEndpoints.cs` and `EvaluationMetricsEndpoints.cs`.
 *
 * Per-record errors are deliberately absent: they arrive inside a `200` as `ImportOrdersResult.errors`
 * and describe a row of the analyst's file, not a failure of her request.
 */
const CONSOLE_CODES = [
  "FILE_REQUIRED",
  "FILE_TOO_LARGE",
  "UNSUPPORTED_MEDIA_TYPE",
  "UNSUPPORTED_FORMAT",
  "TOO_MANY_RECORDS",
  "EMPTY_FILE",
  "INVALID_ENCODING",
  "INVALID_CSV",
  "INVALID_JSON",
  "INVALID_JSON_ROOT",
  "MISSING_HEADER",
  "INVALID_HEADER",
  "DUPLICATE_HEADER",
  "UNKNOWN_HEADER",
  "DEMO_DATA_CONFLICT",
  "DEMO_DATA_PREVIOUS_CORPUS",
  "SCORING_RUN_CONFLICT",
  "METRICS_UNAVAILABLE",
] as const;

/**
 * What the external provider surface can be answered with: the conflicts of
 * `ExternalEvaluationEndpoints.cs`, the rejections of `ExternalCallbackEndpoints.cs`, and the not
 * found of `ExternalDemoEndpoints.cs`.
 *
 * The callback codes are here even though this console never sends a callback — it has no secret and
 * never composes one. They are listed because the catalogue is about what the API can emit, and a
 * proxy or a future stage reaching one of them should not land on "algo salió mal".
 */
const EXTERNAL_CODES = [
  "EXTERNAL_EVALUATION_PENDING",
  "EXTERNAL_EVALUATION_SETTLED",
  "EXTERNAL_EVALUATION_CONFLICT",
  "EXTERNAL_EVALUATION_NOT_FOUND",
  "PROVIDER_NOT_REGISTERED",
  "INVALID_PROVIDER",
  "RECONCILIATION_CONFLICT",
  "CALLBACK_UNAUTHORIZED",
  "CALLBACK_UNAVAILABLE",
  "INVALID_CALLBACK",
  "CALLBACK_TOO_LARGE",
] as const;

/**
 * What `POST /api/alerts/{id}/explanation` can be answered with: the three conflicts of
 * `ExplanationEndpoints.ToCode`, plus the fallback that `ExplanationConflictReason.ConcurrentUpdate`
 * reaches — the same shape `EXTERNAL_EVALUATION_CONFLICT` has, and reachable for the same reason.
 *
 * The nine `ExplanationFailureCode` values are deliberately absent. They arrive inside a `200` as
 * `explanation.failureCode` and describe how a redaction ended, not how a request was refused; they
 * are rendered by `explanationFailureLabel`, beside the other wire values the console names.
 */
const EXPLANATION_CODES = [
  "EXPLANATION_PENDING",
  "EXPLANATION_ALREADY_READY",
  "EXPLANATION_ATTEMPTS_EXHAUSTED",
  "EXPLANATION_CONFLICT",
] as const;

const API_CODES = [
  ...ALERT_CODES,
  ...CONSOLE_CODES,
  ...EXTERNAL_CODES,
  ...EXPLANATION_CODES,
] as const;

function problem(code: string, status = 409): ApiFailure {
  return { kind: "problem", status, code, detail: "detalle técnico de la API" };
}

describe("mensajes de error", () => {
  it("cubre todos los códigos que emiten los endpoints, y ninguno de más", () => {
    for (const code of API_CODES) {
      expect(isKnownFailureCode(code), code).toBe(true);
    }

    expect([...knownFailureCodes()].sort()).toEqual([...API_CODES].sort());
  });

  /**
   * The same catalogue in every language, asserted rather than trusted to the type.
   *
   * `Dictionary` already refuses a `pt.ts` with a code missing, so this cannot fail while the
   * dictionaries are typed the way they are. It is here for the day somebody loosens that type:
   * the exactness of the catalogue is decision 57, and it should not rest on one `typeof`.
   */
  it("el catálogo es el mismo en los dos idiomas", () => {
    for (const language of CONSOLE_LANGUAGES) {
      expect(Object.keys(messagesFor(language).failures).sort(), language).toEqual(
        [...API_CODES].sort(),
      );
    }
  });

  /** Every code answers in both languages, and never with the same sentence in both. */
  it("cada código tiene texto propio en los dos idiomas", () => {
    for (const code of API_CODES) {
      const spanish = describeFailure(problem(code), "es");
      const portuguese = describeFailure(problem(code), "pt");

      expect(portuguese.title.length, code).toBeGreaterThan(0);
      expect(portuguese.body.length, code).toBeGreaterThan(0);
      expect(portuguese.recovery.length, code).toBeGreaterThan(0);
      expect(portuguese.title, code).not.toBe(spanish.title);

      // Where a message belongs on screen is a fact about the console, not about the reader.
      expect(portuguese.isFormError, code).toBe(spanish.isFormError);
    }
  });

  it("da un texto distinto a cada código", () => {
    const titles = API_CODES.map((code) => describeFailure(problem(code), "es").title);

    expect(new Set(titles).size).toBe(API_CODES.length);
  });

  it("da una acción de recuperación a cada código", () => {
    for (const code of API_CODES) {
      const message = describeFailure(problem(code), "es");

      expect(message.body.length, code).toBeGreaterThan(0);
      expect(message.recovery.length, code).toBeGreaterThan(0);
    }
  });

  it("ningún código conocido cae en el texto genérico", () => {
    const generic = describeFailure(problem("UN_CODIGO_QUE_NO_EXISTE", 418), "es");

    for (const code of API_CODES) {
      expect(describeFailure(problem(code), "es").title, code).not.toBe(generic.title);
    }
  });

  it("nunca muestra «Error 409» ni el detalle crudo como texto principal", () => {
    for (const code of API_CODES) {
      const message = describeFailure(problem(code), "es");

      expect(message.title).not.toMatch(/\b(4\d\d|5\d\d)\b/);
      expect(message.title).not.toContain("detalle técnico de la API");
      expect(message.body).not.toContain("detalle técnico de la API");
    }
  });

  it("marca como error de formulario lo que la analista puede corregir en el formulario", () => {
    const formErrors = API_CODES.filter((code) => describeFailure(problem(code), "es").isFormError);

    // Un archivo mal formado o de formato equivocado se corrige eligiendo otro archivo, ahí mismo.
    // Un conflicto de corrida o de datos de demo no: no hay campo que cambiar.
    expect([...formErrors].sort()).toEqual(
      [
        "DUPLICATE_HEADER",
        "EMPTY_FILE",
        "FILE_REQUIRED",
        "FILE_TOO_LARGE",
        "INVALID_CSV",
        "INVALID_ENCODING",
        "INVALID_HEADER",
        "INVALID_JSON",
        "INVALID_JSON_ROOT",
        "INVALID_STATUS",
        "MISSING_HEADER",
        "NOTE_TOO_LONG",
        "TOO_MANY_RECORDS",
        "UNKNOWN_HEADER",
        "UNSUPPORTED_FORMAT",
      ].sort(),
    );
  });

  it("nombra el límite real de la nota", () => {
    expect(describeFailure(problem("NOTE_TOO_LONG", 400), "es").body).toContain("2000");
  });

  it("dice que un veredicto no se reabre cuando otra persona ya revisó", () => {
    expect(describeFailure(problem("ALERT_ALREADY_REVIEWED"), "es").body).toMatch(/no se reabre/i);
  });

  it("manda de vuelta al aviso de divergencia", () => {
    expect(describeFailure(problem("ALERT_DIVERGENCE_NOT_ACKNOWLEDGED"), "es").recovery).toMatch(
      /casilla/i,
    );
  });

  it("distingue timeout, API caída y contrato roto", () => {
    const titles = (["timeout", "unreachable", "malformed"] as const).map(
      (kind) => describeFailure({ kind }, "es").title,
    );

    expect(new Set(titles).size).toBe(3);
  });

  it("traduce un código desconocido sin fingir que lo entiende", () => {
    // Un código que ninguna etapa emite: los del proveedor externo ya están en el catálogo.
    const message = describeFailure(problem("EXTERNAL_PROVIDER_TIMEOUT", 409), "es");

    expect(message.title).toMatch(/rechazó la operación/i);
    expect(message.body).toContain("409");
  });

  it("ofrece ejecutar la corrida cuando faltan las métricas, y reintentar cuando la corrida chocó", () => {
    expect(describeFailure(problem("METRICS_UNAVAILABLE"), "es").recovery).toMatch(/corrida de scoring/i);
    expect(describeFailure(problem("SCORING_RUN_CONFLICT"), "es").recovery).toMatch(/volvé a ejecutar/i);
  });

  it("nombra el límite real del archivo de importación", () => {
    expect(describeFailure(problem("FILE_TOO_LARGE", 413), "es").body).toContain("5 MiB");
  });
});
