# Salvo — Guía de arranque

> Estado del documento: vigente
> Última actualización: 2026-09-08
> Fuente de verdad: [[Salvo-Blueprint]]
> Seguimiento: [[Salvo-Progress]] · Navegación: [[Salvo-MOC]]

## Estado actual

- Etapa 0 documental completada.
- Etapa 1 integrada mediante PR #1, cerrada mediante PR #2 y verificada sobre `main` en `66f0949`.
- Etapas 2 a 8 integradas y verificadas con la compuerta full-stack y, desde la Etapa 5, con
  `scripts/smoke-ui.sh` sobre el estado integrado.
- Etapa 9 completada: el MVP quedó cerrado con las nueve etapas verificadas.
- Etapa 10 en ejecución, la primera fuera del núcleo local: la instancia pública. El estado vigente
  y los propietarios están en `../Coordination/Workboard.md`; ninguna etapa comienza sin
  autorización explícita.

## Herramientas

### Disponibles

- Node.js 24.20.0 mediante nvm.
- npm/npx 11.19.0.
- SDK .NET 10.0.400, fijado mediante `global.json`.
- Git 2.32.0.
- Docker Desktop 29.7.2, instalado en la Etapa 10 para construir y medir el contenedor. No hizo
  falta en ninguna de las nueve etapas del MVP. En macOS su binario vive en `~/.docker/bin/docker`,
  que no siempre está en el `PATH` de un shell no interactivo.

### Pendientes o no detectadas

- El comando `code` no está disponible; cualquier editor compatible sirve.

## Cuentas y credenciales

El núcleo local no necesita cuentas externas.

- Anthropic: decisión aparte, todavía no tomada. La Etapa 7 cerró con una plantilla determinista, y
  hoy `AI_PROVIDER=anthropic` hace fallar el arranque con o sin clave.
- Koin: el sandbox requiere onboarding, private key y `org_id`; no bloquea el MVP. `KOIN_MODE=sandbox`
  también hace fallar el arranque, a propósito.
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
7. Explicabilidad determinista; Anthropic solo tras aprobación, que no se pidió.
8. El argumento del proyecto: README, diagramas, capturas y guion de demo.
9. Corpus, idiomas y cierre: fixture enriquecida, señales estructuradas, portugués y accesibilidad.
10. La instancia pública: el contenedor y su medición, la instancia compartida que se reinicia, y la
    publicación.

## Trabajo con Codex y Claude

- El estado integrado vive en [[Salvo-Progress]].
- Las tareas y propietarios viven en `../Coordination/Workboard.md`.
- Codex y Claude reciben el mismo formato de tarea desde
  `../Coordination/Task-Brief-Template.md`.
- Cada agente trabaja en su propia rama/worktree y escribe su handoff separado.
- El coordinador actualiza el estado canónico después del merge y la verificación conjunta.
- Serializar lockfiles, migraciones y configuración central salvo que exista una partición segura.
- Consultar `../Coordination/README.md` antes de abrir trabajo paralelo.

El protocolo paralelo existe y todavía no se usó: Codex construyó las Etapas 0 a 3 y Claude las 4 en
adelante, siempre en secuencia.

## Preparación local

```bash
nvm use
npm ci --prefix frontend
```

El comando `dotnet` debe estar disponible en `PATH`; `global.json` rechazará un SDK distinto de
10.0.400.

### Un empujón con capturas adentro no entra en el buffer por defecto

Un `git push` que lleve las seis capturas regeneradas mueve varios megabytes en un solo envío y
falla con `HTTP 400 curl 22 The requested URL returned error: 400`, que no dice nada sobre el
tamaño. El buffer de `git` para HTTP es de 1 MiB por defecto y el empujón lo pasa. Se arregla una
vez por clon:

```bash
git config http.postBuffer 524288000
```

Es configuración local del repositorio, no del sistema, y no cambia nada más. Conviene ponerla
antes del primer empujón que incluya capturas, porque el mensaje de error no sugiere la causa.

## Comandos disponibles

| Comando | Propósito |
| --- | --- |
| `dotnet build Salvo.slnx --configuration Release` | Compilar backend con warnings como errores |
| `dotnet test Salvo.slnx --configuration Release` | Tests unitarios y de integración backend |
| `dotnet tool restore` | Restaurar `dotnet-ef` 10.0.11 desde el manifest local |
| `dotnet ef database update --project backend/src/Salvo.Infrastructure --startup-project backend/src/Salvo.Api` | Aplicar migraciones a la DB local configurada |
| `dotnet run --project backend/src/Salvo.Api` | API local en el perfil de desarrollo |
| `npm run dev --prefix frontend` | Frontend local |
| `npm run api:capture --prefix frontend` | Recapturar el documento OpenAPI desde la API |
| `npm run api:types --prefix frontend` | Regenerar los tipos TypeScript desde el documento capturado |
| `npm run api:types:check --prefix frontend` | Comprobar que los tipos generados no derivaron |
| `npm run check --prefix frontend` | Los tipos generados, typecheck, ESLint y tests UI, en ese orden |
| `npm run build --prefix frontend` | Build de producción con Webpack |
| `./scripts/check-docs.sh` | Comprobar que cada ruta, enlace y test que cita el README existe |
| `./scripts/check.sh` | Compuerta completa backend + frontend |
| `./scripts/smoke-ui.sh` | Recorrido de la consola en seis escenarios, con procesos reales |
| `./scripts/contenedor.sh` | Construir, correr y medir la imagen con los dos procesos |

