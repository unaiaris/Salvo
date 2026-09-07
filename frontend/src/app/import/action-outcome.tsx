"use client";

import type { Language } from "@/lib/api/contract";
import { messagesFor, type Dictionary } from "@/lib/i18n/dictionary";
import type { ActionState } from "./action-state";

/**
 * What an attempt produced, announced to a screen reader as soon as it settles.
 *
 * The three forms of this page render the same component because they report the same thing, and
 * because a failure here has to read like a failure anywhere else in the console: the console's own
 * sentence first, the way forward second, the API's `detail` last and only as technical background.
 */
export function ActionOutcome({
  state,
  language,
}: {
  readonly state: ActionState;
  readonly language: Language;
}) {
  const t = messagesFor(language);

  if (state.outcome === "idle") {
    return null;
  }

  const failed = state.outcome === "failed";

  /*
    El rol depende de cómo salió la acción, y no es un detalle de estilo.

    `alert` es asertivo: interrumpe lo que el lector esté diciendo en ese momento. Es lo correcto
    para un fallo, que hay que oír antes de seguir, y es de más para un éxito, que solo hay que
    saber. Un éxito va en `status`, que espera a que el lector termine la frase en curso.
  */
  return (
    <section
      role={failed ? "alert" : "status"}
      className={
        failed
          ? "rounded-lg border border-rose-300 bg-rose-50 p-4 text-rose-950"
          : "rounded-lg border border-emerald-300 bg-emerald-50 p-4 text-emerald-950"
      }
    >
      <h3 className="text-sm font-semibold">{state.title}</h3>
      <p className="mt-1 text-sm leading-6">{state.body}</p>
      {state.recovery !== "" && (
        <p className="mt-1 text-sm font-medium leading-6">{state.recovery}</p>
      )}

      {state.facts.length > 0 && (
        <ul className="mt-3 flex flex-col gap-1 text-sm">
          {state.facts.map((fact) => (
            <li key={fact}>{fact}</li>
          ))}
        </ul>
      )}

      {state.recordErrors.length > 0 && <RecordErrors state={state} t={t} />}

      {state.technicalDetail !== "" && (
        <p className="mt-3 border-t border-current/20 pt-3 text-xs opacity-80">
          {t.common.technicalDetail(state.technicalDetail)}
        </p>
      )}
    </section>
  );
}

/**
 * The rows the import refused, one sentence each.
 *
 * They arrive already written: the code that names the failure is a domain value and the analyst
 * reads a translation of it, decided on the server. The truncation notice is not optional — the API
 * caps the list, and a shorter list read as the whole truth is worse than no list.
 */
function RecordErrors({ state, t }: { readonly state: ActionState; readonly t: Dictionary }) {
  return (
    <div className="mt-4 border-t border-current/20 pt-3">
      <h4 className="text-sm font-semibold">
        {t.importPage.rejectedTitle(String(state.recordErrors.length))}
      </h4>
      <p className="mt-1 text-xs">{t.importPage.rejectedHint}</p>
      <ul className="mt-2 flex max-h-80 flex-col gap-1 overflow-y-auto text-sm">
        {state.recordErrors.map((error) => (
          <li key={error} className="font-mono text-xs leading-5">
            {error}
          </li>
        ))}
      </ul>
      {state.errorsTruncated && (
        <p className="mt-2 text-sm font-medium">{t.importPage.rejectedTruncated}</p>
      )}
    </div>
  );
}
