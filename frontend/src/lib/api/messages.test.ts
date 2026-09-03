import { describe, expect, it } from "vitest";

import type { ApiFailure } from "./failures";
import { describeFailure, isKnownFailureCode, knownFailureCodes } from "./messages";

/**
 * Every code the API can answer with has its own message, and none of them falls back to the generic
 * one. The generic text exists for a code this console has never seen — a future stage, a proxy —
 * and reaching it for a known code would mean the analyst reads "algo salió mal" where the API told
 * her exactly what happened.
 */

/** Every `code` the alert endpoints can emit, read off `AlertEndpoints.cs`. */
const API_CODES = [
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

function problem(code: string, status = 409): ApiFailure {
  return { kind: "problem", status, code, detail: "detalle técnico de la API" };
}

describe("mensajes de error", () => {
  it("cubre todos los códigos que emiten los endpoints de alertas", () => {
    for (const code of API_CODES) {
      expect(isKnownFailureCode(code), code).toBe(true);
    }

    expect([...knownFailureCodes()].sort()).toEqual([...API_CODES].sort());
  });

  it("da un texto distinto a cada código", () => {
    const titles = API_CODES.map((code) => describeFailure(problem(code)).title);

    expect(new Set(titles).size).toBe(API_CODES.length);
  });

  it("da una acción de recuperación a cada código", () => {
    for (const code of API_CODES) {
      const message = describeFailure(problem(code));

      expect(message.body.length, code).toBeGreaterThan(0);
      expect(message.recovery.length, code).toBeGreaterThan(0);
    }
  });

  it("ningún código conocido cae en el texto genérico", () => {
    const generic = describeFailure(problem("UN_CODIGO_QUE_NO_EXISTE", 418));

    for (const code of API_CODES) {
      expect(describeFailure(problem(code)).title, code).not.toBe(generic.title);
    }
  });

  it("nunca muestra «Error 409» ni el detalle crudo como texto principal", () => {
    for (const code of API_CODES) {
      const message = describeFailure(problem(code));

      expect(message.title).not.toMatch(/\b(4\d\d|5\d\d)\b/);
      expect(message.title).not.toContain("detalle técnico de la API");
      expect(message.body).not.toContain("detalle técnico de la API");
    }
  });

  it("marca como errores de formulario solo los dos que lo son", () => {
    const formErrors = API_CODES.filter((code) => describeFailure(problem(code)).isFormError);

    expect([...formErrors].sort()).toEqual(["INVALID_STATUS", "NOTE_TOO_LONG"]);
  });

  it("nombra el límite real de la nota", () => {
    expect(describeFailure(problem("NOTE_TOO_LONG", 400)).body).toContain("2000");
  });

  it("dice que un veredicto no se reabre cuando otra persona ya revisó", () => {
    expect(describeFailure(problem("ALERT_ALREADY_REVIEWED")).body).toMatch(/no se reabre/i);
  });

  it("manda de vuelta al aviso de divergencia", () => {
    expect(describeFailure(problem("ALERT_DIVERGENCE_NOT_ACKNOWLEDGED")).recovery).toMatch(
      /casilla/i,
    );
  });

  it("distingue timeout, API caída y contrato roto", () => {
    const titles = (["timeout", "unreachable", "malformed"] as const).map(
      (kind) => describeFailure({ kind }).title,
    );

    expect(new Set(titles).size).toBe(3);
  });

  it("traduce un código desconocido sin fingir que lo entiende", () => {
    const message = describeFailure(problem("SCORING_RUN_CONFLICT", 409));

    expect(message.title).toMatch(/rechazó la operación/i);
    expect(message.body).toContain("409");
  });
});
