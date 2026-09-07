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
    expect(describeDivergence(detailFrom({}), "es").kind).toBe("none");
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
      "es",
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
      "es",
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
      "es",
    );

    expect(notice.kind).toBe("advisory");
    if (notice.kind === "advisory") {
      expect(notice.summary).toContain("90");
      expect(notice.summary).toContain("100");
      expect(notice.summary).toMatch(/sin cambiar de banda/i);
    }
  });

  it("no inventa un aviso cuando no hay evaluación vigente y tampoco hay divergencia de banda", () => {
    const notice = describeDivergence(detailFrom({ currentEvaluation: null }), "es");

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
      "es",
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
      "es",
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
      "es",
    );

    expect(notice.kind).toBe("advisory");
    if (notice.kind === "advisory") {
      // 100 → 100 no se anuncia como un cambio de score, porque no lo es.
      expect(notice.summary).not.toMatch(/100 → 100/);
      expect(notice.summary).toMatch(/Las señales del pedido cambiaron/i);
      expect(notice.summary).toMatch(/el score sigue en 100/i);
    }
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
      "es",
    );

    expect(notice.kind).toBe("advisory");
    if (notice.kind === "advisory") {
      expect(notice.summary).not.toMatch(/100 → 100/);
    }
  });

  /**
   * La contradicción que el aviso tenía, escrita como prueba.
   *
   * `hasMoved` avisa cuando cambian las reglas aunque el total no se mueva —intercambiar dos reglas
   * del mismo peso es el caso de manual—, y con una sola redacción eso salía como «La evaluación del
   * pedido cambió (70 → 70)». En pantalla se disimula; leído en voz alta es un aviso de cambio que
   * repite el mismo número, y quien no ve la pantalla no tiene con qué resolverlo.
   */
  it("con el score quieto, la frase habla de las señales y no de la flecha", () => {
    const notice = describeDivergence(
      detailFrom({
        snapshot: {
          evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
          score: 70,
          severity: "HIGH",
          signals: [wireSignal({ rule: "new_buyer_high_value", weight: 30 })],
        },
        currentEvaluation: {
          evaluationId: "9f9b7f3e-0000-4000-8000-000000000009",
          score: 70,
          severity: "HIGH",
          isFlagged: true,
          signals: [wireSignal({ rule: "velocity", weight: 30 })],
          evaluatedAt: "2026-09-06T10:02:00+00:00",
        },
        divergence: {
          hasBandDivergence: false,
          snapshotScore: 70,
          snapshotSeverity: "HIGH",
          currentScore: 70,
          currentSeverity: "HIGH",
        },
      }),
      "es",
    );

    expect(notice.kind).toBe("advisory");
    if (notice.kind === "advisory") {
      expect(notice.summary).not.toMatch(/→/);
      expect(notice.summary).toMatch(/Las señales del pedido cambiaron/i);
      expect(notice.summary).toMatch(/Evaluación vigente/);
    }
  });

  it("con el score movido sigue diciendo los dos números", () => {
    const notice = describeDivergence(
      detailFrom({
        snapshot: {
          evaluationId: "3f3b7f3e-0000-4000-8000-000000000003",
          score: 70,
          severity: "HIGH",
          signals: [wireSignal({ weight: 30 })],
        },
        currentEvaluation: {
          evaluationId: "9f9b7f3e-0000-4000-8000-000000000009",
          score: 80,
          severity: "HIGH",
          isFlagged: true,
          signals: [wireSignal({ weight: 40 })],
          evaluatedAt: "2026-09-06T10:02:00+00:00",
        },
        divergence: {
          hasBandDivergence: false,
          snapshotScore: 70,
          snapshotSeverity: "HIGH",
          currentScore: 80,
          currentSeverity: "HIGH",
        },
      }),
      "es",
    );

    expect(notice.kind).toBe("advisory");
    if (notice.kind === "advisory") {
      expect(notice.summary).toMatch(/70 → 80/);
    }
  });

  it("la redacción del score quieto también existe en portugués", () => {
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
      "pt",
    );

    expect(notice.kind).toBe("advisory");
    if (notice.kind === "advisory") {
      expect(notice.summary).toMatch(/Os sinais do pedido mudaram/i);
      expect(notice.summary).not.toMatch(/→/);
    }
  });
});
