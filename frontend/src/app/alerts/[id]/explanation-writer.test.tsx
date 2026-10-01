import { render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  jsonResponse,
  wireAlertDetail,
  wireCapabilities,
  wireExplanation,
  wireExplanationAttempt,
} from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import { explainEvaluation } from "./explanation-action";
import { INITIAL_EXPLANATION_STATE } from "./explanation-state";
import AlertDetailPage from "./page";

const ALERT_ID = "2f2b7f3e-0000-4000-8000-000000000002";

/** A text the template wrote, with the template as today's writer. */
const BY_TEMPLATE = { provider: "MOCK", templateVersion: "e7-v1", currentWriterProvider: "MOCK" };

/** A text a model wrote, with the model as today's writer. */
const BY_MODEL = {
  provider: "ANTHROPIC",
  templateVersion: "anthropic-p1",
  providerVersion: "claude-sonnet-5-5",
  currentWriterProvider: "ANTHROPIC",
};

/** A text of the template, while today's writer is the model: cases 2, 3 and 5. */
const TEMPLATE_UNDER_MODEL = {
  provider: "MOCK",
  templateVersion: "e7-v2",
  writtenByAnotherTemplate: true,
  currentWriterProvider: "ANTHROPIC",
};

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

async function renderDetail(explanation: Record<string, unknown> | null, language = "es") {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      jsonResponse(
        url.pathname === "/api/system/capabilities"
          ? wireCapabilities({ language })
          : wireAlertDetail({ explanation }),
      ),
    ),
  );

  return render(
    await renderableServerTree(AlertDetailPage({ params: Promise.resolve({ id: ALERT_ID }) })),
  );
}

function block() {
  return screen.getByRole("region", { name: /^(Explicación|Explicação)$/i });
}

function failed(overrides: Record<string, unknown> = {}) {
  return {
    status: "FAILED",
    summary: null,
    referencedRules: [],
    failureCode: "PROVIDER_REFUSED",
    failureDetail: "cyber; req_011CSHoEeqs5C35K2UUqR7Fy",
    ...overrides,
  };
}

/**
 * What the button the page shows would send, read off the form the page rendered and handed to the
 * real action — or `null` when there is no button. The action posts to a stubbed API, so what this
 * returns is the request the case builds, never the API's answer to it: that half is the backend's,
 * in `ExplanationSelectionTests`.
 */
async function requestTheButtonBuilds(): Promise<{ label: string; regenerate: unknown } | null> {
  const button = within(block()).queryByRole("button");
  if (button === null) {
    return null;
  }

  const form = button.closest("form");
  expect(form).not.toBeNull();

  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      jsonResponse(
        url.pathname === "/api/system/capabilities"
          ? wireCapabilities()
          : { applied: true, explanation: wireExplanation(BY_MODEL) },
      ),
    ),
  );
  fetchMock.mockClear();
  await explainEvaluation(INITIAL_EXPLANATION_STATE, new FormData(form as HTMLFormElement));

  const posted = fetchMock.mock.calls.filter(
    ([url]) => (url as URL).pathname !== "/api/system/capabilities",
  );
  const [, init] = posted[0] as [URL, RequestInit];

  return {
    label: button.textContent ?? "",
    regenerate: (JSON.parse(String(init.body)) as Record<string, unknown>).regenerate,
  };
}

