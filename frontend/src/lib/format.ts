/**
 * Formatting with a fixed locale and a fixed time zone.
 *
 * Both are pinned on purpose. The time zone is `America/Montevideo`, the same
 * `RuleConfig.BusinessTimeZone` the rules use to decide what day an order happened on: a console
 * that rendered instants in the machine's zone would disagree with the engine about the date of the
 * very order it is describing. The locale is pinned for the same class of reason plus a practical
 * one — tests must not depend on the machine they run on. Node 24.20.0 ships full ICU, so both are
 * available without extra data.
 */

const LOCALE = "es-UY";
export const BUSINESS_TIME_ZONE = "America/Montevideo";

// 24-hour time, stated rather than inherited: an operations console should not make an analyst
// resolve "8:41 p. m." against a timestamp she is comparing with an audit log.
const instantFormatter = new Intl.DateTimeFormat(LOCALE, {
  timeZone: BUSINESS_TIME_ZONE,
  dateStyle: "medium",
  timeStyle: "short",
  hour12: false,
});

const dateFormatter = new Intl.DateTimeFormat(LOCALE, {
  timeZone: BUSINESS_TIME_ZONE,
  dateStyle: "medium",
});

/** An ISO instant as a date and time in business time. */
export function formatInstant(isoInstant: string): string {
  return instantFormatter.format(new Date(isoInstant));
}

/** An ISO instant as a date in business time, for values where the time of day adds nothing. */
export function formatDate(isoInstant: string): string {
  return dateFormatter.format(new Date(isoInstant));
}

/**
 * Minor units as an amount in its own currency.
 *
 * Currencies are never converted or added together anywhere in the console: a single figure across
 * BRL, USD and UYU is a number without a unit. The currency code travels with every amount.
 */
export function formatAmount(amountCents: number, currencyCode: string): string {
  return new Intl.NumberFormat(LOCALE, {
    style: "currency",
    currency: currencyCode,
    currencyDisplay: "code",
  }).format(amountCents / 100);
}

const SEVERITY_LABELS: Readonly<Record<string, string>> = {
  CRITICAL: "CRÍTICA",
  HIGH: "ALTA",
  MEDIUM: "MEDIA",
};

/**
 * The severity in words. Colour alone never carries it: a band is the difference between reviewing
 * an alert now and reviewing it tomorrow, and that cannot depend on distinguishing two hues.
 */
export function severityLabel(severity: string): string {
  return SEVERITY_LABELS[severity] ?? severity;
}

const STATUS_LABELS: Readonly<Record<string, string>> = {
  OPEN: "Abierta",
  CONFIRMED_SAFE: "Confirmada segura",
  REPORTED_FRAUD: "Fraude reportado",
};

export function statusLabel(status: string): string {
  return STATUS_LABELS[status] ?? status;
}

/**
 * The six deterministic rules of `RiskRuleNames`, in their canonical order.
 *
 * `amount_anomaly` is deliberately silent about *whose* median the amount was compared against.
 * `TemporalRiskEngine` uses the buyer's median when the buyer has enough history and falls back to
 * the merchant's when they do not, and it says which one in the signal's own `detail`. A title that
 * named the buyer would contradict the sentence right below it on every order that fell back.
 */
const RULE_LABELS: Readonly<Record<string, string>> = {
  amount_anomaly: "Monto atípico",
  velocity: "Ráfaga de pedidos",
  cross_border_velocity: "Ráfaga entre países",
  unusual_hour: "Hora inusual",
  new_buyer_high_value: "Comprador sin historia y monto alto",
  foreign_country: "País distinto del habitual",
};

/** The rule name in the analyst's language; an unmapped rule shows its own identifier. */
export function ruleLabel(rule: string): string {
  return RULE_LABELS[rule] ?? rule;
}

/**
 * A ratio as a percentage. Rates are shown with one decimal because the corpus is small enough that
 * rounding to whole points would collapse distinct runs onto the same figure.
 */
const percentFormatter = new Intl.NumberFormat(LOCALE, {
  style: "percent",
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});

export function formatPercent(ratio: number): string {
  return percentFormatter.format(ratio);
}

const integerFormatter = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 0 });

/** A count with the thousands separator of the fixed locale. */
export function formatCount(value: number): string {
  return integerFormatter.format(value);
}

/**
 * A `YYYY-MM-DD` calendar date, rendered without going through an instant.
 *
 * `DateOnly` has no time and no zone; parsing it as a `Date` would place it at midnight UTC and the
 * business time zone would then shift it a day back for anybody west of Greenwich, which is exactly
 * where this console lives. Splitting the string keeps the day the API meant.
 */
export function formatCalendarDate(calendarDate: string): string {
  const [year, month, day] = calendarDate.split("-").map(Number);

  return dateFormatter.format(new Date(year ?? 0, (month ?? 1) - 1, day ?? 1));
}

/**
 * The per-record codes an import can report, in the analyst's language.
 *
 * They describe a row of her file rather than a failure of the request, so they never travel through
 * the failure catalogue: an import that rejects forty rows and writes the rest is a success with a
 * list of rejections attached.
 */
