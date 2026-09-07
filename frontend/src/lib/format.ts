/**
 * Formatting and vocabulary, both in the language of the deployment.
 *
 * <h3>What is pinned and what is not</h3>
 *
 * The **time zone** is fixed at `America/Montevideo` in every language: it is
 * `RuleConfig.BusinessTimeZone`, the same zone the rules use to decide what day an order happened
 * on, and a console that rendered instants in the machine's zone would disagree with the engine
 * about the date of the very order it is describing. Language does not move it.
 *
 * The **locale** does follow the language, because that is what a locale is for. Both are pinned
 * rather than taken from the browser, so a test never depends on the machine it runs on. Node
 * 24.20.0 ships full ICU, so both are available without extra data.
 *
 * <h3>Why a factory</h3>
 *
 * Every function here either formats a number in a locale or looks a word up in a dictionary, and
 * both need the language. Threading it through each call would put it in the signature of every
 * label in the console; asking for it once per component and getting the whole vocabulary back
 * keeps the call sites reading as they did. It is a plain object of functions, built on demand and
 * never crossing to a client component — what crosses is the language, which is a string.
 */

import type { AlertSignal, Language } from "@/lib/api/contract";
import { messagesFor, type Dictionary } from "@/lib/i18n/dictionary";

/**
 * The same zone the engine reasons in. Not a locale and not language-dependent: an instant belongs
 * to the day the rules say it does, whoever is reading.
 */
export const BUSINESS_TIME_ZONE = "America/Montevideo";

/**
 * Uruguayan Spanish and Brazilian Portuguese. Both group thousands with a point and separate
 * decimals with a comma — which is why `ExplanationNumberFormat` in the backend needs only one
 * implementation — but they name months and order dates differently, and that is what this picks.
 */
const LOCALES: Readonly<Record<Language, string>> = { es: "es-UY", pt: "pt-BR" };

export interface Formatting {
  readonly language: Language;
  readonly t: Dictionary;

  /** An ISO instant as a date and time in business time. */
  formatInstant(isoInstant: string): string;
  /** An ISO instant as a date, for values where the time of day adds nothing. */
  formatDate(isoInstant: string): string;
  /** Minor units as an amount in its own currency. Currencies are never added together. */
  formatAmount(amountCents: number, currencyCode: string): string;
  /** A count with the thousands separator of the locale. */
  formatCount(value: number): string;
  /** A ratio as a percentage, with one decimal. */
  formatPercent(ratio: number): string;
  /** A `YYYY-MM-DD` calendar date, rendered without going through an instant. */
  formatCalendarDate(calendarDate: string): string;

  severityLabel(severity: string): string;
  statusLabel(status: string): string;
  ruleLabel(rule: string): string;
  signalSentence(signal: AlertSignal): string;
  importErrorLabel(code: string): string;
  externalStatusLabel(status: string): string;
  externalSourceLabel(source: string): string;
  externalErrorLabel(code: string): string;
  explanationFailureLabel(code: string): string;
  explanationProviderLabel(provider: string): string;
  providerLabel(provider: string): string;
}

const CACHE = new Map<Language, Formatting>();

/**
 * Everything this console needs to write a value down, in one language.
 *
 * Memoised per language because `Intl` formatters are expensive to build and there are exactly two
 * possible results. The object is immutable and holds no request state.
 */
export function formatting(language: Language): Formatting {
  const cached = CACHE.get(language);
  if (cached !== undefined) {
    return cached;
  }

  const built = build(language);
  CACHE.set(language, built);

  return built;
}

