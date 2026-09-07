import { render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  jsonResponse,
  problemResponse,
  wireCapabilities,
  wireDashboard,
  wireEvaluationMetrics,
} from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import DashboardPage from "./page";
import { FIXTURE_CAVEAT } from "./quality-section";

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

function respondWith({
  demoDataEnabled = true,
  dashboard = wireDashboard(),
  metrics,
}: {
  demoDataEnabled?: boolean;
  dashboard?: unknown;
  metrics?: unknown;
} = {}) {
  fetchMock.mockImplementation((url: URL) => {
    switch (url.pathname) {
      case "/api/system/capabilities":
        return Promise.resolve(jsonResponse(wireCapabilities({ demoDataEnabled })));
      case "/api/evaluation-metrics":
        return Promise.resolve(
          metrics instanceof Response ? metrics : jsonResponse(metrics ?? wireEvaluationMetrics()),
        );
      default:
        return Promise.resolve(jsonResponse(dashboard));
    }
  });
}

async function renderDashboard() {
  render(await renderableServerTree(DashboardPage()));
}

function requestedPaths(): readonly string[] {
  return fetchMock.mock.calls.map(([url]) => (url as URL).pathname);
}

describe("dashboard operativo", () => {
  it("declara de qué corrida es lo que muestra", async () => {
    respondWith();
    await renderDashboard();

    expect(screen.getByText(/Vigente desde la corrida #3/)).toBeInTheDocument();
    expect(screen.getByText("300 pedidos en la corrida")).toBeInTheDocument();
  });

  it("avisa de los pedidos fuera de la corrida y ofrece ejecutarla", async () => {
    respondWith({ dashboard: wireDashboard({ ordersPendingScoring: 25 }) });
    await renderDashboard();

    const notice = screen.getByRole("status");
    expect(
      within(notice).getByText("Hay 25 pedidos fuera de la corrida vigente"),
    ).toBeInTheDocument();
    expect(within(notice).getByRole("link", { name: "Ejecutar scoring" })).toBeInTheDocument();
  });

  it("muestra las bandas de severidad en cero, no solo las que tienen alertas", async () => {
    respondWith();
    await renderDashboard();

    // El corpus demo no tiene ninguna alerta ALTA y la API por lo tanto no informa esa banda.
    // Omitir la fila haría leer «no existe la banda» donde la verdad es «hoy está vacía».
    expect(screen.getByText("ALTA")).toBeInTheDocument();
    expect(screen.getByText("sin alertas")).toBeInTheDocument();
  });

  it("muestra el monto en riesgo por moneda y no publica ningún total", async () => {
    respondWith();
    await renderDashboard();

    const panel = screen.getByRole("region", { name: /Monto en riesgo/ });
    expect(within(panel).getByText(/BRL/)).toBeInTheDocument();
    expect(within(panel).getByText(/USD/)).toBeInTheDocument();
    expect(within(panel).getByText(/UYU/)).toBeInTheDocument();

    // 1.284.512 + 1.142.890 + 1.514.844 = 3.942.246 centavos: una cifra sin unidad.
    // Ni ese número ni su forma en unidades pueden aparecer en ningún lado de la pantalla.
    const rendered = document.body.textContent ?? "";
    expect(rendered).not.toContain("3.942.246");
    expect(rendered).not.toContain("39.422,46");
  });

  it("presenta el fraude reportado aparte y por pedido, no por alerta", async () => {
    respondWith();
    await renderDashboard();

    const panel = screen.getByRole("region", { name: /Fraude reportado/ });
    expect(
      within(panel).getByText((_, element) => element?.textContent === "2 pedidos"),
    ).toBeInTheDocument();
    expect(within(panel).getByText(/UYU/)).toBeInTheDocument();
  });

  it("dibuja el riesgo temporal como SVG con título, descripción y tabla equivalente", async () => {
    respondWith();
    await renderDashboard();

    const chart = screen.getByRole("img", {
      name: /Pedidos por semana y cuántos de ellos denegó la corrida vigente/,
    });
    expect(chart.tagName.toLowerCase()).toBe("svg");
    expect(chart.querySelector("title")?.textContent).toContain("Pedidos por semana");
    expect(chart.querySelector("desc")?.textContent).toContain("3 semanas");

    // La tabla lleva los mismos números y está siempre en pantalla, no detrás de un hover.
    const table = screen.getByRole("table", {
      name: /Pedidos y denegados por semana/,
    });
    expect(within(table).getAllByRole("row")).toHaveLength(4);
    expect(within(table).getByRole("rowheader", { name: /24 ago/ })).toBeInTheDocument();
  });

  it("no usa ninguna librería de gráficos: el SVG lo escribe el servidor", async () => {
    respondWith();
    await renderDashboard();

    // Un componente cliente no renderiza durante `renderableServerTree` salvo que se lo monte:
    // que el SVG esté en el árbol resuelto es la prueba de que lo produjo el servidor.
    expect(document.querySelectorAll("svg rect").length).toBeGreaterThan(3);
  });

  it("muestra la tasa de marcado sobre la corrida vigente", async () => {
    respondWith();
    await renderDashboard();

    const panel = screen.getByRole("region", { name: /Tasa de marcado/ });
    expect(within(panel).getByText("6,0%")).toBeInTheDocument();
  });

  it("nombra las señales con la etiqueta neutral del monto atípico", async () => {
    respondWith();
    await renderDashboard();

    const panel = screen.getByRole("region", { name: /Señales principales/ });
    // La regla compara contra la mediana del comprador o la del comercio según la historia
    // disponible: el título no puede afirmar una de las dos.
    expect(within(panel).getByText("Monto atípico")).toBeInTheDocument();
    expect(within(panel).queryByText(/Monto atípico para el comprador/)).not.toBeInTheDocument();
  });
});

describe("los tres estados vacíos del dashboard", () => {
  const EMPTY = {
    openAlerts: { total: 0, bySeverity: [] },
    amountAtRisk: [],
    reportedFraud: [],
    riskOverTime: [],
    topSignals: [],
  };

  it("sin pedidos: la base está vacía", async () => {
    respondWith({
      dashboard: wireDashboard({ ...EMPTY, scoringRun: null, ordersPendingScoring: 0, flagRate: null }),
    });
    await renderDashboard();

    expect(screen.getByText("Todavía no hay pedidos")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ir a importación" })).toBeInTheDocument();
  });

  it("con pedidos y sin corrida: nadie los puntuó", async () => {
    respondWith({
      dashboard: wireDashboard({
        ...EMPTY,
        scoringRun: null,
        ordersPendingScoring: 300,
        flagRate: null,
      }),
    });
    await renderDashboard();

    expect(
      screen.getByText("Hay pedidos importados y ninguna corrida de scoring"),
    ).toBeInTheDocument();
    expect(screen.getByText(/Los 300 pedidos de la base no tienen evaluación/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ejecutar scoring" })).toBeInTheDocument();
  });

  it("con corrida y sin alertas abiertas: nada cruzó el umbral", async () => {
    respondWith({ dashboard: wireDashboard({ ...EMPTY, flagRate: 0 }) });
    await renderDashboard();

    expect(screen.getByText("No hay alertas abiertas")).toBeInTheDocument();
    // Es el único de los tres en el que las cifras de riesgo siguen teniendo sentido.
    expect(screen.getByRole("region", { name: /Tasa de marcado/ })).toBeInTheDocument();
  });

  it("los tres dicen cosas distintas", async () => {
    const titles: string[] = [];

    for (const dashboard of [
      wireDashboard({ ...EMPTY, scoringRun: null, ordersPendingScoring: 0, flagRate: null }),
      wireDashboard({ ...EMPTY, scoringRun: null, ordersPendingScoring: 300, flagRate: null }),
      wireDashboard({ ...EMPTY, flagRate: 0 }),
    ]) {
      respondWith({ dashboard });
      await renderDashboard();
      // El encabezado del estado vacío, no el de la sección de calidad que está más abajo.
      const state = screen.getByRole("region", {
        name: /Todavía no hay pedidos|Hay pedidos importados|No hay alertas abiertas/,
      });
      titles.push(within(state).getByRole("heading", { level: 2 }).textContent ?? "");
      cleanupBetweenCases();
    }

    expect(new Set(titles).size).toBe(3);
  });
});

/** Testing Library cleans up after each test, not between renders inside one. */
function cleanupBetweenCases(): void {
  document.body.innerHTML = "";
}

describe("sección de calidad", () => {
  it("aparece con el rótulo de la fixture cuando la instancia es de demostración", async () => {
    respondWith({ demoDataEnabled: true });
    await renderDashboard();

    expect(screen.getByRole("region", { name: "Calidad del criterio" })).toBeInTheDocument();
    expect(screen.getByText(FIXTURE_CAVEAT)).toBeInTheDocument();
  });

  it("no aparece, ni se consulta el endpoint, cuando la instancia no es de demostración", async () => {
    respondWith({ demoDataEnabled: false });
    await renderDashboard();

    expect(screen.queryByRole("region", { name: "Calidad del criterio" })).not.toBeInTheDocument();
    expect(screen.queryByText(FIXTURE_CAVEAT)).not.toBeInTheDocument();
    expect(requestedPaths()).not.toContain("/api/evaluation-metrics");
    // El resto del dashboard no depende de la demo.
    expect(screen.getByRole("region", { name: /Monto en riesgo/ })).toBeInTheDocument();
  });

  it("el rótulo está también cuando las métricas fallan: no se puede quedar sin él", async () => {
    respondWith({
      metrics: problemResponse(409, "METRICS_UNAVAILABLE", "No scoring run has completed."),
    });
    await renderDashboard();

    expect(screen.getByText(FIXTURE_CAVEAT)).toBeInTheDocument();
    expect(
      screen.getByText("Todavía no se pueden calcular las métricas de calidad"),
    ).toBeInTheDocument();
    const failure = screen.getByRole("alert");
    expect(within(failure).getByRole("link", { name: "Ejecutar scoring" })).toBeInTheDocument();
  });

  it("separa el holdout de la calibración y dice cuál no es independiente", async () => {
    respondWith();
    await renderDashboard();

    expect(screen.getByText(/Holdout · 100 pedidos/)).toBeInTheDocument();
    expect(screen.getByText(/Calibración · 200 pedidos/)).toBeInTheDocument();
    expect(screen.getByText(/No es una medición independiente/)).toBeInTheDocument();
  });

  it("cuenta los pedidos sin etiqueta en vez de fallar por ellos", async () => {
    respondWith({
      metrics: wireEvaluationMetrics({ labeledOrders: 300, unlabeledOrders: 42, scoredOrders: 342 }),
    });
    await renderDashboard();

    expect(screen.getByText("Sin etiqueta")).toBeInTheDocument();
    expect(screen.getByText("42")).toBeInTheDocument();
  });

  it("presenta el barrido de umbrales colapsado y marca el elegido", async () => {
    respondWith();
    await renderDashboard();

    const sweep = screen.getByText(/Barrido de umbrales sobre la cohorte de calibración/);
    expect(sweep.tagName.toLowerCase()).toBe("summary");
    expect(sweep.closest("details")?.hasAttribute("open")).toBe(false);
    expect(screen.getByRole("rowheader", { name: /60 · elegido/ })).toBeInTheDocument();
  });
});

describe("panel de denegados por el proveedor sin alerta local", () => {
  it("muestra el conteo y los pedidos, con el score local al lado del veredicto externo", async () => {
    respondWith();
    await renderDashboard();

    const panel = screen.getByRole("region", { name: /Denegados por el proveedor sin alerta local/ });
    const rows = within(panel).getAllByRole("row");

    // Encabezado más las dos filas de la fixture.
    expect(rows).toHaveLength(3);
    expect(within(panel).getByRole("rowheader", { name: "ORD_000275" })).toBeInTheDocument();
    expect(
      within(panel).getByText((_, element) => element?.textContent === "2 pedidos"),
    ).toBeInTheDocument();
    // El score local viaja con cada fila: sin él la fila dice que hay desacuerdo y no cuál.
    expect(within(rows[1]!).getByText("0")).toBeInTheDocument();
    // Y un pedido que la corrida vigente no cubre se dice con palabras, no con un cero.
    expect(within(panel).getByText("sin puntuar")).toBeInTheDocument();
  });

  it("explica el panel vacío en vez de desaparecer", async () => {
    respondWith({
      dashboard: wireDashboard({
        externalDenialsWithoutAlert: { total: 0, listed: 0, items: [] },
      }),
    });
    await renderDashboard();

    const panel = screen.getByRole("region", { name: /Denegados por el proveedor sin alerta local/ });
    expect(within(panel).getByText(/nadie pidió todavía la evaluación externa/i)).toBeInTheDocument();
    expect(within(panel).queryByRole("table")).not.toBeInTheDocument();
  });

  it("dice cuántos lista cuando no los lista todos", async () => {
    respondWith({ dashboard: wireDashboard({ externalDenialsWithoutAlert: capped() }) });
    await renderDashboard();

    const panel = screen.getByRole("region", { name: /Denegados por el proveedor sin alerta local/ });
    expect(within(panel).getByText(/se listan los 2 más recientes/i)).toBeInTheDocument();
  });
});

/** El mismo panel con el total por encima de lo que lista. */
function capped(): unknown {
  const full = wireDashboard().externalDenialsWithoutAlert as { items: unknown[] };

  return { total: 51, listed: 2, items: full.items };
}
