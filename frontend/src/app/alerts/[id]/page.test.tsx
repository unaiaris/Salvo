import { render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  jsonResponse,
  problemResponse,
  wireAlertDetail,
  wireExplanation,
  mockConsoleFetch,
} from "@/test/fixtures";
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
  mockConsoleFetch(fetchMock, () => jsonResponse(wireAlertDetail(overrides)));

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
        explanationId: null,
        reviewedAt: "2026-09-02T22:00:00+00:00",
      },
    });

    expect(screen.getByText(/Veredicto registrado: Fraude reportado/i)).toBeInTheDocument();
    expect(screen.getByText(/Coincide con el patrón de la semana pasada/)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Registrar veredicto/i })).not.toBeInTheDocument();
    expect(screen.getByText(/Nada reabre una alerta revisada/i)).toBeInTheDocument();
  });

  /**
   * La otra mitad del hallazgo del foco, y la que sí se puede afirmar sobre el árbol.
   *
   * Una región viva solo se anuncia si ya existía cuando su contenido cambió; una que se inserta
   * junto con su propio texto no dispara nada en ningún lector. Por eso lo que se comprueba no es
   * que el veredicto tenga una región, sino que **el panel tenga la misma región en los dos
   * estados**: vacía y oculta mientras la alerta está abierta, con la frase cuando ya se revisó.
   */
  it("el panel trae su región viva también cuando no tiene nada que decir", async () => {
    await renderDetail();

    const live = screen.getByRole("status");
    expect(live).toHaveTextContent("");
    expect(live).toHaveClass("sr-only");
  });

  it("la alerta revisada anuncia el cambio en esa misma región", async () => {
    await renderDetail({
      status: "REPORTED_FRAUD",
      reviewedAt: "2026-09-02T22:00:00+00:00",
      review: {
        id: "5f5b7f3e-0000-4000-8000-000000000005",
        previousStatus: "OPEN",
        newStatus: "REPORTED_FRAUD",
        note: null,
        explanationId: null,
        reviewedAt: "2026-09-02T22:00:00+00:00",
      },
    });

    expect(screen.getByRole("status")).toHaveTextContent(
      /La alerta quedó revisada y el formulario de veredicto ya no está/i,
    );
  });

  /** El foco necesita un destino: sin `id` y sin `tabIndex`, `focus()` sobre la sección no hace nada. */
  it("el bloque del veredicto es enfocable por código", async () => {
    const { container } = await renderDetail({
      status: "CONFIRMED_SAFE",
      reviewedAt: "2026-09-02T22:00:00+00:00",
      review: {
        id: "5f5b7f3e-0000-4000-8000-000000000005",
        previousStatus: "OPEN",
        newStatus: "CONFIRMED_SAFE",
        note: null,
        explanationId: null,
        reviewedAt: "2026-09-02T22:00:00+00:00",
      },
    });

    const verdict = container.querySelector("#recorded-verdict");
    expect(verdict).not.toBeNull();
    expect(verdict).toHaveAttribute("tabindex", "-1");
  });

  /**
   * The seam itself: `ReviewPanel` is where the `AlertDetail` stops, so it is the only place that
   * can read the explanation's id and hand the form a string. A page that shows a written
   * explanation and a form that records no id would be exactly the ambiguity D10 exists to remove.
   */
  it("el formulario de revisión lleva el id de la explicación que se muestra", async () => {
    const { container } = await renderDetail({ explanation: wireExplanation() });

    expect(container.querySelector('input[name="explanationId"]')).toHaveValue(
      "6f6b7f3e-0000-4000-8000-000000000006",
    );
  });

  it("sin explicación el campo viaja vacío y el formulario funciona igual", async () => {
    const { container } = await renderDetail();

    expect(container.querySelector('input[name="explanationId"]')).toHaveValue("");
    expect(screen.getByRole("button", { name: /Registrar veredicto/i })).toBeInTheDocument();
  });

  it("una alerta inexistente ofrece volver al feed", async () => {
    mockConsoleFetch(fetchMock, () => 
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
