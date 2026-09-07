"use client";

import { useEffect, useRef } from "react";

/**
 * El foco, cuando el veredicto reemplaza al formulario. No dibuja nada.
 *
 * Al aplicarse la revisión, la acción de servidor revalida la ruta y el panel deja de renderizar el
 * formulario para renderizar el veredicto registrado. El botón que acaba de apretarse —el que tiene
 * el foco— se desmonta con el formulario, y el foco cae al `<body>`. Para quien navega con lector de
 * pantalla eso no es una molestia: es perder el cursor y tener que recorrer la página otra vez desde
 * arriba para averiguar si el veredicto entró.
 *
 * <h3>Por qué es un vecino que no dibuja y no un envoltorio</h3>
 *
 * Un envoltorio recibiría el bloque a enfocar como `children`, y `children` es un objeto: la
 * frontera de esta consola admite primitivas y nada más, y `boundary.test.ts` lo comprueba sobre el
 * árbol de verdad. Así que este componente recibe dos primitivas —si la alerta ya tiene veredicto, y
 * el `id` del elemento a enfocar— y busca el elemento en el documento.
 *
 * <h3>Por qué mira la transición y no el estado</h3>
 *
 * Ver el cambio de `false` a `true` es todo el punto. Un componente que solo existiera dentro del
 * veredicto no podría distinguir «acabo de revisar» de «entré a una alerta ya revisada», y en el
 * segundo caso mover el foco sería arrebatárselo a alguien sin motivo. Por eso el panel lo renderiza
 * en los dos estados —la instancia no se desmonta entre uno y otro—, el estado anterior se recuerda
 * en un ref, y la primera carga nunca dispara.
 *
 * Lo que se **anuncia** lo dice la región viva de `ReviewPanel`, que es otra cosa: mover el foco y
 * anunciar son dos respuestas a dos preguntas distintas —dónde quedé, y qué pasó—.
 */
export function VerdictFocus({
  recorded,
  targetId,
}: {
  /** Si la alerta ya tiene veredicto. */
  readonly recorded: boolean;
  /** El `id` del bloque que reemplazó al formulario. */
  readonly targetId: string;
}) {
  const wasRecorded = useRef(recorded);

  useEffect(() => {
    if (!wasRecorded.current && recorded) {
      document.getElementById(targetId)?.focus();
    }

    wasRecorded.current = recorded;
  }, [recorded, targetId]);

  return null;
}
