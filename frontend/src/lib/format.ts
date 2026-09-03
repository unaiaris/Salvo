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
