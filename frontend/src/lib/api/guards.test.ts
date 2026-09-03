import { describe, expect, it } from "vitest";

import { wireAlertDetail, wireAlertList, wireAlertListItem, wireOrder, wireSignal } from "@/test/fixtures";
import {
  projectAlertDetail,
  projectAlertList,
  projectAlertListItem,
  projectOrder,
  projectSignal,
} from "./guards";

/**
 * The guards project, and these tests are what tells projection apart from a predicate.
 *
 * A predicate over the same object would pass every one of the "accepts a valid payload" cases and
 * fail none of the "drops the unknown key" ones — which is exactly the substitution the design forbids
 * and why the drop is asserted at every level of the contract rather than only at the top.
 */

describe("las guardas descartan claves desconocidas", () => {
  it("las descarta en el nivel superior del detalle", () => {
    const projected = projectAlertDetail(
      wireAlertDetail({ isFraudLabel: true, futureField: "de la etapa 7" }),
    );

    expect(projected).not.toBeNull();
    expect(Object.keys(projected ?? {})).not.toContain("isFraudLabel");
    expect(Object.keys(projected ?? {})).not.toContain("futureField");
  });

  it("las descarta en los objetos anidados", () => {
    const detail = wireAlertDetail();
    detail.order = { ...(detail.order as Record<string, unknown>), isFraudLabel: true };
    detail.snapshot = {
      ...(detail.snapshot as Record<string, unknown>),
      signals: [wireSignal({ isFraudLabel: true })],
    };

    const projected = projectAlertDetail(detail);

    expect(projected).not.toBeNull();
    expect(JSON.stringify(projected)).not.toContain("isFraudLabel");
  });

  it("las descarta en los ítems del feed", () => {
    const projected = projectAlertList(
      wireAlertList({ items: [wireAlertListItem({ isFraudLabel: true })], isFraudLabel: true }),
    );

    expect(JSON.stringify(projected)).not.toContain("isFraudLabel");
  });

  it("conserva exactamente las claves del contrato", () => {
    const projected = projectSignal(wireSignal({ isFraudLabel: true }));

    expect(Object.keys(projected ?? {}).sort()).toEqual(["detail", "rule", "weight"]);
  });
});

describe("las guardas rechazan lo que el contrato exige y no llegó", () => {
  it.each([
    "id",
    "status",
    "severity",
    "order",
    "snapshot",
    "divergence",
    "createdAt",
    "alertPolicyVersion",
  ])("rechaza un detalle sin «%s»", (missing) => {
    const detail = wireAlertDetail();
    delete detail[missing];

    expect(projectAlertDetail(detail)).toBeNull();
  });

  it("rechaza una señal sin detalle legible", () => {
    const signal = wireSignal();
    delete signal.detail;

    expect(projectSignal(signal)).toBeNull();
  });

  it("rechaza un tipo equivocado en vez de arrastrarlo", () => {
    expect(projectOrder(wireOrder({ amountCents: "no es un número" }))).toBeNull();
    expect(projectSignal(wireSignal({ weight: null }))).toBeNull();
    expect(projectAlertListItem(wireAlertListItem({ hasBandDivergence: "sí" }))).toBeNull();
  });

  it("rechaza un instante que no se puede leer", () => {
    expect(projectOrder(wireOrder({ occurredAt: "ayer a la tarde" }))).toBeNull();
  });

  it("rechaza lo que no es un objeto", () => {
    expect(projectAlertDetail(null)).toBeNull();
    expect(projectAlertDetail([])).toBeNull();
    expect(projectAlertDetail("OPEN")).toBeNull();
  });
});

describe("las guardas aceptan lo que el contrato permite", () => {
  it("acepta un entero en su forma de cadena decimal", () => {
    // ASP.NET Core declara todo entero como `["integer", "string"]`, así que la forma de cadena es
    // parte del contrato y no una anomalía.
    const projected = projectAlertListItem(
      wireAlertListItem({ amountCents: "184500", riskScoreSnapshot: "100" }),
    );

    expect(projected?.amountCents).toBe(184_500);
    expect(projected?.riskScoreSnapshot).toBe(100);
  });

  it("no acepta una cadena que solo empieza con dígitos", () => {
    expect(projectAlertListItem(wireAlertListItem({ amountCents: "184500 pesos" }))).toBeNull();
  });

  it("distingue un nulo legítimo de un fallo de proyección", () => {
    const projected = projectAlertDetail(
      wireAlertDetail({ currentEvaluation: null, currentRun: null, review: null }),
    );

    expect(projected).not.toBeNull();
    expect(projected?.currentEvaluation).toBeNull();
    expect(projected?.currentRun).toBeNull();
    expect(projected?.review).toBeNull();
  });

  it("rechaza un objeto anidado inválido sin confundirlo con un nulo", () => {
    expect(projectAlertDetail(wireAlertDetail({ currentRun: { sequence: 3 } }))).toBeNull();
  });

  it("acepta un feed vacío", () => {
    const projected = projectAlertList(
      wireAlertList({ items: [], totalCount: 0, scoringRunSequence: null, currentRun: null }),
    );

    expect(projected?.items).toEqual([]);
    expect(projected?.scoringRunSequence).toBeNull();
    expect(projected?.currentRun).toBeNull();
  });
});
