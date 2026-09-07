import { describe, expect, it } from "vitest";

import type { AlertDetail } from "@/lib/api/contract";
import { projectAlertDetail } from "@/lib/api/guards";
import { wireAlertDetail, wireLegacySignal, wireSignal } from "@/test/fixtures";
import { describeDivergence } from "./divergence";

function detailFrom(overrides: Record<string, unknown>): AlertDetail {
  const projected = projectAlertDetail(wireAlertDetail(overrides));
  if (projected === null) {
    throw new Error("La fixture no respeta el contrato.");
  }

  return projected;
}

describe("divergencia", () => {
  it("no avisa nada cuando la evaluación vigente es la del snapshot", () => {
    expect(describeDivergence(detailFrom({})).kind).toBe("none");
  });

  it("bloquea cuando cambió la banda", () => {
    const notice = describeDivergence(
      detailFrom({
        divergence: {
          hasBandDivergence: true,
          snapshotScore: 100,
          snapshotSeverity: "CRITICAL",
          currentScore: 45,
          currentSeverity: "MEDIUM",
        },
      }),
    );

    expect(notice.kind).toBe("blocking");
    if (notice.kind === "blocking") {
      // Dice en palabras qué cambió: severidades escritas y los dos scores.
      expect(notice.summary).toContain("CRÍTICA");
      expect(notice.summary).toContain("MEDIA");
      expect(notice.summary).toContain("100");
      expect(notice.summary).toContain("45");
    }
  });

  it("bloquea y lo explica cuando el pedido se quedó sin banda vigente", () => {
    const notice = describeDivergence(
      detailFrom({
        divergence: {
          hasBandDivergence: true,
          snapshotScore: 100,
          snapshotSeverity: "CRITICAL",
          currentScore: 20,
          currentSeverity: null,
        },
      }),
    );

    expect(notice.kind).toBe("blocking");
    if (notice.kind === "blocking") {
      expect(notice.summary).toMatch(/por debajo del umbral/i);
    }
  });

  it("avisa sin bloquear cuando la evaluación cambió dentro de la misma banda", () => {
    // La API acepta este veredicto sin reconocimiento; ser más estricto acá crearía un 200 que
    // ningún otro cliente podría alcanzar.
    const notice = describeDivergence(
      detailFrom({
        snapshot: {
          evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
          score: 90,
          severity: "CRITICAL",
          signals: [],
        },
        currentEvaluation: {
          evaluationId: "4f4b7f3e-0000-4000-8000-000000000004",
          score: 100,
          severity: "CRITICAL",
          isFlagged: true,
          signals: [],
          evaluatedAt: "2026-08-31T10:02:00+00:00",
        },
        divergence: {
          hasBandDivergence: false,
          snapshotScore: 90,
          snapshotSeverity: "CRITICAL",
          currentScore: 100,
          currentSeverity: "CRITICAL",
        },
      }),
    );

    expect(notice.kind).toBe("advisory");
    if (notice.kind === "advisory") {
      expect(notice.summary).toContain("90");
      expect(notice.summary).toContain("100");
      expect(notice.summary).toMatch(/sin cambiar de banda/i);
    }
  });

  it("no inventa un aviso cuando no hay evaluación vigente y tampoco hay divergencia de banda", () => {
    const notice = describeDivergence(detailFrom({ currentEvaluation: null }));

    expect(notice.kind).toBe("none");
  });
});

describe("divergencia y versiones del motor", () => {
  /**
   * La razón por la que el aviso compara score y señales en vez de identificadores. Cuando el
   * motor sube de versión, cada evaluación se vuelve a escribir con otro fingerprint y por lo
   * tanto con otro identificador, sin que nada del riesgo haya cambiado. Comparando
   * identificadores, **toda** alerta de la base anunciaba «la evaluación cambió (100 → 100)».
   */
  it("no avisa cuando la evaluación vigente es otra fila con el mismo score y las mismas reglas", () => {
    const notice = describeDivergence(
      detailFrom({
        currentEvaluation: {
          evaluationId: "9f9b7f3e-0000-4000-8000-000000000009",
          score: 100,
          severity: "CRITICAL",
          isFlagged: true,
          signals: [
            wireLegacySignal({ detail: "Otra redacción de la misma señal." }),
            wireLegacySignal({ rule: "velocity", weight: 25, detail: "Otra redacción también." }),
          ],
          evaluatedAt: "2026-09-06T10:02:00+00:00",
        },
      }),
    );

    expect(notice.kind).toBe("none");
  });

  /**
   * The case this whole notice was rewritten for, now that it has actually happened.
   *
   * An alert opened under `e3-v1` keeps a snapshot of English sentences, and rescoring under
   * `e3-v2` writes a new evaluation row of the same order with the same score and the same rules at
   * the same weights. Nothing about the risk changed — only how a signal is written — so the
   * console must say nothing. Comparing identifiers, or comparing `detail`, would announce «la
   * evaluación cambió» on every alert in the database at once, which is the fastest way to teach an
   * analyst to ignore the notice.
   */
  it("no avisa cuando lo único que cambió es la versión que escribió las señales", () => {
    const notice = describeDivergence(
      detailFrom({
        snapshot: {
          evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
          score: 100,
          severity: "CRITICAL",
          signals: [
            wireLegacySignal({ weight: 40 }),
            wireLegacySignal({ rule: "velocity", weight: 25, detail: "4 orders within 10 minutes." }),
          ],
        },
        currentEvaluation: {
          evaluationId: "9f9b7f3e-0000-4000-8000-000000000009",
          score: 100,
          severity: "CRITICAL",
          isFlagged: true,
          signals: [wireSignal({ weight: 40 }), wireSignal({ rule: "velocity", weight: 25 })],
          evaluatedAt: "2026-09-06T10:02:00+00:00",
        },
      }),
    );

    expect(notice.kind).toBe("none");
  });

  it("sí avisa cuando cambió el peso de una regla, aunque el score total no se mueva", () => {
    const notice = describeDivergence(
      detailFrom({
        snapshot: {
          evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
          score: 100,
          severity: "CRITICAL",
          signals: [
            wireSignal({ weight: 40 }),
            wireSignal({ rule: "velocity", weight: 25 }),
          ],
        },
        currentEvaluation: {
          evaluationId: "9f9b7f3e-0000-4000-8000-000000000009",
          score: 100,
          severity: "CRITICAL",
          isFlagged: true,
          signals: [
            wireSignal({ weight: 30 }),
            wireSignal({ rule: "velocity", weight: 35 }),
          ],
          evaluatedAt: "2026-09-06T10:02:00+00:00",
        },
      }),
    );

    expect(notice.kind).toBe("advisory");
  });

  it("sí avisa cuando una regla dejó de dispararse", () => {
    const notice = describeDivergence(
      detailFrom({
        currentEvaluation: {
          evaluationId: "9f9b7f3e-0000-4000-8000-000000000009",
          score: 100,
          severity: "CRITICAL",
          isFlagged: true,
          signals: [wireSignal({ weight: 40 })],
          evaluatedAt: "2026-09-06T10:02:00+00:00",
        },
      }),
    );

    expect(notice.kind).toBe("advisory");
  });
});
