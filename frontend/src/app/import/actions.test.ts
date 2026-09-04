import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  jsonResponse,
  problemResponse,
  wireImportResult,
  wireScoringRunSummary,
  wireSeedResult,
} from "@/test/fixtures";
import { INITIAL_ACTION_STATE } from "./action-state";
import {
  deliverAllCallbacks,
  executeScoringRun,
  importOrderFile,
  requestCorpusExternal,
  seedDemoCorpus,
} from "./actions";

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

function formWith(file: File | null, format = "CSV"): FormData {
  const form = new FormData();
  if (file !== null) {
    form.set("file", file, file.name);
  }
  form.set("format", format);

  return form;
}

const CSV_FILE = new File(["merchantId\nmerchant-demo\n"], "orders.csv", { type: "text/csv" });

describe("importación de un archivo", () => {
  it("lista un renglón por registro rechazado, traduciendo el código", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireImportResult()));

    const state = await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE));

    expect(state.outcome).toBe("done");
    expect(state.recordErrors).toHaveLength(2);
    expect(state.recordErrors[0]).toContain("Registro 4, línea 5");
    expect(state.recordErrors[0]).toContain("campo amountCents");
    expect(state.recordErrors[0]).toContain("El valor está fuera del rango admitido");
    // Un registro sin línea ni campo no inventa ninguno de los dos.
    expect(state.recordErrors[1]).toBe(
      "Registro 11 · La referencia ya existe con otros datos: "
      + "The merchant reference already exists with different data.",
    );
  });

  it("avisa cuando la API dejó de enumerar errores", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireImportResult({ errorsTruncated: true })));

    const state = await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE));

    expect(state.errorsTruncated).toBe(true);
  });

  it("no marca truncado un resultado completo", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireImportResult()));

    expect((await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE))).errorsTruncated).toBe(
      false,
    );
  });

  it("informa los cuatro totales de la importación", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireImportResult()));

    const state = await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE));

    expect(state.facts).toEqual([
      "Registros leídos: 12",
      "Importados: 9",
      "Duplicados, ya presentes con los mismos datos: 1",
      "Rechazados: 2",
    ]);
  });

  it("dice que los pedidos importados todavía no tienen evaluación", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireImportResult()));

    const state = await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE));

    expect(state.recovery).toMatch(/corrida de scoring/i);
  });

  it("rechaza un envío sin archivo sin llegar a llamar a la API", async () => {
    const state = await importOrderFile(INITIAL_ACTION_STATE, formWith(null));

    expect(state.outcome).toBe("failed");
    expect(state.title).toBe("No llegó ningún archivo");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("rechaza un formato que el contrato no admite", async () => {
    const state = await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE, "XLSX"));

    expect(state.title).toBe("Ese formato no está soportado");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("envía el archivo como multipart, sin fijar el Content-Type a mano", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireImportResult()));

    await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE));

    const [url, init] = fetchMock.mock.calls[0] as [URL, RequestInit];
    expect(url.pathname).toBe("/api/order-imports");
    expect(init.body).toBeInstanceOf(FormData);
    // `fetch` tiene que poner el Content-Type con su propio boundary: fijarlo acá lo rompería.
    expect(init.headers).toBeUndefined();
  });

  it("traduce cada rechazo de transporte a su propio mensaje", async () => {
    const cases = [
      ["FILE_TOO_LARGE", 413, "El archivo supera el máximo admitido"],
      ["TOO_MANY_RECORDS", 413, "El archivo tiene demasiados registros"],
      ["UNSUPPORTED_FORMAT", 415, "Ese formato no está soportado"],
      ["EMPTY_FILE", 400, "El archivo no tiene ningún pedido"],
      ["INVALID_CSV", 400, "El CSV está mal formado"],
      ["UNKNOWN_HEADER", 400, "El CSV trae una columna que la importación no conoce"],
    ] as const;

    for (const [code, status, title] of cases) {
      fetchMock.mockResolvedValue(problemResponse(status, code, "detalle de la API"));

      const state = await importOrderFile(INITIAL_ACTION_STATE, formWith(CSV_FILE));

      expect(state.title, code).toBe(title);
      expect(state.technicalDetail, code).toBe("detalle de la API");
    }
  });
});