describe("quién escribió qué (decisión 79)", () => {
  it("la oración de la plantilla no cambia una letra, en los dos idiomas", async () => {
    const { unmount } = await renderDetail(wireExplanation(BY_TEMPLATE));
    expect(block()).toHaveTextContent(
      "Redactada por una plantilla determinista (e7-v1), no por un modelo, el",
    );
    unmount();

    await renderDetail(wireExplanation(BY_TEMPLATE), "pt");
    expect(block()).toHaveTextContent(
      "Redigida por um modelo determinístico (e7-v1), não por um modelo de linguagem, em",
    );
  });

  it("la oración del modelo nombra el modelo y la versión del prompt, y no dice «no por un modelo»", async () => {
    const { unmount } = await renderDetail(wireExplanation(BY_MODEL));
    expect(block()).toHaveTextContent(
      "Redactada por el modelo claude-sonnet-5-5 de Anthropic (prompt anthropic-p1), el",
    );
    expect(block().textContent).not.toMatch(/no por un modelo/i);
    unmount();

    await renderDetail(wireExplanation(BY_MODEL), "pt");
    expect(block()).toHaveTextContent(
      "Redigida pelo modelo claude-sonnet-5-5 da Anthropic (prompt anthropic-p1), em",
    );
    expect(block().textContent).not.toMatch(/não por um modelo/i);
  });

  it("sin el nombre del modelo en la respuesta, dice que fue un modelo de Anthropic", async () => {
    await renderDetail(wireExplanation({ ...BY_MODEL, providerVersion: null }));

    expect(block()).toHaveTextContent("Redactada por un modelo de Anthropic (prompt anthropic-p1)");
  });

  it("el botón con la plantilla vigente dice exactamente lo que decía", async () => {
    const { unmount } = await renderDetail(
      wireExplanation({ ...BY_TEMPLATE, writtenByAnotherTemplate: true }),
    );
    expect(within(block()).getByRole("button")).toHaveTextContent(/^Redactar con la plantilla vigente$/);
    unmount();

    await renderDetail(wireExplanation({ ...BY_TEMPLATE, writtenByAnotherTemplate: true }), "pt");
    expect(within(block()).getByRole("button")).toHaveTextContent(/^Redigir com o modelo vigente$/);
  });

  /**
   * The button names the writer and never the model: the name comes from an answer, and the button
   * appears before there is one.
   */
  it("el botón con el modelo vigente dice «modelo de Anthropic», sin el nombre del modelo", async () => {
    const { unmount } = await renderDetail(wireExplanation(TEMPLATE_UNDER_MODEL));
    const button = within(block()).getByRole("button");
    expect(button).toHaveTextContent(/^Redactar con el modelo de Anthropic$/);
    expect(button.textContent).not.toMatch(/claude/i);
    unmount();

    await renderDetail(wireExplanation(TEMPLATE_UNDER_MODEL), "pt");
    expect(within(block()).getByRole("button")).toHaveTextContent(/^Redigir com o modelo da Anthropic$/);
  });

  it("el título después del botón: el de la plantilla no cambia, el del modelo nombra el modelo", async () => {
    const form = new FormData();
    form.set("alertId", ALERT_ID);
    form.set("ask", "currentTemplate");

    for (const [language, explanation, title] of [
      ["es", wireExplanation(BY_TEMPLATE), "Redactada de nuevo con la plantilla vigente"],
      ["pt", wireExplanation(BY_TEMPLATE), "Redigida de novo com o modelo vigente"],
      ["es", wireExplanation(BY_MODEL), "Redactada con el modelo claude-sonnet-5-5 de Anthropic"],
      ["pt", wireExplanation(BY_MODEL), "Redigida com o modelo claude-sonnet-5-5 da Anthropic"],
    ] as const) {
      fetchMock.mockImplementation((url: URL) =>
        Promise.resolve(
          jsonResponse(
            url.pathname === "/api/system/capabilities"
              ? wireCapabilities({ language })
              : { applied: true, explanation },
          ),
        ),
      );

      const state = await explainEvaluation(INITIAL_EXPLANATION_STATE, form);

      expect(state.title, `${language} ${String(explanation.provider)}`).toBe(title);
    }
  });

  it("un fallo muestra su detalle técnico", async () => {
    await renderDetail(wireExplanation({ ...BY_MODEL, ...failed() }));

    expect(block()).toHaveTextContent("Detalle técnico: cyber; req_011CSHoEeqs5C35K2UUqR7Fy.");
  });
});

/**
 * The frontend half of the button: for the view of each case, which request it builds — with or
 * without `regenerate` — or that there is none.
 */
