import { IMPORT_MAX_FILE_BYTES, type Language } from "./contract";
import type { ApiFailure } from "./failures";
import { messagesFor } from "@/lib/i18n/dictionary";

/**
 * What the analyst reads when something fails, and what she can do next.
 *
 * Two rules the design fixes and this module enforces. First, the main text is the console's own
 * and actionable: "Error 409" is a fact about HTTP, not about the alert, and the API's `detail` is
 * written for whoever integrates against it. `detail` still travels, as secondary information,
 * because it is often the only thing that says *which* value was rejected. Second, every code the
 * API can emit has its own text: a shared "algo salió mal" would hide precisely the difference
 * between "otra persona ya emitió un veredicto" and "el corpus cambió bajo tus pies".
 *
 * <h3>What stayed here when the words left</h3>
 *
 * The sentences live in the dictionaries; this module keeps the two things that are facts about the
 * console rather than about a language. {@link FORM_ERRORS} is where a message belongs on screen —
 * beside a field or at the top of the page — which does not change with the reader. And the list of
 * codes is here, in one place, because its exactness test asserts that this catalogue is precisely
 * what the endpoints emit: decision 57. A code that is the value of a field inside a `200` is
 * labelled in `format.ts` instead, and never enters here.
 */

/**
 * The codes whose message belongs beside a form field.
 *
 * A set rather than a flag per entry, so that adding a code to the dictionary cannot silently
 * decide where its message renders.
 */
const FORM_ERRORS: ReadonlySet<string> = new Set([
  "INVALID_STATUS",
  "NOTE_TOO_LONG",
  "FILE_REQUIRED",
  "FILE_TOO_LARGE",
  "TOO_MANY_RECORDS",
  "UNSUPPORTED_FORMAT",
  "EMPTY_FILE",
  "INVALID_ENCODING",
  "INVALID_CSV",
  "INVALID_JSON",
  "INVALID_JSON_ROOT",
  "MISSING_HEADER",
  "INVALID_HEADER",
  "DUPLICATE_HEADER",
  "UNKNOWN_HEADER",
]);

export interface FailureMessage {
  /** One line, in the analyst's language, naming what happened. */
  readonly title: string;
  /** What it means for the alert in front of her. */
  readonly body: string;
  /** The way forward. */
  readonly recovery: string;
  /** Whether the message belongs beside a form field rather than at the top of the page. */
  readonly isFormError: boolean;
}

const IMPORT_MAX_FILE_MIB = IMPORT_MAX_FILE_BYTES / (1024 * 1024);

export function isKnownFailureCode(code: string): boolean {
  return Object.hasOwn(messagesFor("es").failures, code);
}

export function knownFailureCodes(): readonly string[] {
  return Object.keys(messagesFor("es").failures);
}

export function describeFailure(failure: ApiFailure, language: Language): FailureMessage {
  const { transport } = messagesFor(language);

  switch (failure.kind) {
    case "timeout":
      return {
        title: transport.timeoutTitle,
        body: transport.timeoutBody,
        recovery: transport.timeoutRecovery,
        isFormError: false,
      };
    case "unreachable":
      return {
        title: transport.unreachableTitle,
        body: transport.unreachableBody,
        recovery: transport.unreachableRecovery,
        isFormError: false,
      };
    case "malformed":
      return {
        title: transport.malformedTitle,
        body: transport.malformedBody,
        recovery: transport.malformedRecovery,
        isFormError: false,
      };
    case "problem":
      return describeProblem(failure, language);
  }
}

function describeProblem(
  failure: Extract<ApiFailure, { kind: "problem" }>,
  language: Language,
): FailureMessage {
  const t = messagesFor(language);
  const known = failure.code === null ? undefined : lookup(t.failures, failure.code);

  if (known !== undefined && failure.code !== null) {
    return { ...known, isFormError: FORM_ERRORS.has(failure.code) };
  }

  return {
    title: t.transport.unknownTitle,
    body: t.transport.unknownBody(String(failure.status)),
    recovery: t.transport.unknownRecovery,
    isFormError: false,
  };
}

/**
 * One entry of the catalogue, with the single parameterised body resolved.
 *
 * `FILE_TOO_LARGE` names the ceiling the contract fixes, so its body is a function of it rather
 * than a sentence with the number written into two dictionaries by hand.
 */
function lookup(
  failures: ReturnType<typeof messagesFor>["failures"],
  code: string,
): { title: string; body: string; recovery: string } | undefined {
  const entry = (
    failures as Record<string, { title: string; body: unknown; recovery: string } | undefined>
  )[code];

  if (entry === undefined) {
    return undefined;
  }

  return {
    title: entry.title,
    recovery: entry.recovery,
    body:
      typeof entry.body === "function"
        ? (entry.body as (maxMib: string) => string)(String(IMPORT_MAX_FILE_MIB))
        : (entry.body as string),
  };
}
