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

const instantFormatter = new Intl.DateTimeFormat(LOCALE, {
  timeZone: BUSINESS_TIME_ZONE,
  dateStyle: "medium",
  timeStyle: "short",
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

/** The six deterministic rules of `RiskRuleNames`, in their canonical order. */
const RULE_LABELS: Readonly<Record<string, string>> = {
  amount_anomaly: "Monto atípico para el comprador",
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
