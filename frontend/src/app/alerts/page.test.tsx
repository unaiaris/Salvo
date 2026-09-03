import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  jsonResponse,
  problemResponse,
  wireAlertList,
  wireAlertListItem,
} from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import AlertsPage from "./page";

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

async function renderFeed() {
  render(await renderableServerTree(AlertsPage()));
}

/** `GET /api/orders` is only reached on the empty path, so the two responses are ordered. */
function respondWith(alerts: unknown, orders?: unknown) {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      url.pathname === "/api/orders" ? jsonResponse(orders ?? { totalCount: 0 }) : jsonResponse(alerts),
    ),
  );
}

describe("cola de alertas", () => {
  it("muestra la procedencia de lo que está viendo", async () => {
    respondWith(wireAlertList());
    await renderFeed();

    expect(screen.getByText(/Vigente desde la corrida #3/)).toBeInTheDocument();
  });

  it("muestra la severidad con texto además de color", async () => {
    respondWith(wireAlertList());
    await renderFeed();

    expect(screen.getAllByText("CRÍTICA").length).toBeGreaterThan(0);
  });

  it("distingue el score del snapshot del score vigente", async () => {
    respondWith(
      wireAlertList({
        items: [wireAlertListItem({ riskScoreSnapshot: 100, currentRiskScore: 45, currentSeverity: null })],
      }),
    );
    await renderFeed();

    expect(screen.getByRole("columnheader", { name: /Score del snapshot/i })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: /Score vigente/i })).toBeInTheDocument();
    expect(screen.getByText("SIN BANDA")).toBeInTheDocument();
  });

  it("dice «sin evaluación vigente», nunca un cero", async () => {
    respondWith(
      wireAlertList({
        items: [wireAlertListItem({ currentRiskScore: null, currentSeverity: null })],
      }),
    );
    await renderFeed();

    expect(screen.getByText(/sin evaluación vigente/i)).toBeInTheDocument();
    expect(screen.queryByText("0")).not.toBeInTheDocument();
  });

  it("enlaza cada alerta a su detalle", async () => {
    respondWith(wireAlertList());
    await renderFeed();

    expect(screen.getByRole("link", { name: "ORD-1042" })).toHaveAttribute(
      "href",
      "/alerts/2f2b7f3e-0000-4000-8000-000000000002",
    );
  });

  it("explica en castellano una API caída y no muestra «Error 500»", async () => {
    fetchMock.mockRejectedValue(new TypeError("fetch failed"));
    await renderFeed();

    const alert = screen.getByRole("alert");
    expect(alert).toHaveTextContent(/No se pudo contactar a la API/i);
    expect(alert).not.toHaveTextContent(/Error \d\d\d/);
  });

  it("traduce un problem+json del feed sin mostrar solo el detalle crudo", async () => {
    fetchMock.mockResolvedValue(problemResponse(400, "INVALID_SORT", "sort must be CREATED_DESC…"));
    await renderFeed();

    expect(screen.getByRole("alert")).toHaveTextContent(/orden que la API no reconoce/i);
    expect(screen.getByRole("alert")).toHaveTextContent(/Detalle técnico de la API/i);
  });
});

describe("los tres estados vacíos", () => {
  const emptyFeed = { items: [], totalCount: 0, scoringRunSequence: null, currentRun: null };

  async function textOfEmptyFeed(alerts: unknown, orders?: unknown): Promise<string> {
    respondWith(alerts, orders);
    const { container } = render(await renderableServerTree(AlertsPage()));

    return container.textContent ?? "";
  }

  it("sin pedidos, invita a importar", async () => {
    const text = await textOfEmptyFeed(wireAlertList(emptyFeed), { totalCount: 0 });

    expect(text).toContain("Todavía no hay pedidos");
    expect(screen.getByRole("link", { name: /Ir a importación/i })).toHaveAttribute(
      "href",
      "/import",
    );
  });

  it("con pedidos y sin corrida, invita a ejecutar el scoring", async () => {
    const text = await textOfEmptyFeed(wireAlertList(emptyFeed), { totalCount: 300 });

    expect(text).toContain("ninguna corrida de scoring");
    expect(text).toContain("300");
    expect(screen.getByRole("link", { name: /Ejecutar scoring/i })).toHaveAttribute(
      "href",
      "/import",
    );
  });

  it("con corrida y sin alertas abiertas, no invita a nada", async () => {
    const text = await textOfEmptyFeed(
      wireAlertList({ items: [], totalCount: 0, scoringRunSequence: 3 }),
    );

    expect(text).toContain("No hay alertas abiertas");
    expect(text).toContain("corrida #3");
    expect(screen.queryByRole("link", { name: /Ejecutar scoring/i })).not.toBeInTheDocument();
  });

  it("los tres textos son distintos entre sí", async () => {
    const withoutOrders = await textOfEmptyFeed(wireAlertList(emptyFeed), { totalCount: 0 });
    cleanup();
    const withoutRun = await textOfEmptyFeed(wireAlertList(emptyFeed), { totalCount: 300 });
    cleanup();
    const withoutOpenAlerts = await textOfEmptyFeed(
      wireAlertList({ items: [], totalCount: 0, scoringRunSequence: 3 }),
    );

    expect(new Set([withoutOrders, withoutRun, withoutOpenAlerts]).size).toBe(3);
  });
});
