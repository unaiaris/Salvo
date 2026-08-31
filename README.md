# Salvo

Consola antifraude B2B de portfolio para e-commerce, diseñada alrededor de reglas deterministas,
alertas auditables e integración desacoplada con proveedores externos.

Estado: fundación ejecutable ASP.NET Core + Next.js preparada; la lógica antifraude comienza en
etapas posteriores.

## Verificación local

Con .NET 10.0.400, Node.js 24.20.0 y npm 11.19.0 disponibles en `PATH`:

```bash
npm ci --prefix frontend
./scripts/check.sh
```

La compuerta restaura NuGet en modo bloqueado, compila y prueba el backend en Release, y ejecuta
typecheck, ESLint, Vitest y el build de producción del frontend.

## Arquitectura

- Backend monolítico modular: ASP.NET Core 10, C# 14 y EF Core/SQLite.
- Frontend: Next.js, React y TypeScript estricto.
- Contrato backend–frontend: OpenAPI.
- Tests: xUnit e integración ASP.NET Core; Vitest y Testing Library en UI.
- El scoring, las transacciones y las integraciones viven en .NET; la UI no contiene reglas de
  fraude.

## Documentación

| Documento | Propósito |
| --- | --- |
| [Blueprint](DesignAgent/Salvo-Blueprint.md) | Alcance, arquitectura, modelo y decisiones |
| [Seguimiento](DesignAgent/Salvo-Progress.md) | Estado vivo, checklists, compuertas y evidencias |
| [Resumen ejecutivo](DesignAgent/Salvo-Overview.md) | Visión rápida del MVP |
| [Mapa de contenido](DesignAgent/Salvo-MOC.md) | Navegación de toda la documentación |
| [Guía de arranque](DesignAgent/Salvo-Getting-Started.md) | Entorno y forma de trabajo |
| [Entrevista con Koin](DesignAgent/Salvo-Interview-Prep.md) | Pitch, preguntas y límites honestos |
| [Portabilidad](DesignAgent/Salvo-Portability.md) | Separación de agentes y proveedores |
| [Instrucciones para Codex](AGENTS.md) | Reglas permanentes de implementación |
| [Kit para Claude](ClaudeAgent/README.md) | Contexto, workflow y plantillas de Claude |
| [Coordinación](Coordination/README.md) | Trabajo paralelo, ownership y handoffs |
| [Plantilla de tarea](Coordination/Task-Brief-Template.md) | Resultado, alcance, permisos y verificación para Codex o Claude |

El MVP funciona con datos sintéticos y proveedores mock. Anthropic es el proveedor de IA previsto
para una etapa posterior; una integración real con el sandbox de Koin requiere onboarding y no forma
parte del núcleo inicial.
