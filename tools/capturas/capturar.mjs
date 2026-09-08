// Salvo — las seis capturas del README.
//
// Lo conduce `scripts/capturas.sh`, que antes levantó la API y la consola sobre una base nueva y
// preparó por API el estado de cada toma. Acá solo se fotografía.
//
// La regla que ordena todo el archivo: **antes de cada disparo se afirma que en la pantalla está lo
// que la captura promete**. Una captura es una afirmación sobre el producto, y una que se saca sin
// comprobar nada fotografía igual de bien una página de error. Si una espera vence, el script falla
// nombrando la toma en vez de dejar un PNG que miente.
//
// No borra nada: escribe siempre los mismos seis nombres y sobrescribe.

import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";
import { join } from "node:path";

const webBase = required("CAPTURAS_WEB_BASE");
const alertId = required("CAPTURAS_ALERT_ID");
const outputDir = required("CAPTURAS_OUTPUT_DIR");
const samplePath = required("CAPTURAS_SAMPLE");

// Ancho fijo y factor de escala 2: una regeneración se tiene que poder comparar con la anterior, y
// en una pantalla normal un PNG a 1× se lee borroso.
const VIEWPORT = { width: 1280, height: 900 };
const SCALE = 2;

function required(name) {
  const value = process.env[name];
  if (value === undefined || value === "") {
    throw new Error(`falta la variable ${name}.`);
  }
  return value;
}

function log(shot, message) {
  process.stdout.write(`  ${shot}  ${message}\n`);
}

/** Espera un texto exacto y explica qué toma se quedó sin él. */
async function expectText(page, shot, text) {
  try {
    await page.getByText(text, { exact: false }).first().waitFor({ timeout: 30_000 });
  } catch {
    throw new Error(`${shot}: la página no muestra «${text}». No se saca la captura.`);
  }
}

async function shootPage(page, shot, route, checks) {
  await page.goto(`${webBase}${route}`, { waitUntil: "networkidle" });
  for (const text of checks) {
    await expectText(page, shot, text);
  }

  const path = join(outputDir, shot);
  await page.screenshot({ path, fullPage: true });
  log(shot, `${route} → ${path}`);
}

async function shootElement(page, shot, selector, checks) {
  const element = page.locator(selector);
  try {
    await element.waitFor({ timeout: 30_000 });
  } catch {
    throw new Error(`${shot}: no existe «${selector}» en la página. No se saca la captura.`);
  }

  for (const text of checks) {
    try {
      await element.getByText(text, { exact: false }).first().waitFor({ timeout: 30_000 });
    } catch {
      throw new Error(`${shot}: el bloque no dice «${text}». No se saca la captura.`);
    }
  }

  const path = join(outputDir, shot);
  await element.screenshot({ path });
  log(shot, `${selector} → ${path}`);
}

await mkdir(outputDir, { recursive: true });

const browser = await chromium.launch();
const context = await browser.newContext({
  viewport: VIEWPORT,
  deviceScaleFactor: SCALE,
  locale: "es-UY",
  timezoneId: "America/Montevideo",
  reducedMotion: "reduce",
});
const page = await context.newPage();
page.setDefaultTimeout(30_000);

try {
  // ------------------------------------------------------------------ 01. la cola de alertas
  await shootPage(page, "01-cola.png", "/alerts", ["Cola de alertas", "ORD_000011"]);

  // ------------------------------------------------------------------ 02. el detalle completo
  //
  // Se fotografía con todo puesto —snapshot, evaluación vigente, evaluación externa y explicación—
  // porque «el detalle» de esta consola son esos cuatro bloques y no uno solo.
  await shootPage(page, "02-detalle.png", `/alerts/${alertId}`, [
    "ORD_000011",
    "Snapshot que abrió la alerta",
    "Evaluación vigente",
  ]);

  // ------------------------------------------------------------------ 03. la explicación
  //
  // La frase que se exige es la del pie del bloque: es la que dice que el texto lo escribió una
  // plantilla y no un modelo, que es el argumento entero de la Etapa 7.
  await shootElement(page, "03-explicacion.png", 'section[aria-labelledby="explanation-title"]', [
    "Explicación",
    "no por un modelo",
  ]);

  // ------------------------------------------------------------------ 06. la divergencia
  //
  // Va antes que la importación porque comparte página con las dos anteriores y no cuesta nada.
  // Es la divergencia **de criterio**: el motor local marcó el pedido y el proveedor lo aprobó.
  await shootElement(page, "06-divergencia.png", 'section[aria-labelledby="external-title"]', [
    "Los dos criterios no coinciden",
    "aprobó el pedido",
  ]);

  // ------------------------------------------------------------------ 04. el dashboard
  //
  // «Score local» es la última columna de la tabla de denegados por el proveedor sin alerta local, y
  // solo existe cuando ese panel tiene filas: vacío, el panel es un párrafo. Se exige acá para que
  // la captura no pueda volver a salir con ese panel en blanco sin que nadie se entere, que es como
  // salió antes de que `capturas.sh` pidiera la evaluación externa del corpus. `ORD_000275` es uno
  // de los tres arquetipos que las reglas no pueden ver, y es lo que el panel existe para mostrar.
  await shootPage(page, "04-dashboard.png", "/dashboard", [
    "Monto en riesgo",
    "Calidad del criterio",
    "F1 es un parámetro elegido y no un resultado",
    "Score local",
    "ORD_000275",
  ]);

  // ------------------------------------------------------------------ 05. la importación
  //
  // La única toma que necesita un navegador que interactúe. El resultado de una importación vive en
  // el `useActionState` del cliente y no hay tabla de importaciones: sin enviar el formulario de
  // verdad no hay ninguna pantalla que fotografiar, y componerla de otro modo sería fabricarla.
  await page.goto(`${webBase}/import`, { waitUntil: "networkidle" });
  await page.setInputFiles('input[type="file"][name="file"]', samplePath);
  await page.check('input[type="radio"][name="format"][value="CSV"]');
  await page.getByRole("button", { name: "Importar pedidos" }).click();

  // El resultado, con su cuenta. Que diga cinco es la comprobación de que el archivo que se envió es
  // el versionado y de que la API lo rechazó registro por registro.
  await expectText(page, "05-import.png", "Registros rechazados (5)");
  // Y el rechazo por referencia repetida, que es el que demuestra la clave compuesta. Se busca la
  // frase y no el código: la consola muestra la traducción del código, nunca el identificador.
  await expectText(page, "05-import.png", "La referencia ya existe con otros datos");

  const importPath = join(outputDir, "05-import.png");
  await page.screenshot({ path: importPath, fullPage: true });
  log("05-import.png", `/import tras enviar el formulario → ${importPath}`);
} finally {
  await context.close();
  await browser.close();
}