`npm run check` empieza por `api:types:check`, no por el typecheck: un contrato que derivó invalida
todo lo que viene después. `./scripts/smoke-ui.sh` no forma parte de la compuerta —cuesta compilar
las dos toolchains y arrancar dos procesos— y se ejecuta tras integrar cada etapa.

**No hay proxy del navegador hacia la API, y desde la Etapa 10 tampoco hay un rewrite que lo
finja.** La consola habla con la API desde el proceso de Node, con URL absoluta tomada de
`SALVO_API_BASE_URL`; el navegador nunca la alcanza. Hasta la Etapa 10 `next.config.ts` reescribía
`/api/:path*` hacia la API, y esa regla no atendía ninguna petición de la consola mientras
publicaba el contrato entero —sembrado y scoring incluidos— a un `/api` de distancia del puerto
público. Se borró.

La API no aplica migraciones ni carga demo automáticamente **en ningún modo de desarrollo**. En
desarrollo, después de migrar la DB, `POST /api/demo-data/seed` carga la fixture fija e idempotente.
`POST /api/order-imports` recibe `multipart/form-data` con `file` y `format=CSV|JSON`. Si una DB
local anterior a E2 solo contiene el checkpoint de fundación, debe apartarse o eliminarse de forma
explícita por el desarrollador antes de aplicar la primera migración; la aplicación nunca la borra.

La única excepción es el contenedor, y está detrás de una bandera con nombre:
`Database__MigrateOnStartup=true` hace que la API aplique las migraciones pendientes antes de
atender la primera petición. Vale `false` en todos los demás lados. Migrar al arrancar es correcto
para un contenedor que estrena una base vacía y discutible para una máquina que guarda datos, así
que la diferencia se declara en vez de heredarse.

## El contenedor

Salvo entero —consola y API— en una imagen, con la forma de un tier gratuito: un puerto público, la
API en `127.0.0.1`, y la base creada al arrancar.

```bash
./scripts/contenedor.sh construir   # la imagen
./scripts/contenedor.sh correr      # la levanta con 512 MB y espera a que responda
./scripts/contenedor.sh medir       # camino frío y camino tibio, cronometrados
./scripts/contenedor.sh parar
```

Necesita Docker. Si `docker` no está en el `PATH`, el script lo busca en `~/.docker/bin`, que es
donde lo deja Docker Desktop en macOS.

Tres propiedades de la imagen que conviene conocer antes de tocarla:

- **`tzdata` no es opcional.** `RuleConfig` resuelve `America/Montevideo` en su constructor estático
  y `Program.Main` valida las configuraciones de reglas antes de construir el host, así que una
  imagen base sin husos horarios no arranca. `mcr.microsoft.com/dotnet/aspnet:10.0` los trae; una
  variante `alpine` o `-chiseled` habría que comprobarla.
- **Si muere uno de los dos procesos, muere el contenedor**, y con código distinto de cero. Sin esa
  regla, una API caída deja la consola respondiendo `200` en todas sus rutas —con el aviso de que no
  se pudo contactar a la API, que es lo que corresponde— y una sonda que mire solo el puerto público
  ve una instancia sana. Desde `E10B` hay además una sonda propia en `/health`, que mira **los dos**
  procesos y no solo el que atiende el puerto.
- **El reinicio deliberado y la caída se distinguen por el código de salida.** El reinicio por
  antigüedad termina con **75**; una caída, con lo que haya fallado; un `SIGTERM` de afuera, con 0.
  Un contenedor que se reinicia solo y otro que se murió no se pueden ver iguales desde la
  plataforma.
- **`./scripts/contenedor.sh` no borra nada**: ni imágenes, ni contenedores. Los deja y los nombra
  al terminar, igual que `demo.sh` deja sus bases.

Desde `E10B` la imagen trae además lo que hace publicable a una instancia compartida: la base
sembrada **horneada adentro** —de ahí que arranque con los datos ya puestos en 41 s a 0,1 vCPU, con
103 MiB de 512—, el reinicio periódico por antigüedad, el cartel que avisa que es compartida y
efímera, el limitador de tasa en la capa de Next y el tope de pedidos en la API. Lo único que falta
es elegir dónde publicarla, que es `E10C`.

## Restricciones operativas

- La app sin autenticación permanece local.
- Tests y demo usan proveedores mock y no hacen red.
- No se usan versiones NuGet flotantes ni `@latest`; se fijan tras los smoke tests de la Etapa 1.
- No se mueve ni elimina `DesignAgent/` para ejecutar `create-next-app`.
- No se activa Koin sandbox ni Anthropic sin aprobación y credenciales server-side.
