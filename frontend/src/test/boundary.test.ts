import { readFileSync } from "node:fs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import AlertDetailPage from "@/app/alerts/[id]/page";
import AlertsPage from "@/app/alerts/page";
import DashboardPage from "@/app/dashboard/page";
import ImportPage from "@/app/import/page";
import {
  jsonResponse,
  wireAlertDetail,
  wireAlertList,
  wireAlertListItem,
  wireCapabilities,
  wireDashboard,
  wireEvaluationMetrics,
  wireExternalEvaluation,
} from "./fixtures";
import {
  clientComponentFiles,
  clientComponents,
  collectBoundaryCrossings,
  isPrimitiveProp,
} from "./server-tree";

/**
 * The server–client boundary, checked instead of promised.
 *
 * The stage 4 test asserted that the HTML did not contain `isFraudLabel`. It passed vacuously: the
 * API does not emit that field, so no source could have produced the string and the assertion could
 * never have failed. What follows replaces it with two assertions that can fail, and that fail for
 * different reasons:
 *
 * 1. **Shape.** Every prop that reaches a client component is a primitive. This is the invariant the
 *    design states, and it fails the moment anybody hands a client component an API object —
 *    regardless of what that object contains.
 * 2. **Content.** With the API returning fields the console has never heard of, including
 *    `isFraudLabel: true`, none of them appear anywhere in what crosses the boundary or in the
 *    rendered output. This one can fail, because the payload really does contain the field.
 *
 * Falsification is recorded in the handoff: replacing the primitive props of `ReviewForm` with the
 * whole `AlertDetail` makes assertion 1 fail on `detail` and assertion 2 fail on `isFraudLabel`.
 */

/** Unknown keys, injected at every level the contract has an object. */
const INTRUDERS = {
  isFraudLabel: true,
  internalNotes: "no debería cruzar",
  labelSource: "order_evaluation_labels",
} as const;

function contaminatedDetail(): Record<string, unknown> {
  const detail = wireAlertDetail({ ...INTRUDERS });
  const snapshot = detail.snapshot as Record<string, unknown>;
  const evaluation = detail.currentEvaluation as Record<string, unknown>;

  detail.order = { ...(detail.order as Record<string, unknown>), ...INTRUDERS };
  detail.snapshot = { ...snapshot, ...INTRUDERS };
  detail.currentEvaluation = { ...evaluation, ...INTRUDERS };
  detail.divergence = { ...(detail.divergence as Record<string, unknown>), ...INTRUDERS };

  // The external block is the third object on this page and the newest, so it is contaminated like
  // the rest: a sub-object added for stage 6 must not become the one place unknown fields ride
  // through. Denied against a flagged local evaluation, so the divergence notice renders too.
  detail.externalEvaluation = {
    ...wireExternalEvaluation({ status: "PENDING", settledAt: null, settledBy: null, score: null }),
    ...INTRUDERS,
  };

  return detail;
}

/**
 * The detail page reads two endpoints. Answering both with the alert would make the capabilities
 * projection fail and quietly hide the demo trigger, which is one of the client components this
 * check exists to look at.
 */
function contaminateDetailRoutes(): void {
  fetchMock.mockImplementation((url: URL) =>
    Promise.resolve(
      jsonResponse(
        url.pathname === "/api/system/capabilities"
          ? wireCapabilities({ ...INTRUDERS })
          : contaminatedDetail(),
      ),
    ),
  );
}

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

async function crossingsOf(node: Promise<React.ReactNode> | React.ReactNode) {
  return collectBoundaryCrossings(node, await clientComponents());
}

describe("frontera servidor–cliente", () => {
  it("descubre al menos un componente cliente, para que la comprobación no sea vacía", async () => {
    const files = clientComponentFiles();

    expect(files.length).toBeGreaterThan(0);
    expect((await clientComponents()).size).toBeGreaterThan(0);
  });

  it("no pasa ningún objeto a un componente cliente desde el detalle", async () => {
    contaminateDetailRoutes();

    const crossings = await crossingsOf(
      AlertDetailPage({ params: Promise.resolve({ id: "2f2b7f3e-0000-4000-8000-000000000002" }) }),
    );

    expect(crossings.length).toBeGreaterThan(0);

    for (const crossing of crossings) {
      for (const [name, value] of Object.entries(crossing.props)) {
        expect(
          isPrimitiveProp(value),
          `${crossing.componentName} recibe la prop no primitiva «${name}»: ${JSON.stringify(value)}`,
        ).toBe(true);
      }
    }
  });

  it("ningún campo desconocido de la API cruza la frontera ni llega al render", async () => {
    contaminateDetailRoutes();

    const tree = await AlertDetailPage({
      params: Promise.resolve({ id: "2f2b7f3e-0000-4000-8000-000000000002" }),
    });
    const crossings = await crossingsOf(tree);

    const serializedProps = JSON.stringify(crossings.map((crossing) => crossing.props));
    const serializedTree = JSON.stringify(tree, (_key, value: unknown) =>
      typeof value === "function" ? value.name : value,
    );

    for (const intruder of Object.keys(INTRUDERS)) {
      expect(serializedProps).not.toContain(intruder);
      expect(serializedTree).not.toContain(intruder);
    }

    for (const intruder of Object.values(INTRUDERS)) {
      if (typeof intruder === "string") {
        expect(serializedProps).not.toContain(intruder);
        expect(serializedTree).not.toContain(intruder);
      }
    }
  });

  it("tampoco los pasa desde el feed", async () => {
    fetchMock.mockResolvedValue(
      jsonResponse(
        wireAlertList({
          ...INTRUDERS,
          items: [wireAlertListItem({ ...INTRUDERS })],
        }),
      ),
    );

    const tree = await AlertsPage();
    const crossings = await crossingsOf(tree);
    const serialized = JSON.stringify([crossings.map((c) => c.props), tree]);

    for (const crossing of crossings) {
      for (const [name, value] of Object.entries(crossing.props)) {
        expect(isPrimitiveProp(value), `${crossing.componentName}.${name}`).toBe(true);
      }
    }

    for (const intruder of Object.keys(INTRUDERS)) {
      expect(serialized).not.toContain(intruder);
    }
  });
});

