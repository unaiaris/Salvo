import { fireEvent, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { ConsoleHeader } from "@/components/console-header";
import { SharedInstanceNotice } from "@/components/shared-instance-notice";
import type { Language } from "@/lib/api/contract";
import { accessibilityReport } from "@/test/axe";
import {
  jsonResponse,
  wireAlertDetail,
  wireAlertList,
  wireCapabilities,
  wireDashboard,
  wireEvaluationMetrics,
  wireSeedPreview,
} from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import AlertsPage from "@/app/alerts/page";
import AlertDetailPage from "@/app/alerts/[id]/page";
import DashboardPage from "@/app/dashboard/page";
import Home from "@/app/page";
import ImportPage from "@/app/import/page";
import { ActionOutcome } from "@/app/import/action-outcome";
import { INITIAL_ACTION_STATE } from "@/app/import/action-state";
import { ReviewForm } from "@/app/alerts/[id]/review-form";
import { INITIAL_REVIEW_STATE } from "@/app/alerts/[id]/review-state";

/**
 * La consola, medida por `axe-core` sobre el árbol que el navegador recibiría.
 *
 * <h3>Por qué cada pantalla se compone con su cabecera y su `<main>`</h3>
 *
 * Una página renderizada sola es un fragmento sin marco, y sobre un fragmento `axe-core` declara
 * **inaplicables** exactamente las reglas que más importan acá: `region`, `landmark-one-main`,
 * `landmark-unique`, `landmark-banner-is-top-level`, `page-has-heading-one` y `bypass`. Son las que
 * contestan si alguien puede saltar por regiones y encabezados en vez de recorrer la página entera
 * con la flecha, que es la forma en que un lector de pantalla se usa de verdad.
 *
 * Así que cada prueba reconstruye lo que `layout.tsx` pone dentro de `<body>`: la cabecera y el
 * `<main>` con la página adentro. Es una reproducción estructural, no visual — en jsdom no hay CSS
 * y no hace falta.
 *
 * Lo único del layout que queda afuera es `<html lang>`, que no puede vivir dentro del contenedor
 * de una prueba. No queda sin comprobar: `scripts/smoke-ui.sh` lo lee del HTML servido, en los dos
 * idiomas.
 *
 * <h3>El pie de esta comprobación</h3>
 *
 * Cero violaciones no es «la consola es accesible». Es «ninguna de las reglas que una máquina puede
 * decidir está rota». Lo que ninguna máquina contesta —si una analista que no ve la pantalla
 * entiende qué está por hacer antes de emitir un veredicto— lo contesta el recorrido con lector de
 * pantalla, y por eso la Etapa 9 lo pide aparte.
 */

const reviewAlert = vi.hoisted(() => vi.fn());

vi.mock("@/app/alerts/[id]/review-action", () => ({ reviewAlert }));

let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal("fetch", fetchMock);
  vi.stubEnv("SALVO_API_BASE_URL", "http://127.0.0.1:5100");
  reviewAlert.mockReset();
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
  vi.clearAllMocks();
});

/**
 * Lo que `layout.tsx` renderiza dentro de `<body>`, con la página adentro.
 *
 * `sharedInstance` reconstruye además el cartel de la instancia pública, que es una región más en
 * el árbol y por lo tanto algo que `region` y `landmark-unique` sí miran. Se pide en vez de estar
 * siempre porque las dos formas son despliegues reales y la de todos los días no lo lleva.
 */
async function consoleScreen(
  page: ReactNode | Promise<ReactNode>,
  language: Language = "es",
  { sharedInstance = false }: { sharedInstance?: boolean } = {},
): Promise<HTMLElement> {
  // La página se resuelve primero y el marco después. Anidar la promesa dentro del `<main>` no
  // compila: `ReactNode` admite una promesa, pero no una que a su vez resuelva a otra.
  const body = await renderableServerTree(page);
  const tree = await renderableServerTree(
    <>
      {sharedInstance && <SharedInstanceNotice language={language} resetMinutes={30} />}
      <ConsoleHeader language={language} isSharedInstance={sharedInstance} />
      <main>{body}</main>
    </>,
  );

  return render(tree).container;
}

/** Responde `capabilities` por su cuenta y todo lo demás con <paramref name="body" />. */
function respondWith(body: unknown, capabilities = wireCapabilities()) {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      url.pathname === "/api/system/capabilities"
        ? jsonResponse(capabilities)
        : jsonResponse(body),
    ),
  );
}

describe("la consola de la instancia compartida", () => {
  /**
   * El cartel agrega una región al marco, y una región que no está rotulada deja de ser navegable:
   * `region` y `landmark-unique` son justamente las reglas que un fragmento sin marco declara
   * inaplicables, así que medirlo aparte es la única forma de que cuenten.
   */
  it("el marco con el cartel, sobre la cola de alertas", async () => {
    respondWith(wireAlertList(), wireCapabilities({ sharedInstance: true, resetMinutes: 30 }));

    expect(
      await accessibilityReport(
        await consoleScreen(AlertsPage(), "es", { sharedInstance: true }),
      ),
    ).toBe("");
  });
});

