import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  jsonResponse,
  wireAlertDetail,
  wireCapabilities,
  wireExternalEvaluation,
} from "@/test/fixtures";
import { renderableServerTree } from "@/test/server-tree";
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

async function renderDetail(
  detail: Record<string, unknown> = {},
  capabilities: Record<string, unknown> = {},
) {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      jsonResponse(
        url.pathname === "/api/system/capabilities"
          ? wireCapabilities(capabilities)
          : wireAlertDetail(detail),
      ),
    ),
  );

  return render(
    await renderableServerTree(AlertDetailPage({ params: Promise.resolve({ id: ALERT_ID }) })),
  );
}

function externalBlock() {
  return screen.getByRole("region", { name: /Evaluación externa/i });
}

describe("bloque de evaluación externa", () => {
  it("es un tercer bloque, junto al snapshot y la evaluación vigente", async () => {
    await renderDetail();

    expect(screen.getByRole("region", { name: /Snapshot que abrió la alerta/i })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: /Evaluación vigente/i })).toBeInTheDocument();
    expect(externalBlock()).toBeInTheDocument();
  });

  it("dice el estado del proveedor con su procedencia y su instante", async () => {
    await renderDetail();

    const block = externalBlock();
    expect(block).toHaveTextContent(/Denegado por el proveedor/);
    expect(block).toHaveTextContent(/por callback del proveedor/);
    expect(block).toHaveTextContent(/Pedida el/);
    expect(block).toHaveTextContent(/Respondida el/);
  });

  /**
   * The three "pending" of this system are named apart on purpose: an alert waiting for a human
   * verdict, an external evaluation waiting for the provider, and an order waiting to be scored.
   * A screen that called all three the same word would be unreadable at exactly the moment an
   * analyst needs to know which one is holding things up.
   */
  it("nunca dice «pendiente» a secas para el estado del proveedor", async () => {
    await renderDetail({ externalEvaluation: wireExternalEvaluation({ status: "PENDING", settledAt: null, settledBy: null, score: null }) });

    const block = externalBlock();
    expect(block).toHaveTextContent(/Esperando al proveedor/);
    expect(block.textContent ?? "").not.toMatch(/\bpendiente\b/i);
  });

  it("ofrece solicitar la evaluación cuando no hay ninguna", async () => {
    await renderDetail({ externalEvaluation: null });

    const block = externalBlock();
    expect(block).toHaveTextContent(/Todavía no se pidió ninguna/);
    expect(
      screen.getByRole("button", { name: /Solicitar evaluación externa/i }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /Entregar el callback/i }),
    ).not.toBeInTheDocument();
  });

  it("ofrece entregar el callback solo mientras se espera al proveedor y con la bandera puesta", async () => {
    const waiting = {
      externalEvaluation: wireExternalEvaluation({
        status: "PENDING",
        settledAt: null,
        settledBy: null,
        score: null,
      }),
    };

    const { unmount } = await renderDetail(waiting);
    expect(screen.getByRole("button", { name: /Entregar el callback/i })).toBeInTheDocument();
    unmount();

    await renderDetail(waiting, { externalCallbackTriggerEnabled: false });
    expect(screen.queryByRole("button", { name: /Entregar el callback/i })).not.toBeInTheDocument();
  });

  it("no ofrece entregar el callback cuando el proveedor ya se pronunció", async () => {
    await renderDetail();

    expect(screen.queryByRole("button", { name: /Entregar el callback/i })).not.toBeInTheDocument();
  });

  /**
   * The provider denies, the local engine flagged the order: the two agree, so there is nothing to
   * report. The notice is for disagreement, not for the presence of a second opinion.
   */
  it("no anuncia divergencia cuando los dos criterios coinciden", async () => {
    await renderDetail();

    expect(externalBlock()).not.toHaveTextContent(/Los dos criterios no coinciden/);
  });

  it("muestra las dos opiniones con su procedencia cuando difieren", async () => {
    await renderDetail({
      externalEvaluation: wireExternalEvaluation({ status: "APPROVED", score: 12 }),
    });

    const block = externalBlock();
    expect(block).toHaveTextContent(/Los dos criterios no coinciden/);
    expect(block).toHaveTextContent(/Motor local:/);
    expect(block).toHaveTextContent(/marcó el pedido por encima del umbral/);
    expect(block).toHaveTextContent(/Proveedor externo:/);
    expect(block).toHaveTextContent(/aprobó el pedido/);
  });

  /**
   * The two scores are on different scales of different systems, so nothing may put them beside each
   * other as if the difference meant something. The provider's own score is shown with that said in
   * as many words.
   */
  it("no compara los dos scores numéricamente", async () => {
    await renderDetail({
      externalEvaluation: wireExternalEvaluation({ status: "APPROVED", score: 12 }),
    });

    const block = externalBlock();
    expect(block).toHaveTextContent(/Score del proveedor: 12/);
    expect(block).toHaveTextContent(/no se compara con el score local/);
    expect(block).not.toHaveTextContent(/100/);
  });

  it("muestra la contradicción del proveedor en vez de tragársela", async () => {
    await renderDetail({
      externalEvaluation: wireExternalEvaluation({ hasContradictoryCallback: true }),
    });

    expect(externalBlock()).toHaveTextContent(/veredicto contradictorio/);
  });

  it("nombra el error cuando la consulta al proveedor terminó mal", async () => {
    await renderDetail({
      externalEvaluation: wireExternalEvaluation({
        status: "ERROR",
        score: null,
        errorCode: "UNREACHABLE",
        settledBy: "SYNC",
      }),
    });

    const block = externalBlock();
    expect(block).toHaveTextContent(/La consulta al proveedor falló/);
    expect(block).toHaveTextContent(/la consulta nunca salió/);
  });

  /**
   * A failed attempt on a row that is still waiting is not a verdict, and the block says so rather
   * than letting an analyst read the amber line as an answer.
   */
  it("distingue un intento fallido de un veredicto", async () => {
    await renderDetail({
      externalEvaluation: wireExternalEvaluation({
        status: "PENDING",
        score: null,
        settledAt: null,
        settledBy: null,
        lastErrorCode: "TIMEOUT",
      }),
    });

    const block = externalBlock();
    expect(block).toHaveTextContent(/Último intento fallido/);
    expect(block).toHaveTextContent(/no es un veredicto/);
  });

  /**
   * Capabilities are a second read on this page, and a page that refused to render because that one
   * failed would hide the alert over a button.
   */
  it("sigue mostrando la alerta si no se pueden leer las capacidades", async () => {
    fetchMock.mockImplementation((url: URL) =>
      url.pathname === "/api/system/capabilities"
        ? Promise.reject(new TypeError("fetch failed"))
        : Promise.resolve(jsonResponse(wireAlertDetail())),
    );

    render(
      await renderableServerTree(AlertDetailPage({ params: Promise.resolve({ id: ALERT_ID }) })),
    );

    expect(externalBlock()).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Entregar el callback/i })).not.toBeInTheDocument();
  });
});
