import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { jsonResponse, problemResponse, wireAlertDetail,
  mockConsoleFetch,
} from "@/test/fixtures";
import { reviewAlert } from "./review-action";
import { INITIAL_REVIEW_STATE } from "./review-state";

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

function submission({
  newStatus = "CONFIRMED_SAFE",
  note = "",
  acknowledged = false,
  explanationId = "",
}: {
  newStatus?: string;
  note?: string;
  acknowledged?: boolean;
  explanationId?: string;
} = {}): FormData {
  const form = new FormData();
  form.set("alertId", ALERT_ID);
  form.set("newStatus", newStatus);
  form.set("note", note);
  form.set("explanationId", explanationId);
  if (acknowledged) {
    form.set("acknowledgedDivergence", "on");
  }

  return form;
}

/**
 * The review call, found rather than assumed to be the first.
 *
 * Every action asks the API for the deployment language before it does anything else, so the first
 * call is the capabilities read. Indexing by position would make these assertions depend on an
 * order that is not what they are about.
 */
function reviewCall(): [URL, RequestInit] {
  const call = fetchMock.mock.calls.find(
    ([url]) => (url as URL).pathname !== "/api/system/capabilities",
  );

  expect(call).toBeDefined();

  return call as [URL, RequestInit];
}

describe("acción de revisión", () => {
  it("registra el veredicto cuando la API lo aplica", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse({ applied: true, alert: wireAlertDetail() }));

    const state = await reviewAlert(INITIAL_REVIEW_STATE, submission());

    expect(state.outcome).toBe("applied");
    expect(state.title).toMatch(/Veredicto registrado/i);
  });

  it("con applied=false no dice «revisión registrada»", async () => {
    // applied=false significa «ya estaba exactamente así»: decir otra cosa le haría creer a la
    // analista que acaba de decidir algo que no decidió.
    mockConsoleFetch(fetchMock, () => jsonResponse({ applied: false, alert: wireAlertDetail() }));

    const state = await reviewAlert(INITIAL_REVIEW_STATE, submission());

    expect(state.outcome).toBe("unchanged");
    expect(state.title).toMatch(/ya tenía exactamente este veredicto/i);
    expect(state.title).not.toMatch(/veredicto registrado/i);
    expect(state.body).not.toMatch(/veredicto registrado/i);
  });

  it("devuelve la nota enviada cuando la API responde 409", async () => {
    mockConsoleFetch(fetchMock, () => 
      problemResponse(409, "ALERT_ALREADY_REVIEWED", "Alert was already reviewed."),
    );

    const note = "Coincide con el patrón de la semana pasada.";
    const state = await reviewAlert(INITIAL_REVIEW_STATE, submission({ note }));

    expect(state.outcome).toBe("failed");
    expect(state.submittedNote).toBe(note);
    expect(state.submittedStatus).toBe("CONFIRMED_SAFE");
  });

  it("devuelve también el reconocimiento marcado", async () => {
    mockConsoleFetch(fetchMock, () => 
      problemResponse(409, "ALERT_REVIEW_CONFLICT", "Concurrent review."),
    );

    const state = await reviewAlert(
      INITIAL_REVIEW_STATE,
      submission({ acknowledged: true, note: "nota" }),
    );

    expect(state.acknowledged).toBe(true);
  });

  it("traduce cada código a su mensaje y nunca muestra solo «409»", async () => {
    const codes = [
      "ALERT_ALREADY_REVIEWED",
      "ALERT_REVIEW_NOTE_CONFLICT",
      "ALERT_DIVERGENCE_NOT_ACKNOWLEDGED",
      "ALERT_REVIEW_CONFLICT",
    ];
    const titles: string[] = [];

    for (const code of codes) {
      mockConsoleFetch(fetchMock, () => problemResponse(409, code, `detalle de ${code}`));
      const state = await reviewAlert(INITIAL_REVIEW_STATE, submission());

      expect(state.outcome).toBe("failed");
      expect(state.title).not.toMatch(/\b409\b/);
      expect(state.technicalDetail).toBe(`detalle de ${code}`);
      titles.push(state.title);
    }

    expect(new Set(titles).size).toBe(codes.length);
  });

  it("marca los errores de formulario para que se muestren junto al campo", async () => {
    mockConsoleFetch(fetchMock, () => problemResponse(400, "NOTE_TOO_LONG", "note must not exceed 2000."));

    const state = await reviewAlert(INITIAL_REVIEW_STATE, submission({ note: "x".repeat(2001) }));

    expect(state.isFormError).toBe(true);
    expect(state.submittedNote).toHaveLength(2001);
  });

  it("numera cada intento para que el formulario sepa cuándo re-sembrar sus campos", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse({ applied: true, alert: wireAlertDetail() }));

    const first = await reviewAlert(INITIAL_REVIEW_STATE, submission());
    const second = await reviewAlert(first, submission());

    expect(first.submissionId).toBe(1);
    expect(second.submissionId).toBe(2);
  });

  /**
   * D10 again, on the other half: what the form sent has to reach the API as it stands. An empty
   * field is `null` and not `""` — the column is a foreign key, and the absence of an explanation is
   * a fact worth storing as an absence.
   */
  it("manda el id de la explicación que estaba en pantalla", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse({ applied: true, alert: wireAlertDetail() }));

    await reviewAlert(
      INITIAL_REVIEW_STATE,
      submission({ explanationId: "6f6b7f3e-0000-4000-8000-000000000006" }),
    );

    const [, init] = reviewCall();
    expect(JSON.parse(String(init.body)).explanationId).toBe(
      "6f6b7f3e-0000-4000-8000-000000000006",
    );
  });

  it("sin explicación manda null, y la revisión se aplica igual", async () => {
    mockConsoleFetch(fetchMock, () => jsonResponse({ applied: true, alert: wireAlertDetail() }));

    const state = await reviewAlert(INITIAL_REVIEW_STATE, submission());

    const [, init] = reviewCall();
    expect(JSON.parse(String(init.body)).explanationId).toBeNull();
    expect(state.outcome).toBe("applied");
  });

  it("una API caída durante el envío no rompe la acción", async () => {
    fetchMock.mockRejectedValue(new TypeError("fetch failed"));

    const state = await reviewAlert(INITIAL_REVIEW_STATE, submission({ note: "no se pierde" }));

    expect(state.outcome).toBe("failed");
    expect(state.title).toMatch(/No se pudo contactar a la API/i);
    expect(state.submittedNote).toBe("no se pierde");
  });
});
