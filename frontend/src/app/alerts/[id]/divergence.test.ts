import { describe, expect, it } from "vitest";

import type { AlertDetail } from "@/lib/api/contract";
import { projectAlertDetail } from "@/lib/api/guards";
import { wireAlertDetail } from "@/test/fixtures";
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
