# Salvo — Guía de arranque

> Estado del documento: vigente
> Última actualización: 2026-08-31
> Fuente de verdad: [[Salvo-Blueprint]]
> Seguimiento: [[Salvo-Progress]] · Navegación: [[Salvo-MOC]]

## Estado actual

- Etapa 0 documental completada.
- Etapa 1 implementada en `codex/e1-foundation` y lista para integrar.
- La Etapa 1 no se considera integrada hasta hacer merge y repetir la compuerta conjunta.
- La Etapa 2 continúa pendiente y no se inicia automáticamente.

## Herramientas

### Disponibles

- Node.js 24.20.0 mediante nvm.
- npm/npx 11.19.0.
- SDK .NET 10.0.400, fijado mediante `global.json`.
- Git 2.32.0.

### Pendientes o no detectadas

- Docker no está instalado; no se necesita para el MVP.
- El comando `code` no está disponible; cualquier editor compatible sirve.

## Cuentas y credenciales

El núcleo local no necesita cuentas externas.

- Anthropic: se configurará más adelante si se aprueba la explicación real.
- Koin: el sandbox requiere onboarding, private key y `org_id`; no bloquea el MVP.
- Ninguna clave se copia en chats, documentación, fixtures o Git.

## Flujo por etapa

1. Leer `AGENTS.md` y la etapa correspondiente del Blueprint.
2. Completar un brief basado en `../Coordination/Task-Brief-Template.md` con resultado, alcance,
   autorizaciones, paths, aceptación y verificación.
3. Marcar la etapa `En curso` en [[Salvo-Progress]] solo si la tarea asigna esa coordinación.
4. Implementar un solo concern pequeño.
5. Ejecutar los tests afectados y las compuertas .NET/npm cuando existan.
6. No avanzar si la compuerta está roja.
7. Registrar evidencia y riesgos restantes en el tracker o handoff correspondiente.
8. Registrar decisiones nuevas en la bitácora.
9. Hacer commits pequeños con mensajes descriptivos.

## Orden de construcción

1. Fundaciones: solución ASP.NET Core, frontend Next.js, versiones, OpenAPI, tests y DB.
2. Contrato y seed.
3. Baseline, reglas y score.
4. Alertas y revisión.
5. UI y dashboard.
6. Proveedor antifraude mock y callback.
7. Explicabilidad; Anthropic solo tras aprobación.
8. Métricas finales y README.

## Trabajo con Codex y Claude

- El estado integrado vive en [[Salvo-Progress]].
- Las tareas y propietarios viven en `../Coordination/Workboard.md`.
- Codex y Claude reciben el mismo formato de tarea desde
  `../Coordination/Task-Brief-Template.md`.
- Cada agente trabaja en su propia rama/worktree y escribe su handoff separado.
- El coordinador actualiza el estado canónico después del merge y la verificación conjunta.
- La Etapa 1 debe comenzar de forma secuencial porque concentra solución, paquetes, lockfile,
  EF Core, OpenAPI y configuración.
- Consultar `../Coordination/README.md` antes de abrir trabajo paralelo.

## Preparación local

```bash
nvm use
npm ci --prefix frontend
```

El comando `dotnet` debe estar disponible en `PATH`; `global.json` rechazará un SDK distinto de
10.0.400.

## Comandos disponibles

| Comando | Propósito |
| --- | --- |
| `dotnet build Salvo.slnx --configuration Release` | Compilar backend con warnings como errores |
| `dotnet test Salvo.slnx --configuration Release` | Tests unitarios y de integración backend |
| `dotnet run --project backend/src/Salvo.Api` | API local en el perfil de desarrollo |
| `npm run dev --prefix frontend` | Frontend local |
| `npm run check --prefix frontend` | Typecheck, ESLint y tests UI |
| `npm run build --prefix frontend` | Build de producción con Webpack |
| `./scripts/check.sh` | Compuerta completa backend + frontend |

Para probar el proxy local, arrancar la API en `http://127.0.0.1:5100` y después el frontend. La
ruta `/api/health` del frontend se reescribe al endpoint `/health` de la API mediante
`SALVO_API_BASE_URL`.

## Restricciones operativas

- La app sin autenticación permanece local.
- Tests y demo usan proveedores mock y no hacen red.
- No se usan versiones NuGet flotantes ni `@latest`; se fijan tras los smoke tests de la Etapa 1.
- No se mueve ni elimina `DesignAgent/` para ejecutar `create-next-app`.
- No se activa Koin sandbox ni Anthropic sin aprobación y credenciales server-side.
