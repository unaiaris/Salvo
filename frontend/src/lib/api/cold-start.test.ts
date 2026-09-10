import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, wireAlertList } from "@/test/fixtures";
import { requestJson } from "./server-client";

/**
 * El reintento por arranque en frío de la instancia compartida.
 *
 * <h3>Qué se está probando de verdad</h3>
 *
 * Que la consola espere a una API que todavía no arrancó **solo** en la instancia pública, **solo**
 * en lecturas y **solo** ante fallos de transporte. Las tres condiciones importan por separado y
 * cada una tiene su caso: quitar cualquiera de ellas produce un comportamiento que en algún entorno
 * está mal —una avería silenciada en desarrollo, una importación aplicada dos veces, o una espera
 * inútil contra una API que ya contestó lo que tenía que contestar—.
 *
 * <h3>Por qué los tiempos son falsos</h3>
 *
 * El presupuesto real se cuenta en decenas de segundos, y una prueba que los espere de verdad
 * convierte la suite en algo que nadie corre. Los temporizadores son de mentira y el reloj lo mueve
 * el test, así que lo que se mide es la **lógica** de la espera y no su duración.
 */

let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal("fetch", fetchMock);
  vi.stubEnv("SALVO_API_BASE_URL", "http://127.0.0.1:5100");
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

/** Un fallo de transporte: `fetch` rechaza, que es lo que hace una API que no está escuchando. */
function refuses(): Promise<never> {
  return Promise.reject(new TypeError("fetch failed"));
}

/**
 * Corre una petición dejando que los temporizadores falsos avancen solos.
 *
 * `requestJson` espera entre intento e intento, y con temporizadores falsos esa espera no termina
 * salvo que alguien mueva el reloj. `advanceTimersByTimeAsync` lo mueve en trozos, cediendo el
 * control entre uno y otro para que las promesas pendientes se resuelvan.
 */
async function runWithClock<T>(work: Promise<T>): Promise<T> {
  const settled = work.then((value) => ({ value }));

  for (let i = 0; i < 200; i += 1) {
    await vi.advanceTimersByTimeAsync(1_000);
  }

  return (await settled).value;
}

describe("la espera del arranque en frío", () => {
  it("en la instancia compartida, reintenta una lectura hasta que la API contesta", async () => {
    vi.stubEnv("SharedInstance__Enabled", "true");
    fetchMock
      .mockImplementationOnce(refuses)
      .mockImplementationOnce(refuses)
      .mockImplementationOnce(() => Promise.resolve(jsonResponse(wireAlertList())));

    const result = await runWithClock(requestJson({ path: "/api/alerts" }));

    expect(result.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it("fuera de la instancia compartida, un fallo de transporte se reporta en el acto", async () => {
    // El escenario 3 de `smoke-ui.sh` apaga la API a propósito y espera ver el mensaje de fallo.
    // Si esta condición se cayera, ese escenario esperaría medio minuto y después mostraría lo
    // mismo, y una consola de desarrollo escondería una API caída detrás de una espera larga.
    vi.stubEnv("SharedInstance__Enabled", "");
    fetchMock.mockImplementation(refuses);

    const result = await runWithClock(requestJson({ path: "/api/alerts" }));

    expect(result.ok).toBe(false);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("no reintenta una escritura, ni siquiera en la instancia compartida", async () => {
    // `AGENTS.md`: retries solo donde sean semánticamente seguros. Una importación o un veredicto
    // pueden haberse aplicado con la respuesta perdida en el camino.
    vi.stubEnv("SharedInstance__Enabled", "true");
    fetchMock.mockImplementation(refuses);

    const result = await runWithClock(
      requestJson({ path: "/api/demo-data/seed", method: "POST" }),
    );

    expect(result.ok).toBe(false);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("no reintenta cuando la API contestó un problema", async () => {
    // Un 409 es la API funcionando y diciendo que no. Volver a pedirlo da 409 otra vez.
    vi.stubEnv("SharedInstance__Enabled", "true");
    fetchMock.mockImplementation(() =>
      Promise.resolve(
        new Response(JSON.stringify({ code: "ORDER_LIMIT_REACHED" }), {
          status: 409,
          headers: { "content-type": "application/problem+json" },
        }),
      ),
    );

    const result = await runWithClock(requestJson({ path: "/api/alerts" }));

    expect(result.ok).toBe(false);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("se rinde al agotar el presupuesto, y devuelve un fallo de transporte", async () => {
    vi.stubEnv("SharedInstance__Enabled", "true");
    fetchMock.mockImplementation(refuses);

    const result = await runWithClock(requestJson({ path: "/api/alerts" }));

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.failure.kind).toBe("unreachable");
    }
    // Reintentó: no se quedó en el primer intento ni se colgó para siempre.
    expect(fetchMock.mock.calls.length).toBeGreaterThan(1);
  });
});
