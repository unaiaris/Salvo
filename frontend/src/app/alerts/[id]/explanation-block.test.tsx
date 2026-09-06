import { render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, wireAlertDetail, wireCapabilities, wireExplanation } from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import AlertDetailPage from "./page";

const ALERT_ID = "2f2b7f3e-0000-4000-8000-000000000002";

/** Every value of `ExplanationFailureCode`, read off `ExplanationWireNames.cs`. */
const FAILURE_CODES = [
  "PROVIDER_UNAVAILABLE",
  "PROVIDER_TIMEOUT",
  "PROVIDER_REFUSED",
  "MALFORMED_OUTPUT",
  "NOT_GROUNDED_NUMBER",
  "NOT_GROUNDED_RULE",
  "TOO_LONG",
  "CANCELLED",
  "ATTEMPT_LIMIT_REACHED",
] as const;

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

async function renderDetail(detail: Record<string, unknown> = {}) {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      jsonResponse(
        url.pathname === "/api/system/capabilities"
          ? wireCapabilities()
          : wireAlertDetail(detail),
      ),
    ),
  );

  return render(
    await renderableServerTree(AlertDetailPage({ params: Promise.resolve({ id: ALERT_ID }) })),
  );
}

function block() {
  return screen.getByRole("region", { name: /^Explicación$/i });
}

function failedExplanation(overrides: Record<string, unknown> = {}) {
  return wireExplanation({
    status: "FAILED",
    summary: null,
    referencedRules: [],
    failureCode: "NOT_GROUNDED_NUMBER",
    attemptCount: 1,
    ...overrides,
  });
}

