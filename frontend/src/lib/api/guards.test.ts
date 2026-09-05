import { describe, expect, it } from "vitest";

import {
  wireAlertDetail,
  wireAlertList,
  wireAlertListItem,
  wireCapabilities,
  wireDashboard,
  wireEvaluationMetrics,
  wireExplanation,
  wireImportResult,
  wireOrder,
  wireScoringRunSummary,
  wireSeedResult,
  wireSignal,
} from "@/test/fixtures";
import {
  projectAlertDetail,
  projectAlertList,
  projectAlertListItem,
  projectCapabilities,
  projectDashboard,
  projectEvaluationMetrics,
  projectImportResult,
  projectOrder,
  projectScoringRunSummary,
  projectSeedResult,
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

describe("guardas del dashboard, las métricas y la importación", () => {
  it("proyecta el dashboard completo y descarta lo que no declara el contrato", () => {
    const projected = projectDashboard(
      wireDashboard({ estimatedLoss: 999, labelSource: "order_evaluation_labels" }),
    );

    expect(projected).not.toBeNull();
    expect(Object.keys(projected ?? {})).toEqual([
      "scoringRun",
      "ordersPendingScoring",
      "openAlerts",
      "amountAtRisk",
      "reportedFraud",
      "flagRate",
      "riskOverTime",
      "topSignals",
    ]);
  });

  it("acepta una tasa de marcado en forma de cadena decimal", () => {
    // `decimal` viaja como `number | string` igual que los enteros; una tasa se formatea, nunca se
    // suma, así que se guarda como número.
    expect(projectDashboard(wireDashboard({ flagRate: "0.0625" }))?.flagRate).toBe(0.0625);
  });

  it("distingue una tasa nula de una tasa ilegible", () => {
    expect(projectDashboard(wireDashboard({ flagRate: null }))?.flagRate).toBeNull();
    expect(projectDashboard(wireDashboard({ flagRate: "muchísimo" }))).toBeNull();
  });

  it("acepta un corpus sin corrida y sin alertas", () => {
    const projected = projectDashboard(
      wireDashboard({
        scoringRun: null,
        ordersPendingScoring: 300,
        openAlerts: { total: 0, bySeverity: [] },
        amountAtRisk: [],
        reportedFraud: [],
        flagRate: null,
        riskOverTime: [],
        topSignals: [],
      }),
    );

    expect(projected?.scoringRun).toBeNull();
    expect(projected?.ordersPendingScoring).toBe(300);
    expect(projected?.amountAtRisk).toEqual([]);
  });

  it("exige que la semana sea una fecha de calendario, no un instante", () => {
    // `DateOnly` no tiene hora ni zona. Aceptar un instante haría que el gráfico y el motor
    // discreparan sobre a qué día pertenece un pedido.
    const withInstant = wireDashboard({
      riskOverTime: [{ weekStart: "2026-08-10T00:00:00Z", orderCount: 1, flaggedCount: 0 }],
    });

    expect(projectDashboard(withInstant)).toBeNull();
  });

  it("proyecta las métricas con su barrido y su holdout", () => {
    const projected = projectEvaluationMetrics(wireEvaluationMetrics());

    expect(projected?.calibrationSweep).toHaveLength(2);
    expect(projected?.selectedThreshold.threshold).toBe(60);
    expect(projected?.holdout.matrix.truePositives).toBe(6);
    // El barrido trae decimales en forma de cadena en la primera fila.
    expect(projected?.calibrationSweep[0]?.metrics.precision).toBe(0.75);
  });

  it("acepta métricas sin definir cuando no hay con qué calcularlas", () => {
    const projected = projectEvaluationMetrics(
      wireEvaluationMetrics({
        holdout: {
          matrix: { truePositives: 0, falsePositives: 0, falseNegatives: 0, trueNegatives: 0 },
          precision: null,
          recall: null,
          f1: null,
          falsePositiveRate: null,
          flagRate: null,
        },
      }),
    );

    expect(projected?.holdout.precision).toBeNull();
  });

  it("proyecta el resultado de una importación con sus errores por fila", () => {
    const projected = projectImportResult(wireImportResult());

    expect(projected?.invalidRecordCount).toBe(2);
    expect(projected?.errors).toHaveLength(2);
    expect(projected?.errors[1]).toEqual({
      recordNumber: 11,
      lineNumber: null,
      field: null,
      code: "REFERENCE_CONFLICT",
      message: "The merchant reference already exists with different data.",
    });
  });

  it("rechaza un error de fila al que le falta el código", () => {
    const broken = wireImportResult({
      errors: [{ recordNumber: 1, lineNumber: 2, field: "amountCents", message: "sin código" }],
    });

    expect(projectImportResult(broken)).toBeNull();
  });

  it("proyecta el resumen de la corrida y el de la carga de demo", () => {
    expect(projectScoringRunSummary(wireScoringRunSummary())?.evaluationsReused).toBe(288);
    expect(projectSeedResult(wireSeedResult())?.insertedOrders).toBe(300);
  });

  it("proyecta las capacidades y rechaza una bandera que no es booleana", () => {
    expect(
      projectCapabilities(
        wireCapabilities({ demoDataEnabled: false, externalCallbackTriggerEnabled: false }),
      ),
    ).toEqual({ demoDataEnabled: false, externalCallbackTriggerEnabled: false });
    expect(projectCapabilities(wireCapabilities({ demoDataEnabled: "true" }))).toBeNull();
    expect(
      projectCapabilities(wireCapabilities({ externalCallbackTriggerEnabled: "true" })),
    ).toBeNull();
  });
});

describe("la guarda de la explicación mira el estado antes que el texto", () => {
  it("proyecta una explicación escrita y descarta lo que no declara el contrato", () => {
    const projected = projectAlertDetail(
      wireAlertDetail({
        explanation: wireExplanation({ ordenDeLaCasa: "no debería cruzar" }),
      }),
    );

    expect(projected?.explanation?.summary).toBe(
      "El pedido obtuvo 100 puntos sobre un umbral de 60.",
    );
    expect(projected?.explanation?.referencedRules).toEqual(["amount_anomaly"]);
    expect(Object.keys(projected?.explanation ?? {})).not.toContain("ordenDeLaCasa");
  });

  it("acepta una explicación fallida, que no trae texto", () => {
    const projected = projectAlertDetail(
      wireAlertDetail({
        explanation: wireExplanation({
          status: "FAILED",
          summary: null,
          referencedRules: [],
          failureCode: "NOT_GROUNDED_NUMBER",
        }),
      }),
    );

    expect(projected?.explanation?.status).toBe("FAILED");
    expect(projected?.explanation?.summary).toBeNull();
  });

  /**
   * The refusal this guard exists for.
   *
   * The API cannot send this: a database constraint keeps text and a failed status apart. What the
   * guard defends against is the day something else can — a different version of the API, a proxy,
   * a hand-written response — because the text on a failed explanation is precisely the text that
   * was never allowed to be stored. Dropping the field quietly would render the rest of a payload
   * that contradicts itself; refusing it is the honest answer.
   */
  it("rechaza un texto que viene con un estado que no lo admite", () => {
    const projected = projectAlertDetail(
      wireAlertDetail({
        explanation: wireExplanation({
          status: "FAILED",
          summary: "un resumen que la validación del backend rechazó",
          failureCode: "NOT_GROUNDED_NUMBER",
        }),
      }),
    );

    expect(projected).toBeNull();
  });

  it("rechaza también un texto sobre una explicación que todavía nadie respondió", () => {
    const projected = projectAlertDetail(
      wireAlertDetail({
        explanation: wireExplanation({
          status: "PENDING",
          summary: "texto que no puede existir todavía",
          settledAt: null,
        }),
      }),
    );

    expect(projected).toBeNull();
  });

  it("distingue una explicación ausente de una ilegible", () => {
    const absent = projectAlertDetail(wireAlertDetail({ explanation: null }));
    const unreadable = projectAlertDetail(
      wireAlertDetail({ explanation: wireExplanation({ status: 7 }) }),
    );

    expect(absent?.explanation).toBeNull();
    expect(unreadable).toBeNull();
  });

  it("registra en la revisión qué explicación tenía delante", () => {
    const projected = projectAlertDetail(
      wireAlertDetail({
        review: {
          id: "7f7b7f3e-0000-4000-8000-000000000007",
          previousStatus: "OPEN",
          newStatus: "CONFIRMED_SAFE",
          note: null,
          explanationId: "6f6b7f3e-0000-4000-8000-000000000006",
          reviewedAt: "2026-09-03T12:00:00+00:00",
        },
      }),
    );

    expect(projected?.review?.explanationId).toBe("6f6b7f3e-0000-4000-8000-000000000006");
  });
});
