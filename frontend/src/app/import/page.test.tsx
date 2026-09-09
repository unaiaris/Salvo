import { cleanup, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, wireCapabilities, wireDashboard, wireSeedPreview } from "@/test/fixtures";
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

/**
 * Answers per path, so a test can say what the deployment declares without ordering its mocks.
 *
 * `demoSeedEnabled` follows `demoDataEnabled` unless a test separates them, because that is the
 * implication the API enforces: the seed switch narrows and never widens. A mock where the seed is
 * on while the deployment does not declare itself a demonstration is a response no API can send,
 * and a screen tested against it would be tested against nothing.
 */
function respondWith({
  demoDataEnabled = true,
  demoSeedEnabled = demoDataEnabled,
  dashboard = wireDashboard(),
  seedPreview = wireSeedPreview(),
  language = "es",
}: {
  demoDataEnabled?: boolean;
  demoSeedEnabled?: boolean;
  dashboard?: unknown;
  seedPreview?: unknown;
  language?: "es" | "pt";
} = {}) {
  fetchMock.mockImplementation((url: URL) => {
    switch (url.pathname) {
      case "/api/system/capabilities":
        return Promise.resolve(
          jsonResponse(wireCapabilities({ demoDataEnabled, demoSeedEnabled, language })),
        );
      case "/api/demo-data/seed-preview":
        return Promise.resolve(jsonResponse(seedPreview));
      default:
        return Promise.resolve(jsonResponse(dashboard));
    }
  });
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

  it("esconde el corpus de demostración y conserva el resto cuando solo el sembrado está apagado", async () => {
    // Lo que la instancia pública declara: su corpus viene horneado en la imagen, así que no
    // registra la ruta de sembrado, y todo lo demás sigue en pie.
    respondWith({ demoDataEnabled: true, demoSeedEnabled: false });
    await renderImport();

    expect(
      screen.queryByRole("button", { name: "Cargar corpus de demostración" }),
    ).not.toBeInTheDocument();
    expect(requestedPaths()).not.toContain("/api/demo-data/seed-preview");
    expect(screen.getByRole("button", { name: "Importar pedidos" })).toBeInTheDocument();
    expect(screen.getByText("Proveedor antifraude externo")).toBeInTheDocument();
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

/**
 * Mismo motivo que en el dashboard: el `id` del encabezado salía del título traducido, y en
 * portugués `ção` colapsa a `-o`. Acá las cuatro secciones están todas en pantalla a la vez, así que
 * una colisión rotularía una sección con el título de otra sin que nada más se rompa.
 */
describe("los identificadores de las secciones", () => {
  const IDS = ["section-seed", "section-file", "section-scoring", "section-external"] as const;

  it("no cambian con el idioma del despliegue", async () => {
    respondWith();
    const { container } = render(await renderableServerTree(ImportPage()));

    for (const id of IDS) {
      expect(container.querySelector(`#${id}`), id).not.toBeNull();
      expect(container.querySelector(`[aria-labelledby="${id}"]`), id).not.toBeNull();
    }

    cleanup();

    respondWith({ language: "pt" });
    const portuguese = render(await renderableServerTree(ImportPage())).container;

    for (const id of IDS) {
      expect(portuguese.querySelector(`#${id}`), id).not.toBeNull();
    }

    expect(portuguese.textContent).toMatch(/Importação e scoring/);
  });
});

describe("el ensayo del corpus, antes del clic", () => {
  it("no dice nada cuando la base puede tomar el corpus", async () => {
    respondWith();
    await renderImport();

    expect(screen.queryByText(/versión anterior del corpus/i)).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Cargar corpus de demostración" })).toBeInTheDocument();
  });

  it("avisa que la base tiene la versión anterior, y dice que hace falta una base nueva", async () => {
    respondWith({ seedPreview: wireSeedPreview({ conflict: "PREVIOUS_CORPUS", ordersToInsert: 0 }) });
    await renderImport();

    const notice = screen.getByRole("status", { name: /versión anterior del corpus/i });
    expect(within(notice).getByText(/exige una base nueva/i)).toBeInTheDocument();
    // Y dice lo que le pasa a la base que ya está, porque es la pregunta siguiente.
    expect(within(notice).getByText(/no se toca ni se pierde/i)).toBeInTheDocument();
  });

  it("distingue el otro conflicto: pedidos importados con las mismas referencias", async () => {
    respondWith({ seedPreview: wireSeedPreview({ conflict: "IMPORTED_ORDERS" }) });
    await renderImport();

    expect(screen.getByRole("status", { name: /pedidos importados/i })).toBeInTheDocument();
    expect(screen.queryByText(/versión anterior del corpus/i)).not.toBeInTheDocument();
  });

  it("no pide el ensayo cuando la instancia no se declara de demostración", async () => {
    respondWith({ demoDataEnabled: false });
    await renderImport();

    expect(requestedPaths()).not.toContain("/api/demo-data/seed-preview");
  });
});
