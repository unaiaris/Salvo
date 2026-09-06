# Salvo — Portabilidad de agentes y proveedores

> Estado del documento: vigente
> Última actualización: 2026-09-06
> Fuente de verdad: [[Salvo-Blueprint]]

Este documento describe únicamente los puntos de sustitución. La arquitectura y el dominio no están
atados a una herramienta de construcción, un LLM o un proveedor antifraude.

## Tres capas independientes

| Capa | Hoy | Alternativas |
| --- | --- | --- |
| Agente que construye | Codex construyó las Etapas 0 a 3; Claude Code, de la 4 en adelante | Cursor u otro |
| Explicación en runtime | Plantilla determinista en proceso, sin red | Anthropic u otro LLM, si se aprueba |
| Evaluación antifraude externa | Mock determinista en proceso, sin red | Koin sandbox u otro proveedor |

Cambiar una capa no debe obligar a reescribir las demás. Que la primera fila haya cambiado de agente
a mitad del proyecto sin tocar una línea de dominio es la evidencia más directa de que la separación
es real.

## Agente de construcción

- Codex lee `AGENTS.md`.
- Claude Code lee el `CLAUDE.md` raíz, que importa reglas y estado compartidos mediante `@ruta`.
- `ClaudeAgent/` contiene únicamente workflow y plantillas específicas; no copia el Blueprint.
- `Coordination/` separa ownership, handoffs y estado integrado cuando ambos trabajan en paralelo.
- El Blueprint, el tracker, los tests y los criterios de aceptación son independientes del agente.
- No mantener dos archivos de instrucciones extensos y divergentes.

## Proveedor de explicaciones

El puerto real vive en `backend/src/Salvo.Application/Explanations/IExplanationProvider.cs`:

```csharp
public interface IExplanationProvider
{
    ExplanationProvider Provider { get; }

    string TemplateVersion { get; }

    Task<ExplanationDraft> ExplainAsync(
        ExplanationInput input,
        CancellationToken cancellationToken);
}
```

Las dos propiedades no son decoración. `Provider` y `TemplateVersion` forman parte de la identidad
de la fila que se persiste: la misma evaluación redactada por una plantilla posterior es una
explicación distinta, no la misma reescrita. Por eso cambiar el texto que produce una plantilla
obliga a subir su versión (decisión 58).

`ExplanationDraft` devuelve el resumen —que puede ser nulo, porque un modelo puede legítimamente
declinar, y eso es un fallo con código y no una excepción—, las reglas citadas, y opcionalmente el
modelo concreto y el consumo de tokens. Que la salida sea estructurada en el puerto y no en el
adaptador es deliberado: a un modelo se le pedirá exactamente esa forma como esquema JSON, y la
plantilla determinista la completa igual.

Implementaciones previstas:

- Determinista: la que existe, siempre disponible y sin red.
- Anthropic: decisión aparte, todavía no tomada. Hoy no hay adaptador.
- Otro LLM: posible sin cambiar scoring, alertas o UI.

La salida es estructurada y no decide fraude o severidad. «Usa únicamente señales suministradas»
no se confía al prompt: se **verifica sobre la salida**, comparando cada nombre de regla y cada
cifra del texto contra un conjunto de hechos construido en el dominio a partir de la evaluación y
del pedido. El texto que no lo cumple no se persiste. La verificación vive en el caso de uso, no
en el adaptador, para que todo proveedor —el determinista incluido— la pase por construcción.

## Proveedor antifraude

Contrato conceptual: `IAntifraudProvider` con `EvaluateAsync` y `GetStatusAsync`.

- Mock local: parte del MVP.
- Koin sandbox: adaptador posterior.
- Otro proveedor: conserva el dominio y mapea sus estados a un resultado externo normalizado.

Los datos específicos del proveedor permanecen en infraestructura. El dominio conserva
`referenceId`, correlación, fuente y estado sin depender del payload completo de Koin.

## Variables por proveedor

Los nombres y los valores seguros están en `.env.example`. Ninguna de estas variables es necesaria
para el modo local, y ninguna usa prefijo `NEXT_PUBLIC_`.

**Estos dos valores hoy hacen fallar el arranque, a propósito.** No están «pendientes de
configurar»: `backend/src/Salvo.Infrastructure/DependencyInjection.cs` los rechaza al componer,
porque esta build no tiene adaptador para ninguno de los dos y degradar en silencio a mock sería
exactamente la mentira que este documento existe para impedir. Un valor desconocido en cualquiera de
las dos variables también falla.

Anthropic, si se aprueba:

```dotenv
AI_PROVIDER="anthropic"
ANTHROPIC_API_KEY=""
ANTHROPIC_MODEL=""
```

Koin, si se aprueba:

```dotenv
KOIN_MODE="sandbox"
KOIN_BASE_URL="https://api-sandbox.koin.com.br"
KOIN_PRIVATE_KEY=""
KOIN_ORG_ID=""
KOIN_STORE_CODE=""
KOIN_CALLBACK_URL=""
KOIN_CALLBACK_SHARED_SECRET=
```

### El secreto del callback

Conviene no confundir dos variables que se parecen:

| Variable | Quién la lee | Para qué |
| --- | --- | --- |
| `SALVO_CALLBACK_SHARED_SECRET` | `backend/src/Salvo.Api/ExternalCallbackEndpoints.cs`, hoy | El secreto que un proveedor presenta en la cabecera `X-Salvo-Callback-Secret`. Vacío significa cerrado: todo callback se rechaza con `401` |
| `KOIN_CALLBACK_SHARED_SECRET` | Nada, todavía | Reservada para el día que exista un adaptador de Koin |

El secreto compartido **no** es el mecanismo que usaría una integración real, que verificaría una
firma sobre el cuerpo; el §5.3 del Blueprint lo pide como requisito antes de activar sandbox. Lo que
está modelado es dónde se resuelve el problema. Es backend puro: el proceso de Next nunca lo lee.

## Qué no cambia al sustituir proveedores

- Baseline, reglas y scoring local.
- Modelo de alertas y revisión.
- Métricas de calidad.
- Contratos de aplicación.
- Tests del dominio.
- Principios de idempotencia, redacción y degradación elegante.

## Contrato frontend–backend

ASP.NET Core publica OpenAPI y el frontend consume tipos/cliente generados o derivados de ese
contrato. El artefacto generado no es una segunda fuente de verdad y no se edita manualmente. Esto
permite sustituir la UI sin mover reglas, casos de uso o persistencia fuera de .NET.

## Regla de portfolio

El README debe decir con precisión qué está simulado y qué se ejecutó contra un sandbox. Nunca se
presenta un mock como integración oficial o certificada.
