# Salvo — Portabilidad de agentes y proveedores

> Estado del documento: vigente
> Última actualización: 2026-08-31
> Fuente de verdad: [[Salvo-Blueprint]]

Este documento describe únicamente los puntos de sustitución. La arquitectura y el dominio no están
atados a una herramienta de construcción, un LLM o un proveedor antifraude.

## Tres capas independientes

| Capa | Elección inicial | Alternativas |
| --- | --- | --- |
| Agente que construye | Codex | Claude Code, Cursor u otro |
| Explicación en runtime | Determinista/mock; Anthropic después | OpenAI u otro LLM |
| Evaluación antifraude externa | Mock local | Koin sandbox u otro proveedor |

Cambiar una capa no debe obligar a reescribir las demás.

## Agente de construcción

- Codex lee `AGENTS.md`.
- Claude Code lee el `CLAUDE.md` raíz, que importa reglas y estado compartidos mediante `@ruta`.
- `ClaudeAgent/` contiene únicamente workflow y plantillas específicas; no copia el Blueprint.
- `Coordination/` separa ownership, handoffs y estado integrado cuando ambos trabajan en paralelo.
- El Blueprint, el tracker, los tests y los criterios de aceptación son independientes del agente.
- No mantener dos archivos de instrucciones extensos y divergentes.

## Proveedor de explicaciones

Contrato conceptual:

```csharp
public interface IExplanationProvider
{
    Task<ExplanationResult> ExplainAsync(
        ExplanationInput input,
        CancellationToken cancellationToken);
}
```

Implementaciones previstas:

- Determinista/mock: siempre disponible, sin red.
- Anthropic: proveedor preferido cuando se apruebe la etapa.
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

Anthropic posterior:

```dotenv
AI_PROVIDER="anthropic"
ANTHROPIC_API_KEY=""
ANTHROPIC_MODEL=""
```

Koin posterior:

```dotenv
KOIN_MODE="sandbox"
KOIN_BASE_URL="https://api-sandbox.koin.com.br"
KOIN_PRIVATE_KEY=""
KOIN_ORG_ID=""
KOIN_STORE_CODE=""
KOIN_CALLBACK_URL=""
```

Ninguna de estas variables es necesaria para el modo local mock.

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
