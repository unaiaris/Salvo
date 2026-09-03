import { readFileSync } from "node:fs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import AlertDetailPage from "@/app/alerts/[id]/page";
import AlertsPage from "@/app/alerts/page";
import { jsonResponse, wireAlertDetail, wireAlertList, wireAlertListItem } from "./fixtures";
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

  return detail;
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
    fetchMock.mockResolvedValue(jsonResponse(contaminatedDetail()));

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
    fetchMock.mockResolvedValue(jsonResponse(contaminatedDetail()));

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

describe("módulos server-only", () => {
  /**
   * Vitest resolves `server-only` to a stub so the modules can be imported at all, which means the
   * package can no longer be the thing that stops them reaching a browser bundle. Reading the source
   * restores that check: the declaration has to be there, whether or not the test runner honours it.
   */
  it("el cliente de la API declara import \"server-only\"", () => {
    for (const path of ["src/lib/api/server-client.ts", "src/lib/api/alerts.ts"]) {
      expect(readFileSync(path, "utf8"), path).toMatch(/^import "server-only";$/m);
    }
  });

  it("ningún componente cliente importa el cliente de la API", () => {
    for (const file of clientComponentFiles()) {
      const source = readFileSync(file, "utf8");

      expect(source, file).not.toMatch(/from "@\/lib\/api\/(alerts|server-client)"/);
    }
  });
});
