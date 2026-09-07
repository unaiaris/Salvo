/**
 * Genera `glosario-pt.md` a partir de los dos diccionarios.
 *
 * Existe para que corregir el portugués no exija abrir código: quien lo revisa lee la tabla, dice
 * qué fila está mal, y quien la arregla busca esa clave en `pt.ts`. La tabla se vuelve a generar
 * con
 *
 *   node --experimental-strip-types frontend/src/lib/i18n/glosario.mjs > frontend/src/lib/i18n/glosario-pt.md
 *
 * Node 24 ejecuta TypeScript quitando los tipos, así que no hace falta compilar nada: los dos
 * diccionarios se importan tal como están.
 */
import { es } from "./es.ts";
import { pt } from "./pt.ts";

/** Every leaf of the dictionary, as `a.b.c` paths in declaration order. */
function flatten(value, prefix = "") {
  const rows = [];
  for (const [key, leaf] of Object.entries(value)) {
    const path = prefix === "" ? key : `${prefix}.${key}`;
    if (typeof leaf === "object" && leaf !== null) {
      rows.push(...flatten(leaf, path));
    } else {
      rows.push([path, leaf]);
    }
  }
  return rows;
}

const spanish = new Map(flatten(es));
const portuguese = new Map(flatten(pt));

/** A function value is shown by what it writes, with its parameters named as placeholders. */
function sample(value) {
  if (typeof value !== "function") {
    return String(value);
  }
  const names = (/\(([^)]*)\)/.exec(value.toString())?.[1] ?? "")
    .split(",")
    .map((one) => one.trim().split(":")[0].trim())
    .filter((one) => one.length > 0);
  const args = names.map((name) => (name === "count" ? 2 : `{${name}}`));
  try {
    return String(value(...args));
  } catch {
    return "(no se pudo componer)";
  }
}

const rows = [...spanish.keys()].map((key) => [
  key,
  sample(spanish.get(key)).replaceAll("|", "\\|"),
  sample(portuguese.get(key)).replaceAll("|", "\\|"),
]);

const lines = [
  "# Glosario castellano — portugués",
  "",
  "> **Este portugués no lo revisó un hablante nativo.** Lo escribió la misma persona que escribió",
  "> el castellano, y está publicado así a propósito: una traducción sin revisar y declarada es",
  "> honesta; una sin revisar y presentada como terminada, no.",
  "",
  "Una fila por clave del diccionario. Para corregir una, hay que editar la clave con ese nombre en",
  "`frontend/src/lib/i18n/pt.ts` y volver a generar esta tabla con",
  "`node --experimental-strip-types frontend/src/lib/i18n/glosario.mjs`. No hace falta abrir ningún",
  "otro archivo: el tipo `Dictionary` garantiza que las claves de los dos idiomas son exactamente",
  "las mismas, así que esta tabla no puede quedar incompleta sin que el proyecto deje de compilar.",
  "",
  "Las claves con `{algo}` entre llaves llevan un valor que se calcula: un monto, una cantidad, una",
  "fecha. El texto de alrededor es lo traducible; el valor lo escribe el código y es el mismo en los",
  "dos idiomas.",
  "",
  "**Vocabulario elegido.** «comercio» es *estabelecimento*, la palabra del mercado adquirente",
  "brasileño; «monto» es *valor*; «corrida» es *execução*; «veredicto» es *veredito*; «denegado» es",
  "*negado*; «etiqueta» es *rótulo*.",
  "",
  `Entradas: **${rows.length}**.`,
  "",
  "| Clave | Castellano | Português |",
  "| --- | --- | --- |",
  ...rows.map(([key, a, b]) => `| \`${key}\` | ${a} | ${b} |`),
  "",
];

process.stdout.write(lines.join("\n"));