describe("las cinco pantallas, con su marco", () => {
  it("la entrada", async () => {
    respondWith(wireCapabilities());

    expect(await accessibilityReport(await consoleScreen(Home()))).toBe("");
  });

  it("la cola de alertas", async () => {
    respondWith(wireAlertList());

    expect(await accessibilityReport(await consoleScreen(AlertsPage()))).toBe("");
  });

  it("la cola vacía, que es otro árbol", async () => {
    fetchMock.mockImplementation((url: URL) =>
      Promise.resolve(
        url.pathname === "/api/system/capabilities"
          ? jsonResponse(wireCapabilities())
          : url.pathname === "/api/orders"
            ? jsonResponse({ totalCount: 0 })
            : jsonResponse(
                wireAlertList({ items: [], totalCount: 0, scoringRunSequence: null, currentRun: null }),
              ),
      ),
    );

    expect(await accessibilityReport(await consoleScreen(AlertsPage()))).toBe("");
  });

  it("el detalle de una alerta, la pantalla más densa", async () => {
    respondWith(wireAlertDetail());

    const container = await consoleScreen(
      AlertDetailPage({ params: Promise.resolve({ id: "2f2b7f3e-0000-4000-8000-000000000002" }) }),
    );

    expect(await accessibilityReport(container)).toBe("");
  });

  it("el dashboard, con la sección de calidad", async () => {
    fetchMock.mockImplementation((url: URL) =>
      Promise.resolve(
        url.pathname === "/api/system/capabilities"
          ? jsonResponse(wireCapabilities())
          : url.pathname === "/api/evaluation-metrics"
            ? jsonResponse(wireEvaluationMetrics())
            : jsonResponse(wireDashboard()),
      ),
    );

    expect(await accessibilityReport(await consoleScreen(DashboardPage()))).toBe("");
  });

  it("la importación", async () => {
    fetchMock.mockImplementation((url: URL) =>
      Promise.resolve(
        url.pathname === "/api/system/capabilities"
          ? jsonResponse(wireCapabilities())
          : url.pathname === "/api/demo-data/seed-preview"
            ? jsonResponse(wireSeedPreview())
            : jsonResponse(wireDashboard()),
      ),
    );

    expect(await accessibilityReport(await consoleScreen(ImportPage()))).toBe("");
  });

  /**
   * El portugués recorre el mismo árbol con otras cadenas, y una cadena más larga no rompe una
   * estructura. Se comprueba igual sobre la pantalla más densa: `E9C1` movió noventa y siete
   * archivos y la forma de comprobar que un idioma no quedó a medias es mirarlo, no suponerlo.
   */
  it("el detalle, en el idioma que no es el de la demostración", async () => {
    respondWith(wireAlertDetail(), wireCapabilities({ language: "pt" }));

    const container = await consoleScreen(
      AlertDetailPage({ params: Promise.resolve({ id: "2f2b7f3e-0000-4000-8000-000000000002" }) }),
      "pt",
    );

    expect(await accessibilityReport(container)).toBe("");
  });
});

/**
 * Los estados que solo existen después de apretar un botón.
 *
 * Ninguna de las pruebas de arriba los alcanza, porque en el primer render no están. Y son los dos
 * momentos en que la pantalla cambia sola bajo alguien que no la ve, así que son los que el
 * recorrido con lector de pantalla mira con más atención.
 *
 * Acá se mide lo que una máquina puede decidir sobre ese árbol. **Que salga limpio no significa que
 * el resultado se anuncie**: si `axe-core` pudiera contestar eso, el recorrido humano no haría
 * falta. Lo que estas dos pruebas dejan fijo es que el árbol nuevo no introduce una violación
 * estructural, que es una condición necesaria y no la suficiente.
 */
describe("los estados que aparecen después de una acción", () => {
  it("el resultado de una importación con filas rechazadas", async () => {
    const { container } = render(
      <main>
        <ActionOutcome
          state={{
            ...INITIAL_ACTION_STATE,
            outcome: "done",
            title: "Se importaron 3 pedidos",
            body: "Importar escribe pedidos y no produce evaluaciones ni alertas.",
            recovery: "Ejecutá la corrida de scoring.",
            facts: ["Registros leídos: 6", "Rechazados: 3"],
            recordErrors: [
              "Registro 2, línea 3 · campo merchantReferenceId · Falta un campo obligatorio: value is empty",
              "Registro 4, línea 5 · campo currencyCode · El valor no es uno de los admitidos: DOBLONES",
            ],
            errorsTruncated: true,
            submissionId: 1,
          }}
          language="es"
        />
      </main>,
    );

    expect(await accessibilityReport(container)).toBe("");
  });

  /**
   * La interacción va por `fireEvent` y no por `user-event`, por el mismo motivo que la deja afuera
   * `review-form.test.tsx`: no es una dependencia de este proyecto y la etapa autoriza dos, ambas ya
   * usadas. El envío se despacha sobre el formulario porque jsdom no implementa el envío nativo.
   */
  it("el formulario de veredicto después de un rechazo de la API", async () => {
    reviewAlert.mockResolvedValue({
      ...INITIAL_REVIEW_STATE,
      outcome: "failed",
      title: "Otra persona ya revisó esta alerta",
      body: "La alerta quedó cerrada con un veredicto distinto.",
      recovery: "Recargá la alerta para ver el estado registrado.",
      technicalDetail: "Alert was already reviewed.",
      submittedStatus: "CONFIRMED_SAFE",
      submissionId: 1,
    });

    const { container } = render(
      <main>
        <ReviewForm
          alertId="a1"
          explanationId=""
          requiresAcknowledgement={false}
          divergenceSummary=""
          language="es"
        />
      </main>,
    );

    fireEvent.click(screen.getByRole("radio", { name: /Confirmar segura/i }));

    const form = screen.getByRole("button", { name: /Registrar veredicto/i }).closest("form");
    if (form === null) {
      throw new Error("El botón de envío no está dentro de un formulario.");
    }

    fireEvent.submit(form);
    await screen.findByText(/Otra persona ya revisó esta alerta/i);

    expect(await accessibilityReport(container)).toBe("");
  });
});
