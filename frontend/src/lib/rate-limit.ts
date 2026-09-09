/**
 * El límite de tasa de la instancia pública, como política pura.
 *
 * <h3>Por qué vive en la capa de Next y no en la API</h3>
 *
 * Porque **la API no ve visitantes**. Desde que la Etapa 10 borró el rewrite, toda petición le
 * llega desde `127.0.0.1`, sin cabecera de origen y sin nada que distinga a una persona de otra:
 * las hace el proceso de Node al renderizar. Un limitador ahí estaría limitando a la consola contra
 * sí misma, y una sola pantalla que hace tres lecturas contaría como tres visitantes distintos.
 * El visitante existe en el servidor de Next, que es donde llega su conexión.
 *
 * <h3>Lo que este límite no hace, dicho para que nadie se confíe</h3>
 *
 * Acota **cuántas veces** se pide trabajo. No acota **cuánto cuesta** ese trabajo, que depende de
 * cuántos pedidos hay en la base y no de cuántas veces se pidió una corrida. Eso lo acota
 * `SharedInstance:MaxOrders`, en la API, y las dos defensas hacen falta.
 *
 * <h3>Un contador en memoria, y qué supone</h3>
 *
 * Supone **una sola instancia**, que es exactamente lo que la Etapa 10 despliega. Con dos réplicas
 * cada una contaría por su lado y el límite efectivo sería el doble. La documentación de Next
 * desaconseja el estado compartido en `proxy.ts` por ese motivo —está pensado para correr
 * distribuido—, pero en Next 16 el proxy corre en el runtime de Node, en el mismo proceso que
 * `server.js`, así que acá el contador es real. Queda dicho para quien replique.
 *
 * La política es pura y el reloj entra como argumento, así que se puede probar sin levantar nada.
 */

/** Qué cubo consume una petición. Una mutación cuesta mucho más que una lectura. */
export type RequestKind = "read" | "write";

export interface RateLimitPolicy {
  readonly windowMs: number;
  /** Lecturas por ventana y por origen. */
  readonly reads: number;
  /** Mutaciones por ventana y por origen, incluidas las acciones de servidor. */
  readonly writes: number;
}

export interface RateLimitDecision {
  readonly allowed: boolean;
  /** Segundos hasta que el cubo se vacía. Solo tiene sentido cuando se rechaza. */
  readonly retryAfterSeconds: number;
}

/**
 * La política que declara el entorno del proceso de Next, o <c>null</c> si no declara ninguna.
 *
 * **Apagado por omisión**, y a propósito: la compuerta, el smoke y las capturas hacen decenas de
 * peticiones en segundos contra una consola local, y un límite pensado para desconocidos las
 * volvería intermitentes. La imagen de la instancia pública lo enciende.
 *
 * Un valor ilegible **no se ignora**: se rechaza la configuración entera y la instancia queda sin
 * límite de forma visible en vez de creerse protegida. El arranque lo dice.
 */
export function readPolicy(env: Record<string, string | undefined>): RateLimitPolicy | null {
  if (env.SALVO_RATE_LIMIT !== "on") {
    return null;
  }

  const windowSeconds = positiveInteger(env.SALVO_RATE_LIMIT_WINDOW_SECONDS) ?? 60;
  const reads = positiveInteger(env.SALVO_RATE_LIMIT_READS) ?? 120;
  const writes = positiveInteger(env.SALVO_RATE_LIMIT_WRITES) ?? 10;

  return { windowMs: windowSeconds * 1000, reads, writes };
}

function positiveInteger(raw: string | undefined): number | null {
  if (raw === undefined || !/^[1-9]\d*$/.test(raw)) {
    return null;
  }

  return Number(raw);
}

interface Bucket {
  count: number;
  /** Instante en que la ventana de este cubo termina. */
  resetsAt: number;
}

/**
 * Ventanas fijas por origen y por cubo.
 *
 * Ventana fija y no deslizante porque lo que hay que frenar es un bucle, y un bucle satura
 * cualquiera de las dos. Una deslizante cuesta memoria proporcional a las peticiones, que es
 * exactamente lo que no conviene gastar en la instancia que se quiere proteger.
 */
export class RateLimiter {
  private readonly buckets = new Map<string, Bucket>();

  public constructor(private readonly policy: RateLimitPolicy) {}

  public check(origin: string, kind: RequestKind, now: number): RateLimitDecision {
    this.forget(now);

    const key = `${kind}:${origin}`;
    const limit = kind === "read" ? this.policy.reads : this.policy.writes;
    const bucket = this.buckets.get(key);

    if (bucket === undefined || bucket.resetsAt <= now) {
      this.buckets.set(key, { count: 1, resetsAt: now + this.policy.windowMs });
      return { allowed: true, retryAfterSeconds: 0 };
    }

    bucket.count += 1;

    if (bucket.count <= limit) {
      return { allowed: true, retryAfterSeconds: 0 };
    }

    return {
      allowed: false,
      retryAfterSeconds: Math.max(1, Math.ceil((bucket.resetsAt - now) / 1000)),
    };
  }

  /**
   * Suelta las ventanas vencidas.
   *
   * Sin esto el mapa crece con cada origen que pasó alguna vez, y un contador de abuso que crece
   * sin techo con la cantidad de visitantes es una manera nueva de tumbar la instancia. Se hace al
   * consultar y no con un temporizador para no dejar un `setInterval` vivo en un proceso que la
   * plataforma va a dormir.
   */
  private forget(now: number): void {
    for (const [key, bucket] of this.buckets) {
      if (bucket.resetsAt <= now) {
        this.buckets.delete(key);
      }
    }
  }
}

/**
 * Quién es el visitante, según lo que la plataforma haya puesto delante.
 *
 * `x-forwarded-for` trae la cadena de proxies y el primero es el cliente. Si no hay ninguna
 * cabecera, **todos comparten cubo**, que es el caso conservador: la instancia se protege igual, al
 * precio de que dos visitantes se estorben. Es el peor caso y conviene que sea el que ocurre cuando
 * no se sabe, no el contrario.
 */
export function originOf(headers: Headers): string {
  const forwarded = headers.get("x-forwarded-for");

  if (forwarded !== null && forwarded.trim() !== "") {
    return forwarded.split(",")[0]!.trim();
  }

  return headers.get("x-real-ip")?.trim() || "sin-origen";
}

/** Una lectura no cambia nada; todo lo demás sí, incluidas las acciones de servidor. */
export function kindOf(method: string): RequestKind {
  return method === "GET" || method === "HEAD" ? "read" : "write";
}
