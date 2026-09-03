import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, wireCapabilities, wireDashboard } from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import ImportPage from "./page";

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

/** Answers per path, so a test can say what the deployment declares without ordering its mocks. */
function respondWith({
  demoDataEnabled = true,
  dashboard = wireDashboard(),
}: {
  demoDataEnabled?: boolean;
  dashboard?: unknown;
} = {}) {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      url.pathname === "/api/system/capabilities"
        ? jsonResponse(wireCapabilities({ demoDataEnabled }))
        : jsonResponse(dashboard),
    ),
  );
}

async function renderImport() {
  render(await renderableServerTree(ImportPage()));
}

function requestedPaths(): readonly string[] {
  return fetchMock.mock.calls.map(([url]) => (url as URL).pathname);
}

describe("pantalla de importación", () => {
  it("ofrece importar y ejecutar la corrida, y dice que importar no procesa", async () => {
    respondWith();
    await renderImport();

    expect(screen.getByRole("button", { name: "Importar pedidos" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Ejecutar scoring" })).toBeInTheDocument();
    expect(screen.getByText(/Importar escribe pedidos y nada más/)).toBeInTheDocument();
  });

  it("declara de qué corrida es el estado que muestra", async () => {
    respondWith();
    await renderImport();

    expect(screen.getByText(/Vigente desde la corrida #3/)).toBeInTheDocument();
  });

  it("avisa cuántos pedidos quedaron sin puntuar y qué implica", async () => {
    respondWith({ dashboard: wireDashboard({ ordersPendingScoring: 42 }) });
    await renderImport();

    expect(screen.getByText(/Hay 42 pedidos sin puntuar/)).toBeInTheDocument();
    expect(screen.getByText(/No aparecen en el dashboard/)).toBeInTheDocument();
  });

  it("no avisa de pendientes cuando la corrida cubre todo el corpus", async () => {
    respondWith();
    await renderImport();

    expect(screen.queryByText(/sin puntuar/)).not.toBeInTheDocument();
    expect(screen.getByText(/Todos los pedidos de la base están cubiertos/)).toBeInTheDocument();
  });

  it("ofrece el corpus de demostración cuando la API declara que existe", async () => {
    respondWith({ demoDataEnabled: true });
    await renderImport();

    expect(
      screen.getByRole("button", { name: "Cargar corpus de demostración" }),
    ).toBeInTheDocument();
  });

  it("no ofrece el corpus de demostración cuando la API dice que no existe", async () => {
    respondWith({ demoDataEnabled: false });
    await renderImport();

    expect(
      screen.queryByRole("button", { name: "Cargar corpus de demostración" }),
    ).not.toBeInTheDocument();
    // El resto de la pantalla no depende de la demo y sigue entero.
    expect(screen.getByRole("button", { name: "Importar pedidos" })).toBeInTheDocument();
  });

  it("no pregunta por las capacidades más de una vez ni consulta rutas de demo", async () => {
    respondWith({ demoDataEnabled: false });
    await renderImport();

    expect(requestedPaths()).toEqual(["/api/system/capabilities", "/api/dashboard"]);
  });

  it("dice el límite real de tamaño del archivo", async () => {
    respondWith();
    await renderImport();

    expect(screen.getByText(/Hasta 5 MiB y 10\.000 pedidos/)).toBeInTheDocument();
  });

  it("explica en castellano una API caída, sin dejar la pantalla en blanco", async () => {
    fetchMock.mockRejectedValue(new TypeError("fetch failed"));
    await renderImport();

    expect(screen.getAllByText("No se pudo contactar a la API").length).toBeGreaterThan(0);
    // La importación no depende de poder leer el estado, así que el formulario sigue disponible.
    expect(screen.getByRole("button", { name: "Importar pedidos" })).toBeInTheDocument();
  });
});
