"use client";

import { useActionState } from "react";
import { IMPORT_FORMATS } from "@/lib/api/contract";
import { importOrderFile } from "./actions";
import { ActionOutcome } from "./action-outcome";
import { INITIAL_ACTION_STATE } from "./action-state";

/**
 * The file import.
 *
 * It takes no props at all, which is the strongest form of the rule decision 42 fixes: nothing about
 * the corpus reaches this component, so nothing about the corpus can be serialised into the RSC
 * payload through it. The format is declared rather than sniffed because the API requires it and
 * because guessing from an extension would be wrong on the first `.txt` somebody exports.
 */
export function ImportForm({ maxFileMib }: { readonly maxFileMib: number }) {
  const [state, formAction, pending] = useActionState(importOrderFile, INITIAL_ACTION_STATE);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <label className="flex flex-col gap-1 text-sm">
        <span className="font-semibold text-slate-900">Archivo</span>
        <input
          type="file"
          name="file"
          accept=".csv,.json,text/csv,application/json"
          required
          aria-describedby="file-hint"
          className="rounded-md border border-slate-300 bg-white p-2 text-sm text-slate-900 file:mr-3 file:rounded file:border-0 file:bg-slate-900 file:px-3 file:py-1.5 file:text-sm file:font-semibold file:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        />
        <span id="file-hint" className="text-xs text-slate-600">
          Hasta {maxFileMib} MiB y 10.000 pedidos por archivo. Los datos deben ser sintéticos.
        </span>
      </label>

      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-semibold text-slate-900">Formato</legend>
        <div className="flex gap-4">
          {IMPORT_FORMATS.map((format, index) => (
            <label key={format} className="flex items-center gap-2 text-sm text-slate-800">
              <input
                type="radio"
                name="format"
                value={format}
                defaultChecked={index === 0}
                className="size-4 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
              />
              {format}
            </label>
          ))}
        </div>
      </fieldset>

      <div>
        <button
          type="submit"
          disabled={pending}
          className="inline-flex rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-400 hover:bg-slate-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-900"
        >
          {pending ? "Importando…" : "Importar pedidos"}
        </button>
      </div>

      <ActionOutcome state={state} />
    </form>
  );
}