describe("qué pide el botón en cada caso", () => {
  it("caso 1: la explicación del escritor vigente no ofrece nada", async () => {
    await renderDetail(wireExplanation(BY_MODEL));

    expect(await requestTheButtonBuilds()).toBeNull();
  });

  /**
   * The paragraph of the template stays, the failed attempt of the model is shown beside it with its
   * code and its detail, and the button retakes that attempt.
   */
  it("caso 2: el texto de otro sigue en pantalla, el intento fallido al lado, y el botón lo reintenta", async () => {
    await renderDetail(
      wireExplanation({ ...TEMPLATE_UNDER_MODEL, currentWriterAttempt: wireExplanationAttempt() }),
    );

    const shown = block();
    expect(shown).toHaveTextContent("El pedido obtuvo 100 puntos sobre un umbral de 60.");
    expect(shown).toHaveTextContent("Último intento con el modelo de Anthropic");
    expect(shown).toHaveTextContent(/El proveedor respondió sin texto/);
    expect(shown).toHaveTextContent("Detalle técnico: cyber; req_011CSHoEeqs5C35K2UUqR7Fy.");

    expect(await requestTheButtonBuilds()).toEqual({
      label: "Volver a intentar con el modelo de Anthropic",
      regenerate: true,
    });
  });

  it("caso 2 con los intentos agotados: sin botón, y dice por qué", async () => {
    await renderDetail(
      wireExplanation({
        ...TEMPLATE_UNDER_MODEL,
        currentWriterAttempt: wireExplanationAttempt({
          failureCode: "ATTEMPT_LIMIT_REACHED",
          attemptCount: 3,
          attemptsExhausted: true,
        }),
      }),
    );

    expect(block()).toHaveTextContent(/Se agotaron los intentos de redacción/);
    expect(await requestTheButtonBuilds()).toBeNull();
  });

  it("caso 2 con el escritor vigente redactando: sin botón", async () => {
    await renderDetail(
      wireExplanation({
        ...TEMPLATE_UNDER_MODEL,
        currentWriterAttempt: wireExplanationAttempt({
          status: "PENDING",
          failureCode: null,
          failureDetail: null,
          settledAt: null,
        }),
      }),
    );

    expect(block()).toHaveTextContent(/Está redactando ahora/);
    expect(await requestTheButtonBuilds()).toBeNull();
  });

  it("caso 3: el texto de otro y ninguna fila del vigente: el botón crea la fila, sin regenerar", async () => {
    await renderDetail(wireExplanation(TEMPLATE_UNDER_MODEL));

    expect(await requestTheButtonBuilds()).toEqual({
      label: "Redactar con el modelo de Anthropic",
      regenerate: false,
    });
  });

  it("caso 4: sin nada listo, la fila fallida del vigente se reintenta", async () => {
    await renderDetail(wireExplanation({ ...BY_MODEL, ...failed() }));

    expect(await requestTheButtonBuilds()).toEqual({
      label: "Volver a intentar la explicación",
      regenerate: true,
    });
  });

  it("caso 4 con el vigente redactando: sin botón", async () => {
    await renderDetail(
      wireExplanation({ ...BY_MODEL, status: "PENDING", summary: null, settledAt: null }),
    );

    expect(await requestTheButtonBuilds()).toBeNull();
  });

  it("caso 5: el fallo de otro escritor y ninguna fila del vigente: el botón crea la fila", async () => {
    await renderDetail(wireExplanation({ ...TEMPLATE_UNDER_MODEL, ...failed() }));

    expect(await requestTheButtonBuilds()).toEqual({
      label: "Redactar con el modelo de Anthropic",
      regenerate: false,
    });
  });

  /**
   * The reservation of pending rows is unique per evaluation, provider and language: a pending row of
   * another writer of the same provider would make the current writer's collide.
   */
  it("caso 5 con una fila redactando del mismo proveedor: sin botón", async () => {
    await renderDetail(
      wireExplanation({
        provider: "ANTHROPIC",
        templateVersion: "anthropic-p0",
        writtenByAnotherTemplate: true,
        currentWriterProvider: "ANTHROPIC",
        status: "PENDING",
        summary: null,
        settledAt: null,
      }),
    );

    expect(await requestTheButtonBuilds()).toBeNull();
  });

  it("caso 5 con una fila redactando de otro proveedor: el botón crea la fila", async () => {
    await renderDetail(
      wireExplanation({ ...TEMPLATE_UNDER_MODEL, status: "PENDING", summary: null, settledAt: null }),
    );

    expect(await requestTheButtonBuilds()).toEqual({
      label: "Redactar con el modelo de Anthropic",
      regenerate: false,
    });
  });
});
