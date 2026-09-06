import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, wireAlertDetail, wireCapabilities, wireExplanation } from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
import { explainEvaluation } from "./explanation-action";
import { INITIAL_EXPLANATION_STATE } from "./explanation-state";
import AlertDetailPage from "./page";

const ALERT_ID = "2f2b7f3e-0000-4000-8000-000000000002";

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

function submission(question: string): FormData {
  const form = new FormData();
  form.set("alertId", ALERT_ID);
  form.set("ask", question);

  return form;
}

async function ask(question = "first") {
  return explainEvaluation(INITIAL_EXPLANATION_STATE, submission(question));
}

/** The body the API was posted, so a test can read which request the question became. */
function postedBody(call: number): Record<string, unknown> {
  const [, init] = fetchMock.mock.calls[call] as [URL, RequestInit];

  return JSON.parse(String(init.body)) as Record<string, unknown>;
}

/**
 * The block as the analyst reads it once an explanation is written, which is the surface the notice
 * lands next to.
 */
async function writtenBlock(): Promise<string> {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      jsonResponse(
        url.pathname === "/api/system/capabilities"
          ? wireCapabilities()
          : wireAlertDetail({ explanation: wireExplanation() }),
      ),
    ),
  );

  render(
    await renderableServerTree(AlertDetailPage({ params: Promise.resolve({ id: ALERT_ID }) })),
  );

  return screen.getByRole("region", { name: /^Explicación$/i }).textContent ?? "";
}

/**
 * Sentences, compared the way a reader compares them: without the punctuation that ends them and
 * without caring how the source file wrapped its lines.
 */
function sentences(text: string): string[] {
  return text
    .split(/[.:]/)
    .map((sentence) => sentence.replaceAll(/\s+/gu, " ").trim().toLocaleLowerCase("es"))
    .filter((sentence) => sentence.length > 0);
}

describe("acción de explicación", () => {
  it("anuncia el texto redactado, y lo distingue de un segundo intento", async () => {
    // A fresh response per call: a `Response` body is read once, and the second ask would otherwise
    // be handed an empty one and report a contract that never broke.
    fetchMock.mockImplementation(() =>
      Promise.resolve(jsonResponse({ applied: true, explanation: wireExplanation() })),
    );

    const first = await ask();
    const again = await ask("retry");

    expect(first.outcome).toBe("done");
    expect(first.title).toBe("Explicación redactada");
    expect(again.title).toBe("Explicación redactada en el nuevo intento");
    expect(first.body).toBe(again.body);
  });

  /**
   * Only a retry asks the API to replace something. Asking for the current template writes the row
   * that template does not have yet, so sending `regenerate` would turn an offer the page makes
   * over a written explanation into a request the API refuses with a 409.
   */
  it("solo el reintento viaja como regeneración", async () => {
    fetchMock.mockImplementation(() =>
      Promise.resolve(jsonResponse({ applied: true, explanation: wireExplanation() })),
    );

    await ask();
    await ask("retry");
    await ask("currentTemplate");
    await ask("cualquier-otra-cosa");

    expect(postedBody(0).regenerate).toBe(false);
    expect(postedBody(1).regenerate).toBe(true);
    expect(postedBody(2).regenerate).toBe(false);

    // A field that arrived over the network and says nothing this action knows is read as the
    // plainest of the three, never as the one that replaces a paragraph.
    expect(postedBody(3).regenerate).toBe(false);
  });

  it("con la plantilla vigente dice que el texto anterior sigue guardado", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ applied: true, explanation: wireExplanation() }));

    const state = await ask("currentTemplate");

    expect(state.outcome).toBe("done");
    expect(state.title).toMatch(/plantilla vigente/i);
    expect(state.body).toMatch(/al lado del anterior/i);
    expect(state.body).toMatch(/sigue guardado sin cambios/i);

    // No alarm anywhere in the wording: what changed is how the paragraph is worded, not what it
    // says about the order.
    expect(state.body).not.toMatch(/desactualizad/i);
  });

  /**
   * The notice is read directly under the legend of the block, so anything it repeats is read
   * twice. It exists to say what was stored and what follows from that; the legend already says who
   * wrote the text and what was checked before it was kept.
   */
  it.each(["first", "retry", "currentTemplate"])(
    "no repite ninguna oración de la leyenda del bloque: %s",
    async (question) => {
      const legend = sentences(await writtenBlock());

      vi.clearAllMocks();
      fetchMock.mockResolvedValue(jsonResponse({ applied: true, explanation: wireExplanation() }));
      const notice = sentences((await ask(question)).body);

      expect(notice.length).toBeGreaterThan(1);
      expect(notice.filter((sentence) => legend.includes(sentence))).toEqual([]);
    },
  );

  it("con applied=false no dice que se acaba de redactar nada", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ applied: false, explanation: wireExplanation() }));

    const state = await ask();

    expect(state.outcome).toBe("done");
    expect(state.title).toMatch(/ya tenía su explicación/i);
    expect(state.body).not.toMatch(/quedó guardado/i);
  });

  it("una fila sin texto utilizable es un fallo, aunque la petición haya salido bien", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({
        applied: true,
        explanation: wireExplanation({
          status: "FAILED",
          summary: null,
          referencedRules: [],
          failureCode: "NOT_GROUNDED_NUMBER",
        }),
      }),
    );

    const state = await ask();

    expect(state.outcome).toBe("failed");
    expect(state.body).toMatch(/la evaluación no respalda/i);
    expect(state.recovery).toMatch(/volver a intentarlo/i);
  });
});