describe("corrida de scoring", () => {
  it("presenta la secuencia y las seis cifras del resumen", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireScoringRunSummary()));

    const state = await executeScoringRun(INITIAL_ACTION_STATE);

    expect(state.title).toBe("Corrida #4 completada");
    expect(state.facts).toContain("Pedidos evaluados: 300");
    expect(state.facts).toContain("Evaluaciones creadas: 12");
    expect(state.facts).toContain("Evaluaciones reusadas: 288");
    expect(state.facts).toContain("Alertas abiertas: 2");
    expect(state.facts).toContain("Omitidas por tener ya una alerta abierta: 16");
    expect(state.facts).toContain("Omitidas por tener ya un veredicto: 0");
  });

  it("da su propio mensaje a dos corridas simultáneas", async () => {
    fetchMock.mockResolvedValue(
      problemResponse(409, "SCORING_RUN_CONFLICT", "A concurrent scoring run already persisted."),
    );

    const state = await executeScoringRun(INITIAL_ACTION_STATE);

    expect(state.outcome).toBe("failed");
    expect(state.title).toBe("Otra corrida de scoring se ejecutó al mismo tiempo");
    expect(state.recovery).toMatch(/volvé a ejecutar/i);
  });

  it("cuenta cada intento, para poder anunciar dos resultados iguales seguidos", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireScoringRunSummary()));

    const first = await executeScoringRun(INITIAL_ACTION_STATE);
    const second = await executeScoringRun(first);

    expect(second.submissionId).toBe(first.submissionId + 1);
  });
});

describe("corpus de demostración", () => {
  it("distingue una carga real de una repetición idempotente", async () => {
    fetchMock.mockResolvedValue(jsonResponse(wireSeedResult()));
    const loaded = await seedDemoCorpus(INITIAL_ACTION_STATE);

    fetchMock.mockResolvedValue(
      jsonResponse(wireSeedResult({ insertedOrders: 0, duplicateOrders: 300, insertedLabels: 0 })),
    );
    const again = await seedDemoCorpus(loaded);

    expect(loaded.title).toBe("Corpus cargado");
    expect(again.title).toBe("El corpus de demostración ya estaba cargado");
    expect(again.body).toMatch(/idempotente/i);
  });

  it("explica un choque con pedidos ya existentes", async () => {
    fetchMock.mockResolvedValue(
      problemResponse(409, "DEMO_DATA_CONFLICT", "The demo dataset conflicts."),
    );

    const state = await seedDemoCorpus(INITIAL_ACTION_STATE);

    expect(state.title).toBe("El corpus de demostración choca con pedidos que ya existen");
  });
});

/**
 * The corpus-wide external actions of the demo surface.
 *
 * They are how the orders that never produced an alert get a second opinion at all: the alert detail
 * only reaches the ones that did, and stage 6 deliberately adds no orders screen.
 */
describe("evaluación externa del corpus", () => {
  it("cuenta lo consultado y separa lo que sigue esperando al proveedor", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ examined: 300, requested: 300, settled: 279, stillPending: 21, skipped: 0 }),
    );

    const state = await requestCorpusExternal(INITIAL_ACTION_STATE);

    expect(state.outcome).toBe("done");
    expect(state.title).toContain("300");
    expect(state.facts).toContain("Esperando al proveedor: 21");
    expect(state.recovery).toContain("entregando sus callbacks");
  });

  it("dice que no había nada nuevo cuando el proveedor ya conocía el corpus", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ examined: 0, requested: 0, settled: 0, stillPending: 0, skipped: 0 }),
    );

    const state = await requestCorpusExternal(INITIAL_ACTION_STATE);

    expect(state.outcome).toBe("done");
    expect(state.title).toMatch(/ya conocía todos los pedidos/i);
  });

  it("traduce el conflicto de una evaluación que ya espera al proveedor", async () => {
    fetchMock.mockResolvedValue(problemResponse(
        409,
        "EXTERNAL_EVALUATION_PENDING",
        "Order ... already has an external evaluation waiting for the provider.",
      ));

    const state = await requestCorpusExternal(INITIAL_ACTION_STATE);

    expect(state.outcome).toBe("failed");
    expect(state.title).toMatch(/esperando al proveedor/i);
    expect(state.title).not.toMatch(/409/);
  });
});

describe("entrega de callbacks", () => {
  it("dice qué se entregó y qué se cerró", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ examined: 21, delivered: 21, settled: 21, replayed: 0, unavailable: 0 }),
    );

    const state = await deliverAllCallbacks(INITIAL_ACTION_STATE);

    expect(state.outcome).toBe("done");
    expect(state.title).toContain("21");
    expect(state.body).toMatch(/nunca qué responde el proveedor/);
    expect(state.facts).toContain("Cerraron con veredicto: 21");
  });

  /**
   * Repeating the delivery is meant to be harmless, and the wording has to say that rather than
   * report a second success that never happened.
   */
  it("nombra una reentrega como reentrega y no como un efecto nuevo", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ examined: 21, delivered: 21, settled: 0, replayed: 21, unavailable: 0 }),
    );

    const state = await deliverAllCallbacks(INITIAL_ACTION_STATE);

    expect(state.title).toMatch(/ya se habían recibido/i);
    expect(state.body).toMatch(/no se repitió ningún efecto/);
  });

  it("dice qué hacer cuando no hay nada esperando al proveedor", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({ examined: 0, delivered: 0, settled: 0, replayed: 0, unavailable: 0 }),
    );

    const state = await deliverAllCallbacks(INITIAL_ACTION_STATE);

    expect(state.title).toMatch(/No hay ninguna evaluación externa esperando al proveedor/);
    expect(state.recovery).toMatch(/Solicitá evaluaciones externas/);
  });
});
