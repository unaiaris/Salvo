# Salvo

Consola antifraude B2B de portfolio para e-commerce, diseñada alrededor de reglas deterministas,
alertas auditables e integración desacoplada con proveedores externos.

Estado: cinco etapas integradas y verificadas. El sistema importa pedidos, los puntúa con seis
reglas deterministas sobre historia estrictamente anterior, persiste evaluaciones idempotentes, abre
alertas con escalada por banda, permite revisarlas con veredicto terminal y expone un dashboard
operativo — todo con interfaz. Las Etapas 6 a 8 —proveedor externo, explicabilidad y portfolio—
están pendientes.

## Levantar la consola

Con .NET 10.0.400, Node.js 24.20.0 y npm 11.19.0 en `PATH`. Hacen falta **tres terminales**: las dos
primeras quedan ocupadas mientras los procesos corren.

**Terminal A — la API**, en `http://127.0.0.1:5100`:

```bash
cd backend/src/Salvo.Api
dotnet run
```

Esperar `Now listening on: http://127.0.0.1:5100`. Si responde *address already in use*, hay otra
instancia viva: `kill $(lsof -ti tcp:5100)` y repetir.

**Terminal B — la consola**, en `http://localhost:3000`:

```bash
npm run dev --prefix frontend
```

**Terminal C — libre**, para `curl`, git y todo lo demás.

Después, en el navegador:

| Ruta | Qué muestra |
| --- | --- |
| `http://localhost:3000/import` | Cargar el corpus demo o importar un archivo, y **ejecutar la corrida de scoring** |
| `http://localhost:3000/alerts` | Cola de alertas abiertas, de mayor a menor score vigente |
| `http://localhost:3000/dashboard` | Estado operativo de la corrida vigente |

**La primera vez, o sobre una base vacía, el orden importa**: cargar el corpus demo y ejecutar la
corrida desde `/import`. Sin corrida no hay evaluaciones ni alertas, y el feed y el dashboard lo
dicen explícitamente.

`dotnet run` usa el entorno `Development`, donde `DemoData:Enabled` es `true`: por eso aparecen el
botón de carga demo y la sección de calidad del criterio.

Para poblar la base sin la interfaz:

```bash
curl -s -X POST http://127.0.0.1:5100/api/demo-data/seed
curl -s -X POST http://127.0.0.1:5100/api/risk-evaluations:run
```

## Verificación local

Con .NET 10.0.400, Node.js 24.20.0 y npm 11.19.0 disponibles en `PATH`:

```bash
npm ci --prefix frontend
./scripts/check.sh
```

La compuerta restaura NuGet y el tooling local, verifica que los tipos generados desde OpenAPI estén
al día, compila y prueba el backend en Release, comprueba que el modelo EF no tenga cambios
pendientes y ejecuta typecheck, ESLint, Vitest y el build de producción del frontend.

Y el recorrido completo de la interfaz, que la compuerta no cubre:

```bash
./scripts/smoke-ui.sh
```

Levanta la API y la consola en sus propios puertos —5199 y 3199, para no chocar con los de
desarrollo— sobre bases temporales propias, y comprueba las cuatro rutas en tres escenarios: con
datos, con la base vacía y con la API apagada. Libera procesos y puertos al terminar, también si
falla. Se ejecuta tras integrar cada etapa.

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
| [Portabilidad](DesignAgent/Salvo-Portability.md) | Separación de agentes y proveedores |
| [Instrucciones para Codex](AGENTS.md) | Reglas permanentes de implementación |
| [Kit para Claude](ClaudeAgent/README.md) | Contexto, workflow y plantillas de Claude |
| [Coordinación](Coordination/README.md) | Trabajo paralelo, ownership y handoffs |
| [Plantilla de tarea](Coordination/Task-Brief-Template.md) | Resultado, alcance, permisos y verificación para Codex o Claude |

El MVP funciona con datos sintéticos y proveedores mock. Anthropic es el proveedor de IA previsto
para una etapa posterior; una integración real con el sandbox de Koin requiere onboarding y no forma
parte del núcleo inicial.