describe("frontera servidor–cliente en importación y dashboard", () => {
  /**
   * The dashboard is the screen with the most data on it and the one a charting library would have
   * turned into a client component, so it is the one where decision 43 is worth checking rather than
   * asserting. Every path answers with the unknown fields injected, including the metrics — the only
   * response in the console that is derived from ground-truth labels.
   */
  function contaminateEverything(): void {
    fetchMock.mockImplementation((url: URL) => {
      switch (url.pathname) {
        case "/api/system/capabilities":
          return Promise.resolve(jsonResponse(wireCapabilities({ ...INTRUDERS })));
        case "/api/evaluation-metrics":
          return Promise.resolve(jsonResponse(wireEvaluationMetrics({ ...INTRUDERS })));
        default:
          return Promise.resolve(
            jsonResponse(
              wireDashboard({
                ...INTRUDERS,
                openAlerts: {
                  total: 18,
                  bySeverity: [{ severity: "CRITICAL", alertCount: 5, ...INTRUDERS }],
                },
                amountAtRisk: [
                  { currencyCode: "UYU", amountCents: 1_000, alertCount: 1, ...INTRUDERS },
                ],
                riskOverTime: [
                  { weekStart: "2026-08-24", orderCount: 24, flaggedCount: 5, ...INTRUDERS },
                ],
                topSignals: [{ rule: "amount_anomaly", alertCount: 18, ...INTRUDERS }],
              }),
            ),
          );
      }
    });
  }

  it("el dashboard no cruza la frontera ni una sola vez", async () => {
    contaminateEverything();

    const crossings = await crossingsOf(DashboardPage());

    // Cero, no «primitivas»: la pantalla entera, gráfico incluido, se renderiza en el servidor. Es
    // la consecuencia observable de la decisión 43, y una librería de gráficos la rompería con solo
    // entrar, porque obligaría a que el componente del gráfico fuera de cliente.
    expect(crossings.map((crossing) => crossing.componentName)).toEqual([]);
  });

  it("ningún campo desconocido del dashboard ni de las métricas llega al render", async () => {
    contaminateEverything();

    const tree = await DashboardPage();
    const crossings = await crossingsOf(tree);
    const serialized = JSON.stringify([crossings.map((crossing) => crossing.props), tree], (_key, value: unknown) =>
      typeof value === "function" ? value.name : value,
    );

    for (const intruder of [...Object.keys(INTRUDERS), ...Object.values(INTRUDERS)]) {
      if (typeof intruder === "string") {
        expect(serialized).not.toContain(intruder);
      }
    }
  });

  it("la importación tampoco pasa objetos, ni siquiera el estado del corpus", async () => {
    fetchMock.mockImplementation((url: URL) =>
      Promise.resolve(
        jsonResponse(
          url.pathname === "/api/system/capabilities"
            ? wireCapabilities({ ...INTRUDERS })
            : wireDashboard({ ...INTRUDERS }),
        ),
      ),
    );

    const tree = await ImportPage();
    const crossings = await crossingsOf(tree);

    expect(crossings.length).toBeGreaterThan(0);

    for (const crossing of crossings) {
      for (const [name, value] of Object.entries(crossing.props)) {
        expect(
          isPrimitiveProp(value),
          `${crossing.componentName} recibe la prop no primitiva «${name}»`,
        ).toBe(true);
      }
    }

    const serialized = JSON.stringify([crossings.map((crossing) => crossing.props), tree], (_key, value: unknown) =>
      typeof value === "function" ? value.name : value,
    );

    for (const intruder of Object.keys(INTRUDERS)) {
      expect(serialized).not.toContain(intruder);
    }
  });
});

describe("módulos server-only", () => {
  /**
   * Vitest resolves `server-only` to a stub so the modules can be imported at all, which means the
   * package can no longer be the thing that stops them reaching a browser bundle. Reading the source
   * restores that check: the declaration has to be there, whether or not the test runner honours it.
   */
  it("el cliente de la API declara import \"server-only\"", () => {
    for (const path of [
      "src/lib/api/server-client.ts",
      "src/lib/api/alerts.ts",
      "src/lib/api/console.ts",
    ]) {
      expect(readFileSync(path, "utf8"), path).toMatch(/^import "server-only";$/m);
    }
  });

  it("ningún componente cliente importa el cliente de la API", () => {
    for (const file of clientComponentFiles()) {
      const source = readFileSync(file, "utf8");

      expect(source, file).not.toMatch(/from "@\/lib\/api\/(alerts|console|server-client)"/);
    }
  });
});