describe("bloque de explicación", () => {
  it("es un bloque más del detalle, junto a los otros tres", async () => {
    await renderDetail();

    expect(screen.getByRole("region", { name: /Snapshot que abrió la alerta/i })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: /Evaluación vigente/i })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: /Evaluación externa/i })).toBeInTheDocument();
    expect(block()).toBeInTheDocument();
  });

  it("sin explicación pedida lo dice, y ofrece pedirla", async () => {
    await renderDetail();

    expect(block()).toHaveTextContent(/Todavía no se pidió una explicación/i);
    expect(
      within(block()).getByRole("button", { name: /Explicar esta evaluación/i }),
    ).toBeInTheDocument();
  });

  it("muestra el resumen escrito y declara qué plantilla lo compuso, no un modelo", async () => {
    await renderDetail({ explanation: wireExplanation() });

    const written = block();
    expect(written).toHaveTextContent("El pedido obtuvo 100 puntos sobre un umbral de 60.");
    expect(written).toHaveTextContent(
      /Redactada por una plantilla determinista \(e7-v1\), no por un modelo/i,
    );
    expect(written).toHaveTextContent(/Reglas citadas: Monto atípico/);
  });

  it("no ofrece regenerar una explicación ya escrita", async () => {
    await renderDetail({ explanation: wireExplanation() });

    expect(within(block()).queryByRole("button")).not.toBeInTheDocument();
  });

  /**
   * The case E7C left open: the template that wrote the paragraph is no longer the one this
   * deployment writes with, so the row the current template would own does not exist and asking for
   * it replaces nothing. Without the offer, a wording fix never reaches an evaluation somebody
   * already explained — the API is never asked, because nothing on the page can ask it.
   */
  it("ofrece redactar con la plantilla vigente cuando otra plantilla escribió el texto", async () => {
    await renderDetail({
      explanation: wireExplanation({ templateVersion: "e7-v1", writtenByAnotherTemplate: true }),
    });

    const older = block();
    expect(
      within(older).getByRole("button", { name: /Redactar con la plantilla vigente/i }),
    ).toBeInTheDocument();

    // The offer, and nothing else: a change of wording does not invalidate what the paragraph says,
    // and a badge or a notice would read as an alarm over a cosmetic fix.
    expect(within(older).queryByRole("note")).not.toBeInTheDocument();
    expect(older.textContent).not.toMatch(/desactualizad/i);

    // The paragraph stays on screen, and the small print says which template wrote it.
    expect(older).toHaveTextContent("El pedido obtuvo 100 puntos sobre un umbral de 60.");
    expect(older).toHaveTextContent(/plantilla determinista \(e7-v1\)/i);

    // Not a retry: nothing failed, and telling a reader to try again sends them looking for an
    // error that is not there.
    expect(
      within(older).queryByRole("button", { name: /Volver a intentar/i }),
    ).not.toBeInTheDocument();
  });

  /**
   * The other half of the same claim. Over a row the current template wrote there is nothing to
   * offer, and the API would refuse anyway.
   */
  it("no ofrece nada sobre una explicación escrita por la plantilla vigente", async () => {
    await renderDetail({
      explanation: wireExplanation({ writtenByAnotherTemplate: false }),
    });

    expect(within(block()).queryByRole("button")).not.toBeInTheDocument();
  });

  /**
   * A failed row from a previous template is offered the current template rather than a retry: the
   * request will not touch that row at all, so its spent or unspent attempts describe something
   * else.
   */
  it("sobre un fallo de otra plantilla ofrece la vigente, no un reintento", async () => {
    await renderDetail({
      explanation: failedExplanation({
        attemptsExhausted: true,
        failureCode: "ATTEMPT_LIMIT_REACHED",
        attemptCount: 3,
        writtenByAnotherTemplate: true,
      }),
    });

    expect(
      within(block()).getByRole("button", { name: /Redactar con la plantilla vigente/i }),
    ).toBeInTheDocument();
  });

  /**
   * Four different things wait in this console — an alert for a verdict, an external evaluation for
   * the provider, an order to be scored, and this — so none of them is ever called "pendiente" on
   * its own.
   */
  it("mientras se redacta no dice «pendiente» a secas, y no ofrece un segundo pedido", async () => {
    await renderDetail({
      explanation: wireExplanation({ status: "PENDING", summary: null, settledAt: null }),
    });

    const waiting = block();
    expect(waiting).toHaveTextContent(/Redactando la explicación/i);
    expect(waiting.textContent).not.toMatch(/\bpendiente\b/i);
    expect(within(waiting).queryByRole("button")).not.toBeInTheDocument();
  });

  it("con un fallo con intentos disponibles rotula el motivo y ofrece reintentar", async () => {
    await renderDetail({ explanation: failedExplanation() });

    const failed = block();
    expect(failed).toHaveTextContent(/cifra que la evaluación no respalda/i);
    expect(
      within(failed).getByRole("button", { name: /Volver a intentar la explicación/i }),
    ).toBeInTheDocument();
  });

  it("con el tope de intentos agotado rotula el motivo y no ofrece botón", async () => {
    await renderDetail({
      explanation: failedExplanation({
        failureCode: "ATTEMPT_LIMIT_REACHED",
        attemptCount: 3,
        attemptsExhausted: true,
      }),
    });

    const exhausted = block();
    expect(exhausted).toHaveTextContent(/Se agotaron los intentos de redacción/i);
    expect(exhausted).toHaveTextContent(/Se intentó 3 veces/);
    expect(exhausted).toHaveTextContent(/no necesita explicación para emitirse/i);
    expect(within(exhausted).queryByRole("button")).not.toBeInTheDocument();
  });

  it("ningún código de fallo se muestra crudo", async () => {
    for (const code of FAILURE_CODES) {
      const { unmount } = await renderDetail({
        explanation: failedExplanation({ failureCode: code }),
      });

      expect(block().textContent, code).not.toContain(code);
      unmount();
    }
  });

  /**
   * The notice precedes the text and never hides it: an outdated explanation is the record of what
   * could have been read while the verdict was being formed, which is exactly why it is kept.
   */
  it("el aviso de desactualizada precede al contenido y no lo oculta", async () => {
    await renderDetail({ explanation: wireExplanation({ isOutdated: true }) });

    const outdated = block();
    const notice = within(outdated).getByRole("note");
    const summary = within(outdated).getByText(/El pedido obtuvo 100 puntos/);

    expect(notice).toHaveTextContent(/ya no es la vigente/i);
    expect(summary).toBeInTheDocument();
    expect(notice.compareDocumentPosition(summary) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("no avisa de desactualizada cuando la explicación es la de la evaluación vigente", async () => {
    await renderDetail({ explanation: wireExplanation() });

    expect(within(block()).queryByRole("note")).not.toBeInTheDocument();
  });

  it("dice que la evaluación vigente ya tiene la suya, cuando existe", async () => {
    await renderDetail({
      explanation: wireExplanation({ isOutdated: true }),
      currentExplanation: wireExplanation({
        id: "7f7b7f3e-0000-4000-8000-000000000007",
        summary: "Otra evaluación, otro texto.",
      }),
    });

    expect(within(block()).getByRole("note")).toHaveTextContent(/su propia explicación escrita/i);
  });
});
