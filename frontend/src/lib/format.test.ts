import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

import { projectSignal } from "@/lib/api/guards";
import { ruleLabel, signalSentence } from "@/lib/format";
import { wireLegacySignal, wireSignal } from "@/test/fixtures";

/**
 * A signal has to be readable without an interpreter.
 *
 * That is a rule of the domain rather than a preference of this module, and it was rewritten when
 * `e3-v2` stopped writing sentences: a signal is read by its `detail` while it is `e3-v1` prose and
 * by its named fields afterwards. A rule that says «legible» and has no test is an intention, so
 * this is the test — and the six signals it composes are taken out of the capture the engine
 * produced over the demo corpus, not invented here.
 */

/** One real signal of every rule, as `e3-v2` stored it over the demo corpus. */
const CAPTURE = "../../../backend/tests/Salvo.Api.IntegrationTests/Goldens/signal-facts.v2.json";

type CapturedSignal = Record<string, unknown> & { readonly rule: string };
type CapturedEntry = { readonly reference: string; readonly signals: readonly CapturedSignal[] };

function captured(): ReadonlyMap<string, { reference: string; signal: CapturedSignal }> {
  const entries = JSON.parse(readFileSync(new URL(CAPTURE, import.meta.url), "utf8")) as CapturedEntry[];
  const first = new Map<string, { reference: string; signal: CapturedSignal }>();

  for (const entry of entries) {
    for (const signal of entry.signals) {
      if (!first.has(signal.rule)) {
        first.set(signal.rule, { reference: entry.reference, signal });
      }
    }
  }

  return first;
}

/**
 * The capture omits the fields a rule does not use; the API sends every one of them as `null`.
 * `detail` goes too: the engine writes no prose, and the capture keeps it only as the record of
 * where the numbers were read from.
 */
function asTheApiSendsIt(signal: CapturedSignal): Record<string, unknown> {
  const wire: Record<string, unknown> = {};
  for (const key of Object.keys(wireSignal())) {
    wire[key] = signal[key] ?? null;
  }

  return { ...wire, rule: signal.rule, weight: signal.weight, detail: null };
}

describe("la frase de una señal", () => {
  const rows = captured();

  it.each([
    [
      "amount_anomaly",
      "ORD_000011",
      "El monto, BRL\u00a0507,86, es 3,4 veces la mediana del comercio, BRL\u00a0149,37, calculada sobre "
      + "3 pedidos previos de los últimos 90 días.",
    ],
    [
      "velocity",
      "ORD_000160",
      "Hubo 4 pedidos del mismo comprador dentro de 10 minutos; el umbral es 4.",
    ],
    [
      "cross_border_velocity",
      "ORD_000112",
      "El país cambió de BR a UY en 90 minutos, con el mismo comercio y el mismo comprador.",
    ],
    [
      "unusual_hour",
      "ORD_000160",
      "La franja de 00:00 a 06:00, hora del comercio, aparece en 0 de 25 pedidos previos del "
      + "comercio: un 0 %.",
    ],
    [
      "new_buyer_high_value",
      "ORD_000011",
      "El comprador no tenía pedidos previos con este comercio, y el monto, BRL\u00a0507,86, es 3,4 "
      + "veces la mediana del comercio, BRL\u00a0149,37, sobre 3 pedidos previos.",
    ],
    [
      "foreign_country",
      "ORD_000011",
      "El país del pedido, AR, difiere del habitual del comercio, BR, observado en 3 de 3 pedidos "
      + "previos: un 100 %.",
    ],
  ])("se compone entera desde los campos de %s", (rule, reference, expected) => {
    const row = rows.get(rule);
    expect(row?.reference).toBe(reference);

    const projected = projectSignal(asTheApiSendsIt(row!.signal));
    expect(projected).not.toBeNull();

    const sentence = signalSentence(projected!);
    expect(sentence).toBe(expected);
    // Nothing of the engine's own vocabulary survives into what an analyst reads.
    expect(sentence).not.toContain(rule);
    expect(ruleLabel(rule)).not.toBe(rule);
  });

  it("cubre las seis reglas del motor", () => {
    expect([...rows.keys()].sort()).toEqual([
      "amount_anomaly",
      "cross_border_velocity",
      "foreign_country",
      "new_buyer_high_value",
      "unusual_hour",
      "velocity",
    ]);
  });

  /**
   * An `e3-v1` snapshot keeps the sentence it was stored with. It is the premise a verdict was
   * formed on and it is never rewritten, so it is shown as written or not at all.
   */
  it("muestra la prosa heredada cuando la fila es e3-v1", () => {
    const projected = projectSignal(wireLegacySignal());

    expect(signalSentence(projected!)).toBe(
      "201111 BRL cents is 23.2x the buyer median 8685 over 3 prior orders in 90 days.",
    );
  });

  /**
   * A rule this build has never heard of still shows whatever the row says, rather than an empty
   * line where a signal should be.
   */
  it("cae a la prosa cuando la regla es desconocida", () => {
    const projected = projectSignal(
      wireLegacySignal({ rule: "regla_futura", detail: "Algo que este build no sabe componer." }),
    );

    expect(signalSentence(projected!)).toBe("Algo que este build no sabe componer.");
  });
});