const IMPORT_ERROR_LABELS: Readonly<Record<string, string>> = {
  REQUIRED: "Falta un campo obligatorio",
  INVALID_FORMAT: "El valor no tiene el formato esperado",
  OUT_OF_RANGE: "El valor está fuera del rango admitido",
  UNSUPPORTED_VALUE: "El valor no es uno de los admitidos",
  REFERENCE_CONFLICT: "La referencia ya existe con otros datos",
};

export function importErrorLabel(code: string): string {
  return IMPORT_ERROR_LABELS[code] ?? code;
}

/**
 * The state of an external evaluation, in words.
 *
 * `PENDING` is never rendered as "pendiente" on its own. Three different things are pending in this
 * console — an alert waiting for a human verdict, an external evaluation waiting for the provider,
 * and an order waiting to be scored — and a screen that called all three the same word would be
 * unreadable at exactly the moment an analyst needs to know which one is holding things up.
 */
const EXTERNAL_STATUS_LABELS: Readonly<Record<string, string>> = {
  PENDING: "Esperando al proveedor",
  APPROVED: "Aprobado por el proveedor",
  DENIED: "Denegado por el proveedor",
  ERROR: "La consulta al proveedor falló",
};

export function externalStatusLabel(status: string): string {
  return EXTERNAL_STATUS_LABELS[status] ?? status;
}

/**
 * How a verdict got here. Provenance is part of the record: an answer that arrived on its own and
 * one this API went looking for are not the same fact about the integration.
 */
const EXTERNAL_SOURCE_LABELS: Readonly<Record<string, string>> = {
  SYNC: "en la misma respuesta del proveedor",
  CALLBACK: "por callback del proveedor",
  RECONCILIATION: "al reconciliar, preguntándole de nuevo",
};

export function externalSourceLabel(source: string): string {
  return EXTERNAL_SOURCE_LABELS[source] ?? source;
}

/** The closed catalogue of `ExternalEvaluationErrorCode`, sanitised at the border and named here. */
const EXTERNAL_ERROR_LABELS: Readonly<Record<string, string>> = {
  UNREACHABLE: "No se pudo contactar al proveedor: la consulta nunca salió",
  PROVIDER_REJECTED: "El proveedor rechazó la consulta",
  TIMEOUT: "El proveedor no respondió a tiempo",
  PROVIDER_ERROR: "El proveedor respondió con un error",
  INVALID_RESPONSE: "La respuesta del proveedor no se pudo leer",
};

export function externalErrorLabel(code: string): string {
  return EXTERNAL_ERROR_LABELS[code] ?? code;
}

/**
 * Why an explanation ended without text, in the analyst's language.
 *
 * These are values of `failureCode` inside a `200`, not rejections of a request, so they live here
 * beside the other wire values the console renders and never travel through the failure catalogue —
 * the same split `externalErrorLabel` makes for exactly the same reason.
 *
 * Two of them describe a rejection this system performed on its own provider, and they say so. An
 * explanation whose figure was not backed by the evaluation is not a glitch to apologise for: it is
 * the verification working, and the analyst reading the block is entitled to know that the text was
 * withheld on purpose rather than lost.
 */
const EXPLANATION_FAILURE_LABELS: Readonly<Record<string, string>> = {
  PROVIDER_UNAVAILABLE: "No se pudo redactar: el proveedor falló antes de responder",
  PROVIDER_TIMEOUT: "El proveedor no respondió dentro del tiempo permitido",
  PROVIDER_REFUSED: "El proveedor respondió sin texto",
  MALFORMED_OUTPUT: "El texto devuelto no era utilizable: vino vacío o con marcado",
  NOT_GROUNDED_NUMBER:
    "El texto traía una cifra que la evaluación no respalda, así que se descartó entero",
  NOT_GROUNDED_RULE:
    "El texto nombraba una regla que esta evaluación no disparó, así que se descartó entero",
  TOO_LONG: "El texto superó el largo máximo admitido",
  CANCELLED: "La petición se abandonó antes de que el proveedor respondiera",
  ATTEMPT_LIMIT_REACHED: "Se agotaron los intentos de redacción para esta evaluación",
};

export function explanationFailureLabel(code: string): string {
  return EXPLANATION_FAILURE_LABELS[code] ?? code;
}

/**
 * Who wrote the explanation. Kept apart from `providerLabel`, which names antifraud providers: the
 * two catalogues share no value and merging them would let a future `ANTHROPIC` be read as somebody
 * who might have decided something about the order.
 */
const EXPLANATION_PROVIDER_LABELS: Readonly<Record<string, string>> = {
  MOCK: "plantilla determinista",
  ANTHROPIC: "Anthropic",
};

export function explanationProviderLabel(provider: string): string {
  return EXPLANATION_PROVIDER_LABELS[provider] ?? provider;
}

/** The provider an evaluation was asked of. */
const PROVIDER_LABELS: Readonly<Record<string, string>> = {
  EXTERNAL_MOCK: "Proveedor simulado",
  KOIN_SANDBOX: "Koin sandbox",
};

export function providerLabel(provider: string): string {
  return PROVIDER_LABELS[provider] ?? provider;
}