function build(language: Language): Formatting {
  const t = messagesFor(language);
  const locale = LOCALES[language];

  // 24-hour time, stated rather than inherited: an operations console should not make an analyst
  // resolve "8:41 p. m." against a timestamp she is comparing with an audit log.
  const instantFormatter = new Intl.DateTimeFormat(locale, {
    timeZone: BUSINESS_TIME_ZONE,
    dateStyle: "medium",
    timeStyle: "short",
    hour12: false,
  });
  const dateFormatter = new Intl.DateTimeFormat(locale, {
    timeZone: BUSINESS_TIME_ZONE,
    dateStyle: "medium",
  });
  // Rates carry one decimal because the corpus is small enough that whole points would collapse
  // distinct runs onto the same figure.
  const percentFormatter = new Intl.NumberFormat(locale, {
    style: "percent",
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  });
  const integerFormatter = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });

  const formatAmount = (amountCents: number, currencyCode: string): string =>
    new Intl.NumberFormat(locale, {
      style: "currency",
      currency: currencyCode,
      currencyDisplay: "code",
    }).format(amountCents / 100);

  const formatCount = (value: number): string => integerFormatter.format(value);

  /**
   * A measured number with at most as many decimals as its field carries, and with none when every
   * one of them would be a zero. «4 veces» rather than «4,0 veces»: the trailing zero claims a
   * measurement to the tenth that the ratio does not have.
   */
  const measured = (value: number, decimals: number): string =>
    new Intl.NumberFormat(locale, { maximumFractionDigits: decimals }).format(value);

  /** An hour of the day as a clock reads it. */
  const clock = (hour: number): string => `${hour < 10 ? "0" : ""}${String(hour)}:00`;

  const label = (catalogue: Readonly<Record<string, string>>, key: string): string =>
    catalogue[key] ?? key;

  return {
    language,
    t,

    formatInstant: (isoInstant) => instantFormatter.format(new Date(isoInstant)),
    formatDate: (isoInstant) => dateFormatter.format(new Date(isoInstant)),
    formatAmount,
    formatCount,
    formatPercent: (ratio) => percentFormatter.format(ratio),

    /**
     * `DateOnly` has no time and no zone; parsing it as a `Date` would place it at midnight UTC and
     * the business time zone would then shift it a day back for anybody west of Greenwich, which is
     * exactly where this console lives. Splitting the string keeps the day the API meant.
     */
    formatCalendarDate: (calendarDate) => {
      const [year, month, day] = calendarDate.split("-").map(Number);

      return dateFormatter.format(new Date(year ?? 0, (month ?? 1) - 1, day ?? 1));
    },

    /**
     * The severity in words. Colour alone never carries it: a band is the difference between
     * reviewing an alert now and reviewing it tomorrow, and that cannot depend on two hues.
     */
    severityLabel: (severity) => label(t.severity, severity),
    statusLabel: (status) => label(t.status, status),
    ruleLabel: (rule) => label(t.rules, rule),
    importErrorLabel: (code) => label(t.importErrors, code),
    externalStatusLabel: (status) => label(t.externalStatus, status),
    externalSourceLabel: (source) => label(t.externalSource, source),
    externalErrorLabel: (code) => label(t.externalError, code),
    explanationFailureLabel: (code) => label(t.explanationFailure, code),
    explanationProviderLabel: (provider) => label(t.explanationProvider, provider),
    providerLabel: (provider) => label(t.provider, provider),

    /**
     * What a signal says, in words, composed from the fields the engine measured.
     *
     * A signal stored by `e3-v1` carries no fields and its own English sentence instead. It is
     * returned as it stands: the snapshot of an alert opened under that version is never rewritten,
     * so it is either shown as written or not shown at all. Which is also why it is not translated
     * — translating a stored sentence would be inventing a record nobody wrote.
     */
    signalSentence: (signal) => {
      const s = t.signals;

      switch (signal.rule) {
        case "amount_anomaly":
          return absent(
            signal.amountCents,
            signal.currencyCode,
            signal.ratio,
            signal.medianCents,
            signal.historyCount,
            signal.windowDays,
          )
            ? legacy(signal)
            : s.amountAnomaly(
                formatAmount(signal.amountCents!, signal.currencyCode!),
                measured(signal.ratio!, 1),
                signal.scope === "buyer" ? s.scopeBuyer : s.scopeMerchant,
                formatAmount(signal.medianCents!, signal.currencyCode!),
                formatCount(signal.historyCount!),
                formatCount(signal.windowDays!),
              );

        case "velocity":
          return absent(signal.orderCount, signal.windowMinutes, signal.threshold)
            ? legacy(signal)
            : s.velocity(
                formatCount(signal.orderCount!),
                formatCount(signal.windowMinutes!),
                formatCount(signal.threshold!),
              );

        case "cross_border_velocity":
          return absent(signal.fromCountry, signal.toCountry, signal.elapsedMinutes)
            ? legacy(signal)
            : s.crossBorderVelocity(
                signal.fromCountry!,
                signal.toCountry!,
                measured(signal.elapsedMinutes!, 2),
              );

        case "unusual_hour":
          return absent(
            signal.bucketStartHour,
            signal.bucketEndHour,
            signal.observedCount,
            signal.totalCount,
            signal.sharePercent,
          )
            ? legacy(signal)
            : s.unusualHour(
                clock(signal.bucketStartHour!),
                clock(signal.bucketEndHour!),
                formatCount(signal.observedCount!),
                formatCount(signal.totalCount!),
                measured(signal.sharePercent!, 1),
              );

        case "new_buyer_high_value":
          return absent(
            signal.amountCents,
            signal.currencyCode,
            signal.medianCents,
            signal.historyCount,
            signal.ratio,
          )
            ? legacy(signal)
            : s.newBuyerHighValue(
                formatAmount(signal.amountCents!, signal.currencyCode!),
                measured(signal.ratio!, 1),
                formatAmount(signal.medianCents!, signal.currencyCode!),
                formatCount(signal.historyCount!),
              );

        case "foreign_country":
          return absent(
            signal.country,
            signal.habitualCountry,
            signal.observedCount,
            signal.totalCount,
            signal.sharePercent,
          )
            ? legacy(signal)
            : s.foreignCountry(
                signal.country!,
                signal.habitualCountry!,
                formatCount(signal.observedCount!),
                formatCount(signal.totalCount!),
                measured(signal.sharePercent!, 1),
              );

        default:
          return legacy(signal);
      }
    },
  };
}

/** Whether any field the sentence needs is missing, which is what an `e3-v1` signal looks like. */
function absent(...fields: readonly (number | string | null)[]): boolean {
  return fields.some((field) => field === null);
}

/** The sentence `e3-v1` wrote, shown exactly as the engine wrote it, or nothing at all. */
function legacy(signal: AlertSignal): string {
  return signal.detail ?? "";
}
