import axe from "axe-core";

/**
 * `axe-core` sobre el árbol ya renderizado, sin ningún paquete de por medio.
 *
 * <h3>Por qué esto no es un `expect(...).toHaveNoViolations()`</h3>
 *
 * Existe un matcher de terceros que hace lo mismo, y no se usa. La comprobación entera son estas
 * líneas: correr `axe.run` sobre el contenedor y comparar contra la cadena vacía. Un matcher
 * importado esconde qué se afirma exactamente y agrega una dependencia para ahorrar veinte líneas
 * que además hay que leer igual el día que una comprobación falle.
 *
 * Devolver **texto** en vez de un booleano o una cuenta es deliberado. `expect(informe).toBe("")`
 * imprime el informe entero en el diff cuando falla, con la regla, su impacto, el elemento y qué
 * hacer al respecto. Un `expect(3).toBe(0)` obligaría a volver a correr axe a mano para saber qué
 * pasó.
 *
 * <h3>Lo que esto comprueba, y lo que no</h3>
 *
 * Comprueba el **árbol renderizado**: los elementos ya compuestos, con el texto de los diccionarios
 * adentro. Es justo lo que un linter de código fuente no alcanza, porque en esta consola cada
 * literal llega al JSX como expresión.
 *
 * No comprueba **contraste de color**. jsdom no calcula estilos ni tiene canvas, así que `axe-core`
 * no puede medirlo y lo devuelve como «incompleto», no como violación. Un incompleto que nadie mira
 * es una comprobación que se cree hecha, así que la regla se apaga acá **por su nombre**: apagada y
 * dicha es honesto, silenciosamente incompleta no lo es. El contraste queda para el recorrido con
 * lector de pantalla y para el ojo.
 *
 * Tampoco comprueba nada que dependa del foco o del orden de tabulación, que es lo que el recorrido
 * humano sí contesta.
 */
const SIN_LAYOUT = ["color-contrast"];

/**
 * Las violaciones del contenedor, como texto listo para leer. Cadena vacía cuando no hay ninguna.
 */
export async function accessibilityReport(container: HTMLElement): Promise<string> {
  const result = await axe.run(container, {
    resultTypes: ["violations"],
    rules: Object.fromEntries(SIN_LAYOUT.map((rule) => [rule, { enabled: false }])),
  });

  return result.violations
    .map((violation) => {
      const elements = violation.nodes
        .map((node) => `      ${node.html}`)
        .join("\n");

      return [
        `[${violation.impact ?? "sin impacto"}] ${violation.id}`,
        `   ${violation.help}`,
        `   ${violation.helpUrl}`,
        elements,
      ].join("\n");
    })
    .join("\n\n");
}
