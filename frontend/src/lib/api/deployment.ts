import "server-only";

/**
 * Lo que el proceso de Next sabe de su propio despliegue sin preguntarle a nadie.
 *
 * <h3>Por qué esto no sale de `capabilities`, como todo lo demás</h3>
 *
 * Porque acá hay una circularidad. `GET /api/system/capabilities` es la fuente de verdad de si esta
 * instancia es compartida, y el layout la usa para decidir si pinta el cartel. Pero esta bandera
 * existe **para decidir qué mostrar cuando la API no contesta**, que es exactamente el momento en
 * que `capabilities` tampoco contesta. Preguntarle a la API si estamos en la instancia compartida
 * justo cuando la API es lo que falta es una pregunta que nunca tiene respuesta cuando importa.
 *
 * <h3>Por qué esto no contradice la regla del layout</h3>
 *
 * `layout.tsx` explica por qué el **idioma** no se lee del entorno del proceso: dos lectores de una
 * variable es una consola en un idioma alrededor de un párrafo en el otro, sin nada que lo reporte.
 * Ese argumento vale cuando los dos lectores pueden **discrepar en pantalla al mismo tiempo**, y acá
 * no pueden: si la API contesta, esta bandera no se usa para nada; si no contesta, la API no tiene
 * ninguna opinión que contradecir. Los dos caminos no se cruzan nunca.
 *
 * Y no es una segunda fuente de verdad sobre el valor: el `Dockerfile` pone
 * `SharedInstance__Enabled` una sola vez, en el entorno del contenedor, y de ahí lo leen la API
 * —que lo publica en `capabilities`— y este módulo. Es la misma variable, leída por dos procesos que
 * comparten entorno, igual que `SALVO_API_BASE_URL`.
 *
 * <h3>Cómo se escribe el nombre</h3>
 *
 * `SharedInstance__Enabled`, con dos guiones bajos, porque es la convención de configuración de
 * .NET y la variable está puesta para la API. Node no la interpreta: la lee tal cual.
 */

/**
 * Si este despliegue es la instancia pública compartida.
 *
 * Se lee en cada llamada y no una vez por módulo, porque un módulo de Next se evalúa una sola vez
 * por proceso y los tests necesitan poder cambiar el entorno entre casos. El costo es leer una
 * propiedad de un objeto.
 */
export function isSharedInstance(): boolean {
  return process.env.SharedInstance__Enabled === "true";
}

/**
 * Cuánto se le dice al visitante que suele tardar el arranque, en segundos.
 *
 * **De dónde sale, dicho con precisión, porque el número aparece en pantalla.** No es el resultado
 * de una sola corrida cronometrada de punta a punta: son dos tramos que se suman, cada uno con su
 * fuente.
 *
 * - Despertar el servicio dormido: «This process takes about one minute», documentación de Render
 *   (https://render.com/docs/free), releída el 2026-09-10.
 * - Que la API quede lista dentro del contenedor: 41 s medidos en `E10B` a 0,1 vCPU, con la base ya
 *   horneada.
 *
 * Los dos tramos se solapan —Render cuenta desde que recibe la petición y la aplicación arranca
 * dentro de esa misma ventana—, así que sumarlos daría un número peor que el real. Lo que la
 * pantalla promete es el orden de magnitud, redondeado hacia arriba, y eso es deliberado:
 * prometerle a alguien menos de lo que va a esperar es peor que prometerle un poco más. La medición
 * desde afuera y sus tropiezos están en la entrada de `E10C` de `Coordination/Handoffs/Claude.md`.
 */
export const COLD_START_SECONDS = 60;

/**
 * Cuánto está dispuesta a esperar la consola, del lado del servidor, a que la API arranque.
 *
 * El presupuesto se cuenta sobre el reloj y no en número de intentos, porque cada intento consume su
 * propio plazo de 5 s y un contador escondería cuánto llega a esperar el visitante de verdad.
 *
 * **40 s y no 60**, aunque la pantalla diga un minuto, y el motivo es que las dos cifras miden cosas
 * distintas: la pantalla habla del arranque completo visto por el visitante —con la pantalla de
 * carga de Render incluida—, y esto empieza a contar recién cuando el proceso de Next ya atiende, o
 * sea después de ese primer tramo. Cubre la ventana en la que Next está en pie y la API todavía no,
 * que es la que produjo el defecto.
 *
 * Es un tope, no una espera fija: en cuanto la API contesta, la petición sigue su camino. Y si se
 * agota, lo que aparece no es un error sino {@link "@/components/starting-up-notice"}, con un botón
 * para volver a pedir.
 */
export const COLD_START_GRACE_MS = 40_000;

/**
 * Cuánto se espera entre un intento y el siguiente.
 *
 * Corto, porque lo que se está esperando es un proceso que va a estar listo de golpe y no un
 * servicio degradado que convenga no castigar: no hay nada que un backoff exponencial proteja acá.
 * Y no cero, porque martillar una API que está arrancando le quita el poco CPU que tiene para
 * arrancar, que a 0,1 de un núcleo no es una figura retórica.
 */
export const COLD_START_RETRY_GAP_MS = 1_500;
