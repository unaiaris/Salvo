import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";
import jsxA11y from "eslint-plugin-jsx-a11y";

/**
 * <h3>Por qué el conjunto de accesibilidad se declara acá y no se hereda</h3>
 *
 * `eslint-config-next` registra `eslint-plugin-jsx-a11y` y activa **seis** de sus reglas, todas en
 * `warn`. Como `lint` corre con `--max-warnings=0`, esas seis ya rompían la compuerta, así que el
 * punto de partida no era «sin accesibilidad»; era una sexta parte del conjunto recomendado, y las
 * seis eran las que comprueban que un atributo `aria-*` esté bien escrito. Ninguna de ellas mira si
 * un control se puede operar, si un `label` está asociado a su campo, o si un `<a>` tiene contenido.
 *
 * Estas son las veinticinco que faltaban. `flatConfigs.recommended` se extiende **después** de las
 * dos configuraciones de Next para que gane, y sube las seis heredadas de `warn` a `error`: con
 * `--max-warnings=0` el resultado de la compuerta es el mismo, pero el nivel dice lo que se quiere
 * decir en vez de dejarlo a merced de una bandera del script.
 *
 * <h3>Lo que este linter no puede ver en esta consola, y conviene tener presente</h3>
 *
 * Todo el texto vive en `src/lib/i18n/es.ts` y `pt.ts` y llega al JSX como expresión. Una regla que
 * comprueba **contenido** —`anchor-has-content`, `heading-has-content`— ve que hay una expresión y
 * la da por buena sin poder leerla. Por eso `anchor-ambiguous-text` queda apagada, como viene: su
 * lista de palabras es inglesa —«click here», «here», «learn more»— y en esta consola no hay un
 * literal en JSX contra el que pudiera dispararse. Una regla que no puede fallar nunca es peor que
 * ninguna, porque promete una verificación que no existe.
 *
 * Ese hueco es la razón de que `src/test/axe.ts` exista: `axe-core` corre sobre el árbol ya
 * renderizado, con el texto del diccionario adentro, que es exactamente lo que un linter de código
 * fuente no puede alcanzar.
 *
 * <h3>Solo las reglas, nunca el `plugins` del preset</h3>
 *
 * Se extiende `flatConfigs.recommended.rules` y no el objeto entero, porque `eslint-config-next` ya
 * registró el plugin bajo este mismo nombre y la configuración plana prohíbe registrarlo dos veces:
 * `ConfigError: Cannot redefine plugin "jsx-a11y"`. Las reglas se activan por nombre contra el
 * plugin que ya está.
 *
 * Lo que sí necesitaba la declaración de la dependencia es este `import`: se resuelve desde la raíz
 * del proyecto, y hasta ahora el paquete solo existía anidado bajo `eslint-config-next/node_modules/`,
 * donde este archivo no lo alcanza.
 *
 * Se toma la lista del propio plugin en vez de copiar treinta y un nombres con sus opciones: una
 * lista a mano deja de cubrir la regla que la próxima versión agregue, en silencio.
 */
const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  {
    rules: {
      ...jsxA11y.flatConfigs.recommended.rules,

      /**
       * Una sola opción cambiada, y por una razón medida.
       *
       * `label-has-associated-control` recorre **dos** niveles de hijos buscando el texto del
       * `label`, y en el formulario de veredicto el texto está a tres: el `label` envuelve al radio
       * y a un `<span>` que agrupa el nombre de la opción y su aclaración en dos renglones. Ese
       * marcado es correcto y es el que el producto quiere —el nombre accesible del radio termina
       * siendo «Confirmar segura El pedido no es fraude», que es exactamente lo que hay que oír
       * antes de emitir un veredicto—, así que lo que se corrige es la profundidad que la regla
       * mira, no el marcado que ya funciona.
       *
       * Sube a 3, que es lo que hay, y no más: a mayor profundidad la regla empieza a dar por bueno
       * un `label` cuyo texto está tan lejos que ya no lo nombra. Con 3 sigue fallando sobre un
       * `label` sin texto, y eso está falsado.
       */
      "jsx-a11y/label-has-associated-control": ["error", { depth: 3 }],
    },
  },
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
  ]),
]);

export default eslintConfig;
