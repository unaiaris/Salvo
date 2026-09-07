import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, problemResponse, wireAlertDetail, wireAlertList,
  mockConsoleFetch,
} from "@/test/fixtures";
import { fetchAlert, fetchOpenAlerts, submitAlertReview } from "./alerts";
import { requestJson } from "./server-client";
import { describeFailure } from "./messages";

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

function requestOf(call: number): { url: URL; init: RequestInit } {
  const [url, init] = fetchMock.mock.calls[call] as [URL, RequestInit];

  return { url, init };
}

describe("el cliente de la API", () => {
  it("pide URLs absolutas, no rutas relativas al rewrite", async () => {
    // El rewrite de next.config.ts solo existe para el navegador; en Node una ruta relativa es un
    // TypeError, así que la URL absoluta no es una preferencia de estilo.
    mockConsoleFetch(fetchMock, () => jsonResponse(wireAlertList()));

    await fetchOpenAlerts();

    const { url } = requestOf(0);
    expect(url.origin).toBe("http://127.0.0.1:5100");
    expect(url.pathname).toBe("/api/alerts");
  });

  it("pide una sola página de 200 alertas abiertas por score vigente descendente", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse(wireAlertList()));

    await fetchOpenAlerts();

    const { url } = requestOf(0);
    expect(url.searchParams.get("status")).toBe("OPEN");
    expect(url.searchParams.get("sort")).toBe("SCORE_DESC");
    expect(url.searchParams.get("pageSize")).toBe("200");
    expect(url.searchParams.get("page")).toBeNull();
  });

  it("fija un timeout explícito en toda llamada", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse(wireAlertDetail()));

    await fetchAlert("2f2b7f3e-0000-4000-8000-000000000002");
    await submitAlertReview("2f2b7f3e-0000-4000-8000-000000000002", {
      newStatus: "CONFIRMED_SAFE",
      note: null,
      acknowledgedDivergence: false,
      explanationId: null,
    });

    for (const call of [0, 1]) {
      expect(requestOf(call).init.signal).toBeInstanceOf(AbortSignal);
    }
  });

  it("no cachea: una revisión tiene que verse en la lectura siguiente", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse(wireAlertList()));

    await fetchOpenAlerts();

    expect(requestOf(0).init.cache).toBe("no-store");
  });

  it("convierte un timeout en un estado de error, no en una excepción sin manejar", async () => {
    fetchMock.mockRejectedValue(
      Object.assign(new DOMException("The operation timed out.", "TimeoutError")),
    );

    const result = await fetchOpenAlerts();

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.failure.kind).toBe("timeout");
      expect(describeFailure(result.failure, "es").title).toMatch(/tardó demasiado/i);
    }
  });

  it("aborta de verdad cuando la API no responde a tiempo", async () => {
    // No basta con mapear un error simulado: esto ejercita el AbortSignal real, comprueba que la
    // petición se corta sola y que el corte llega como estado de error y no como excepción.
    fetchMock.mockImplementation(
      (_url: URL, init: RequestInit) =>
        new Promise((_resolve, reject) => {
          init.signal?.addEventListener("abort", () => {
            reject(init.signal?.reason as Error);
          });
        }),
    );

    const result = await requestJson({ path: "/api/alerts", timeoutMs: 20 });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.failure.kind).toBe("timeout");
    }
  });

  it("distingue una API inalcanzable de una que tardó", async () => {
    fetchMock.mockRejectedValue(new TypeError("fetch failed"));

    const result = await fetchOpenAlerts();

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.failure.kind).toBe("unreachable");
    }
  });

  it("lee el código de problem+json, que es una extensión fuera del schema", async () => {
    mockConsoleFetch(fetchMock, () => 
      problemResponse(409, "ALERT_ALREADY_REVIEWED", "Alert 2f2b… was already reviewed."),
    );

    const result = await submitAlertReview("2f2b7f3e-0000-4000-8000-000000000002", {
      newStatus: "CONFIRMED_SAFE",
      note: null,
      acknowledgedDivergence: false,
      explanationId: null,
    });

    expect(result.ok).toBe(false);
    if (!result.ok && result.failure.kind === "problem") {
      expect(result.failure.status).toBe(409);
      expect(result.failure.code).toBe("ALERT_ALREADY_REVIEWED");
      expect(result.failure.detail).toContain("already reviewed");
    }
  });

  it("tolera un error sin cuerpo legible", async () => {
    mockConsoleFetch(fetchMock, () => new Response("<html>502</html>", { status: 502 }));

    const result = await fetchOpenAlerts();

    expect(result.ok).toBe(false);
    if (!result.ok && result.failure.kind === "problem") {
      expect(result.failure.status).toBe(502);
      expect(result.failure.code).toBeNull();
    }
  });

  it("rechaza una respuesta que no respeta el contrato en vez de renderizarla a medias", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse({ items: "no es una lista" }));

    const result = await fetchOpenAlerts();

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.failure.kind).toBe("malformed");
    }
  });

  it("envía la revisión como JSON con los cuatro campos del contrato", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse({ applied: true, alert: wireAlertDetail() }));

    await submitAlertReview("2f2b7f3e-0000-4000-8000-000000000002", {
      newStatus: "REPORTED_FRAUD",
      note: "Coincide con el patrón de la semana pasada.",
      acknowledgedDivergence: true,
      explanationId: "6f6b7f3e-0000-4000-8000-000000000006",
    });

    const { url, init } = requestOf(0);
    expect(init.method).toBe("POST");
    expect(url.pathname).toBe("/api/alerts/2f2b7f3e-0000-4000-8000-000000000002/review");
    expect(JSON.parse(String(init.body))).toEqual({
      newStatus: "REPORTED_FRAUD",
      note: "Coincide con el patrón de la semana pasada.",
      acknowledgedDivergence: true,
      explanationId: "6f6b7f3e-0000-4000-8000-000000000006",
    });
  });

  it("manda la nota vacía como null, no como cadena vacía", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse({ applied: true, alert: wireAlertDetail() }));

    await submitAlertReview("2f2b7f3e-0000-4000-8000-000000000002", {
      newStatus: "CONFIRMED_SAFE",
      note: null,
      acknowledgedDivergence: false,
      explanationId: null,
    });

    expect(JSON.parse(String(requestOf(0).init.body)).note).toBeNull();
  });
});
