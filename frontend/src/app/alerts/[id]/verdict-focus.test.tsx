import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { VerdictFocus } from "./verdict-focus";

/**
 * Lo que pasa con el foco cuando el veredicto reemplaza al formulario.
 *
 * El panel se reproduce en su forma esencial: el vecino que no dibuja, y después el formulario o el
 * bloque del veredicto. Lo que la prueba mira es dónde queda `document.activeElement`, que es
 * exactamente lo que se pierde en el navegador y lo que ninguna regla de `axe-core` puede contestar.
 */
const VERDICT_ID = "recorded-verdict";

function Panel({ recorded }: { readonly recorded: boolean }) {
  return (
    <>
      <VerdictFocus recorded={recorded} targetId={VERDICT_ID} />
      {recorded ? (
        <section id={VERDICT_ID} tabIndex={-1} aria-labelledby="verdict-title">
          <h2 id="verdict-title">Veredicto registrado: Fraude reportado</h2>
        </section>
      ) : (
        <button type="submit">Registrar veredicto</button>
      )}
    </>
  );
}

describe("el foco después del veredicto", () => {
  it("lo lleva al bloque que reemplazó al formulario", () => {
    const { rerender } = render(<Panel recorded={false} />);

    // Lo que hace el navegador al apretar el botón: el foco está en él.
    const button = screen.getByRole("button", { name: /Registrar veredicto/i });
    button.focus();
    expect(document.activeElement).toBe(button);

    rerender(<Panel recorded />);

    // Sin esto el botón se desmonta y el foco cae al <body>: el cursor del lector se pierde.
    expect(document.activeElement).toBe(document.getElementById(VERDICT_ID));
  });

  it("no lo mueve al entrar a una alerta que ya estaba revisada", () => {
    render(<Panel recorded />);

    // No hubo transición, así que nadie tocó el foco: arrebatárselo a quien recién llega sería peor
    // que el problema que este componente resuelve.
    expect(document.activeElement).toBe(document.body);
  });

  it("tampoco lo mueve mientras la alerta sigue abierta", () => {
    const { rerender } = render(<Panel recorded={false} />);
    rerender(<Panel recorded={false} />);

    expect(document.activeElement).toBe(document.body);
  });

  it("no rompe nada si el bloque no está en el documento", () => {
    const { rerender } = render(<VerdictFocus recorded={false} targetId="no-existe" />);

    expect(() => {
      rerender(<VerdictFocus recorded targetId="no-existe" />);
    }).not.toThrow();
  });
});
