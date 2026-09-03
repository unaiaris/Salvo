import { render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, problemResponse, wireAlertDetail } from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import AlertDetailPage from "./page";

const ALERT_ID = "2f2b7f3e-0000-4000-8000-000000000002";

let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal("fetch", fetchMock);
  vi.stubEnv("SALVO_API_BASE_URL", "http://127.0.0.1:5100");
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

async function renderDetail(overrides: Record<string, unknown> = {}) {
  fetchMock.mockResolvedValue(jsonResponse(wireAlertDetail(overrides)));

  return render(
    await renderableServerTree(AlertDetailPage({ params: Promise.resolve({ id: ALERT_ID }) })),
  );
}

describe("detalle de la alerta", () => {
  it("presenta el snapshot y la evaluación vigente como dos bloques distintos", async () => {
    await renderDetail();

    expect(
      screen.getByRole("region", { name: /Snapshot que abrió la alerta/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole("region", { name: /Evaluación vigente/i })).toBeInTheDocument();
  });

  it("rotula el bloque vigente con la corrida, no con evaluatedAt", async () => {
    await renderDetail();

    const current = screen.getByRole("region", { name: /Evaluación vigente/i });
    expect(current).toHaveTextContent(/Vigente desde la corrida #3/);
    expect(current).toHaveTextContent(/Calculada por primera vez el/);
    expect(current).toHaveTextContent(/no es el de la corrida vigente/);
  });

  it("cada bloque lleva su propia procedencia", async () => {
    await renderDetail();

    expect(screen.getByRole("region", { name: /Snapshot que abrió la alerta/i })).toHaveTextContent(
      /Congelado el/,
    );
  });

  it("escribe la severidad además de colorearla", async () => {
    await renderDetail();

    expect(screen.getAllByText("CRÍTICA").length).toBeGreaterThan(0);
  });

  it("sin evaluación vigente dice que no la hay, nunca que el score es 0", async () => {
    await renderDetail({ currentEvaluation: null });

    const current = screen.getByRole("region", { name: /Evaluación vigente/i });
    expect(current).toHaveTextContent(/No hay evaluación vigente para este pedido/i);
    expect(current).toHaveTextContent(/No es un score de cero/i);
    expect(within(current).queryByText("0")).not.toBeInTheDocument();
  });

  it("con divergencia de banda muestra la casilla de reconocimiento", async () => {
    await renderDetail({
      divergence: {
        hasBandDivergence: true,
        snapshotScore: 100,
        snapshotSeverity: "CRITICAL",
        currentScore: 45,
        currentSeverity: "MEDIUM",
      },
    });

    expect(screen.getByRole("checkbox")).toBeInTheDocument();
    expect(screen.getByText(/El corpus cambió desde que se abrió esta alerta/i)).toBeInTheDocument();
  });

  it("con cambio dentro de la misma banda avisa sin casilla", async () => {
    await renderDetail({
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
    });

    expect(screen.getByRole("status")).toHaveTextContent(/sin cambiar de banda/i);
    expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Registrar veredicto/i })).toBeInTheDocument();
  });

  it("una alerta ya revisada muestra el veredicto y no el formulario", async () => {
    await renderDetail({
      status: "REPORTED_FRAUD",
      reviewedAt: "2026-09-02T22:00:00+00:00",
      review: {
        id: "5f5b7f3e-0000-4000-8000-000000000005",
        previousStatus: "OPEN",
        newStatus: "REPORTED_FRAUD",
        note: "Coincide con el patrón de la semana pasada.",
        reviewedAt: "2026-09-02T22:00:00+00:00",
      },
    });

    expect(screen.getByText(/Veredicto registrado: Fraude reportado/i)).toBeInTheDocument();
    expect(screen.getByText(/Coincide con el patrón de la semana pasada/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Registrar veredicto/i })).not.toBeInTheDocument();
    expect(screen.getByText(/Nada reabre una alerta revisada/i)).toBeInTheDocument();
  });

  it("una alerta inexistente ofrece volver al feed", async () => {
    fetchMock.mockResolvedValue(
      problemResponse(404, "ALERT_NOT_FOUND", "No alert exists with identifier 2f2b…"),
    );

    render(
      await renderableServerTree(AlertDetailPage({ params: Promise.resolve({ id: ALERT_ID }) })),
    );

    expect(screen.getByRole("alert")).toHaveTextContent(/Esta alerta ya no existe/i);
    expect(screen.getByRole("link", { name: /Volver a la cola de alertas/i })).toHaveAttribute(
      "href",
      "/alerts",
    );
  });

  it("formatea montos e instantes con zona de negocio fija", async () => {
    await renderDetail();

    // 2026-08-14T23:41Z es el 14 en America/Montevideo (UTC−3, 20:41).
    expect(screen.getByText(/14 ago\. 2026, 20:41/)).toBeInTheDocument();
    expect(screen.getByText(/UYU\s*1\.845,00/)).toBeInTheDocument();
  });
});
