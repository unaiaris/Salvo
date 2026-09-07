import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { ActionOutcome } from "./action-outcome";
import { INITIAL_ACTION_STATE, type ActionState } from "./action-state";

/**
 * El rol del bloque de resultado, que no es una cuestión de estilo.
 *
 * `alert` es asertivo: interrumpe lo que el lector esté diciendo en ese momento. Para un fallo es lo
 * correcto —hay que oírlo antes de seguir—; para un éxito es de más, y con las tres acciones de esta
 * pantalla usando `alert` una importación que salió bien cortaba la frase en curso.
 */
function outcome(overrides: Partial<ActionState>): ActionState {
  return {
    ...INITIAL_ACTION_STATE,
    title: "Se importaron 3 pedidos",
    body: "Importar escribe pedidos y no produce evaluaciones ni alertas.",
    submissionId: 1,
    ...overrides,
  };
}

describe("el resultado de una acción de importación", () => {
  it("anuncia un éxito sin interrumpir", () => {
    render(<ActionOutcome state={outcome({ outcome: "done" })} language="es" />);

    expect(screen.getByRole("status")).toHaveTextContent(/Se importaron 3 pedidos/);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("interrumpe cuando la acción falló", () => {
    render(
      <ActionOutcome
        state={outcome({
          outcome: "failed",
          title: "No se pudo importar el archivo",
          recovery: "Revisá el formato y volvé a intentarlo.",
        })}
        language="es"
      />,
    );

    expect(screen.getByRole("alert")).toHaveTextContent(/No se pudo importar el archivo/);
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });

  it("no dice nada mientras nadie apretó nada", () => {
    const { container } = render(<ActionOutcome state={INITIAL_ACTION_STATE} language="es" />);

    expect(container).toBeEmptyDOMElement();
  });
});
