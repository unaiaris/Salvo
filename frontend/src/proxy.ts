import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

import { es } from "@/lib/i18n/es";
import { pt } from "@/lib/i18n/pt";
import { RateLimiter, kindOf, originOf, readPolicy } from "@/lib/rate-limit";

/**
 * Lo único que corre antes de que la consola atienda: el límite de tasa de la instancia pública.
 *
 * <h3>Por qué acá y no en la API</h3>
 *
 * Porque acá está el visitante. Desde que la Etapa 10 borró el rewrite, la API solo recibe
 * peticiones del proceso de Node desde `127.0.0.1`; limitar ahí sería limitar a la consola contra
 * sí misma. El motivo largo está en `lib/rate-limit.ts`.
 *
 * <h3>Por qué esto ve también los botones</h3>
 *
 * Porque una acción de servidor **no es una ruta**: viaja como un `POST` a la misma ruta donde vive
 * el botón, según la documentación de Next 16 que viene en `node_modules/next/dist/docs`. Así que
 * sembrar, puntuar, importar y emitir un veredicto pasan por acá y cuentan como mutaciones, sin que
 * haya que enumerarlas. La contracara está dicha en esa misma documentación y vale como aviso: un
 * `matcher` que excluya una ruta excluye también sus acciones, así que la lista de exclusiones de
 * abajo es corta a propósito.
 *
 * <h3>Nombre del archivo</h3>
 *
 * `proxy.ts` y no `middleware.ts`: desde Next 16 el Middleware se llama Proxy, y desde Next 16
 * corre en el runtime de Node en vez del de Edge — que es lo que hace que el contador en memoria
 * de `RateLimiter` viva en el mismo proceso que `server.js` y cuente de verdad.
 */

/**
 * Vive en el módulo, que es lo que la documentación de Next desaconseja para un proxy pensado para
 * correr distribuido. Acá corre en un proceso y en uno solo, y la instancia que protege es una sola
 * por definición. El comentario de `lib/rate-limit.ts` dice qué pasaría con réplicas.
 */
const policy = readPolicy(process.env);
const limiter = policy === null ? null : new RateLimiter(policy);

export function proxy(request: NextRequest): NextResponse | undefined {
  if (limiter === null) {
    return undefined;
  }

  const decision = limiter.check(
    originOf(request.headers),
    kindOf(request.method),
    Date.now(),
  );

  if (decision.allowed) {
    return undefined;
  }

  // Texto plano y en los dos idiomas a la vez. Renderizar la respuesta traducida costaría pedirle
  // el idioma a la API y componer una página, que es exactamente el trabajo que este límite existe
  // para no hacer; y el proxy no debe hacer lecturas lentas. Las frases salen de los diccionarios
  // igual, así que no hay texto de producto suelto en este archivo.
  return new NextResponse(
    `${es.rateLimit.title}\n${es.rateLimit.body}\n\n${pt.rateLimit.title}\n${pt.rateLimit.body}\n`,
    {
      status: 429,
      headers: {
        "content-type": "text/plain; charset=utf-8",
        "retry-after": String(decision.retryAfterSeconds),
        // Que un intermediario no guarde un 429 y se lo sirva a otro visitante.
        "cache-control": "no-store",
      },
    },
  );
}

export const config = {
  // Los archivos estáticos quedan afuera: no cuestan trabajo de servidor y contarlos haría que una
  // sola pantalla, que trae su JavaScript y su CSS, gastara el cubo de lecturas de un visitante.
  //
  // `/health` también, y por un motivo distinto: la sonda de la plataforma la consulta cada pocos
  // segundos desde una sola dirección, así que sería la primera en limitarse a sí misma — y una
  // sonda limitada es una instancia que la plataforma cree caída.
  matcher: ["/((?!_next/static|_next/image|health|favicon.ico).*)"],
};
