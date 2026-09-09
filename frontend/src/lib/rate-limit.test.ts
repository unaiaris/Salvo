import { describe, expect, it } from "vitest";

import { RateLimiter, kindOf, originOf, readPolicy } from "./rate-limit";

/**
 * La política es pura y el reloj entra como argumento, así que todo esto se decide sin levantar
 * nada. Lo que el contenedor demuestra aparte es que el proxy la aplica; lo que se decide acá es
 * que la política dice lo que hay que decir.
 */

const POLICY = { windowMs: 60_000, reads: 3, writes: 2 };

describe("la política que declara el entorno", () => {
  it("no existe si nadie la enciende", () => {
    expect(readPolicy({})).toBeNull();
    expect(readPolicy({ SALVO_RATE_LIMIT_READS: "5" })).toBeNull();
  });

  it("trae valores por omisión que un uso humano no toca", () => {
    expect(readPolicy({ SALVO_RATE_LIMIT: "on" })).toEqual({
      windowMs: 60_000,
      reads: 120,
      writes: 10,
    });
  });

  /**
   * Un número ilegible cae al valor por omisión en vez de apagar el límite. La alternativa sería
   * una instancia que se cree protegida y no lo está, que es peor que una con números distintos de
   * los que alguien quiso.
   */
  it("no deja que un número ilegible apague el límite", () => {
    const policy = readPolicy({ SALVO_RATE_LIMIT: "on", SALVO_RATE_LIMIT_WRITES: "diez" });

    expect(policy).not.toBeNull();
    expect(policy?.writes).toBe(10);
  });
});

describe("los dos cubos", () => {
  it("cuenta lecturas y mutaciones por separado", () => {
    const limiter = new RateLimiter(POLICY);

    for (let i = 0; i < POLICY.reads; i++) {
      expect(limiter.check("1.2.3.4", "read", 0).allowed, `lectura ${i}`).toBe(true);
    }

    // La lectura de más se rechaza y la mutación sigue teniendo su propio cupo entero.
    expect(limiter.check("1.2.3.4", "read", 0).allowed).toBe(false);
    expect(limiter.check("1.2.3.4", "write", 0).allowed).toBe(true);
  });

  it("un visitante no gasta el cupo de otro", () => {
    const limiter = new RateLimiter(POLICY);

    for (let i = 0; i <= POLICY.writes; i++) {
      limiter.check("1.2.3.4", "write", 0);
    }

    expect(limiter.check("1.2.3.4", "write", 0).allowed).toBe(false);
    expect(limiter.check("5.6.7.8", "write", 0).allowed).toBe(true);
  });

  it("dice cuántos segundos faltan, y abre la ventana siguiente", () => {
    const limiter = new RateLimiter(POLICY);

    for (let i = 0; i <= POLICY.writes; i++) {
      limiter.check("1.2.3.4", "write", 0);
    }

    expect(limiter.check("1.2.3.4", "write", 15_000)).toEqual({
      allowed: false,
      retryAfterSeconds: 45,
    });
    expect(limiter.check("1.2.3.4", "write", 60_000).allowed).toBe(true);
  });

  /**
   * Un contador de abuso que crece sin techo con la cantidad de visitantes sería una manera nueva
   * de tumbar la instancia que protege.
   */
  it("suelta las ventanas vencidas en vez de acumular un origen por visitante", () => {
    const limiter = new RateLimiter(POLICY);

    for (let visitor = 0; visitor < 500; visitor++) {
      limiter.check(`10.0.0.${visitor}`, "read", 0);
    }

    // Pasada la ventana, el primero vuelve a arrancar de cero: su cubo ya no existe.
    for (let i = 0; i < POLICY.reads; i++) {
      expect(limiter.check("10.0.0.0", "read", 60_000).allowed).toBe(true);
    }
  });
});

describe("quién es el visitante", () => {
  it("toma el primero de la cadena de proxies", () => {
    expect(originOf(new Headers({ "x-forwarded-for": "203.0.113.7, 10.0.0.1" }))).toBe("203.0.113.7");
    expect(originOf(new Headers({ "x-real-ip": "203.0.113.9" }))).toBe("203.0.113.9");
  });

  /**
   * Sin cabecera, todos comparten cubo. Es el caso conservador y conviene que sea el que ocurre
   * cuando no se sabe: la instancia se protege igual, al precio de que dos visitantes se estorben.
   */
  it("cae en un origen común cuando la plataforma no dice nada", () => {
    expect(originOf(new Headers())).toBe("sin-origen");
    expect(originOf(new Headers({ "x-forwarded-for": "  " }))).toBe("sin-origen");
  });
});

describe("qué cuesta cada método", () => {
  /**
   * Una acción de servidor viaja como `POST` a la ruta donde vive el botón, así que sembrar,
   * puntuar, importar y emitir un veredicto entran por acá sin que nadie los enumere.
   */
  it("solo una lectura es una lectura", () => {
    expect(kindOf("GET")).toBe("read");
    expect(kindOf("HEAD")).toBe("read");
    expect(kindOf("POST")).toBe("write");
    expect(kindOf("DELETE")).toBe("write");
  });
});
