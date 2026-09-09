import { apiBaseUrl } from "@/lib/api/server-client";

/**
 * La sonda de salud del contenedor, y comprueba **los dos procesos**.
 *
 * <h3>Por qué una sonda contra la consola sola miente</h3>
 *
 * Si la API muere y `server.js` sigue en pie, la consola contesta `200` en todas sus rutas: las
 * páginas renderizan igual, con el aviso de que no se pudo contactar a la API, porque así está
 * escrito a propósito en `lib/api/console.ts`. Una sonda de plataforma que mire el puerto público
 * vería una instancia sana con medio producto muerto, y nadie se enteraría hasta que alguien la
 * abriera. Lo marcó la revisión adversarial de la Etapa 10.
 *
 * El supervisor del contenedor acota ese caso —la muerte de cualquiera de los dos procesos termina
 * el contenedor— pero acotar no es lo mismo que ver: entre que la API muere y el supervisor
 * reacciona, y en cualquier estado donde la API esté viva y no responda, una sonda que solo mira a
 * Next sigue siendo una sonda que miente. Esta pregunta por las dos cosas: que este proceso conteste
 * es la mitad, y que la API conteste es la otra.
 *
 * <h3>Qué no es</h3>
 *
 * No es una superficie de diagnóstico. Contesta `ok` o `unavailable` y nada más: el detalle de por
 * qué la API no responde iría a un endpoint público de una instancia sin autenticación, y
 * `AGENTS.md` pide sanitizar los errores externos. Los registros del contenedor tienen el detalle.
 *
 * <h3>Y no la limita el límite de tasa</h3>
 *
 * `proxy.ts` la excluye por su ruta. La plataforma la consulta cada pocos segundos desde una sola
 * dirección, así que sería la primera en gastarse su propio cubo, y una sonda limitada es una
 * instancia que la plataforma cree caída.
 */
export const dynamic = "force-dynamic";

/** Corto a propósito: una sonda que tarda es una sonda que la plataforma da por fallada. */
const PROBE_TIMEOUT_MS = 3_000;

export async function GET(): Promise<Response> {
  const apiIsUp = await probeApi();

  return Response.json(
    { status: apiIsUp ? "ok" : "unavailable", console: "ok", api: apiIsUp ? "ok" : "unreachable" },
    {
      status: apiIsUp ? 200 : 503,
      headers: { "cache-control": "no-store" },
    },
  );
}

async function probeApi(): Promise<boolean> {
  try {
    const response = await fetch(new URL("/health", apiBaseUrl()), {
      cache: "no-store",
      signal: AbortSignal.timeout(PROBE_TIMEOUT_MS),
    });

    return response.ok;
  } catch {
    return false;
  }
}
