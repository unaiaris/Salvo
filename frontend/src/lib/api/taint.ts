import * as React from "react";

type TaintObjectReference = (message: string | undefined, object: object) => void;

/**
 * Marks a raw API body so that React refuses to serialise it across the server–client boundary.
 *
 * `experimental.taint` swaps the app-directory React for the experimental channel, which is where
 * `experimental_taintObjectReference` lives; the pinned React 19.2.8 that Vitest resolves does not
 * export it. Reading the function off the namespace instead of importing it by name keeps the call a
 * no-op under test rather than a crash, without weakening it where it runs.
 *
 * Tainting tracks objects by reference, so it protects the raw body only. The projection the guards
 * build is a new object and is deliberately untainted: it is the shape the console is allowed to
 * read from. Taint is the backstop, not the mechanism — the mechanism is that client components
 * receive primitives.
 */
export function taintApiPayload(payload: unknown): void {
  if (typeof payload !== "object" || payload === null) {
    return;
  }

  const taint = (React as { experimental_taintObjectReference?: TaintObjectReference })
    .experimental_taintObjectReference;

  taint?.(
    "No pasar la respuesta cruda de la API a un componente cliente: proyectar y enviar primitivas.",
    payload,
  );
}
