# Salvo — la consola y su API en un solo contenedor.
#
# Un servicio, un puerto público, dos procesos: `server.js` de Next escuchando donde la plataforma
# diga, y la API de ASP.NET Core escuchando en 127.0.0.1 y nada más. El navegador nunca habla con
# la API: la consola la consulta desde el proceso de Node con URL absoluta a loopback. Desde la
# Etapa 10 no hay ningún rewrite que la reexponga, así que «la API no es alcanzable desde afuera»
# es cierto por topología y no por configuración.
#
# Las versiones no se eligen acá: las clava el repositorio y esta imagen las obedece.
#
#   - `global.json` fija el SDK en 10.0.400 con `rollForward: disable`. La imagen
#     `mcr.microsoft.com/dotnet/sdk:10.0` trae exactamente 10.0.400 — comprobado con
#     `dotnet --list-sdks` dentro del contenedor—, así que el build no arranca con otro.
#   - `frontend/package.json` fija Node en 24.20.0 y npm en 11.19.0. Se baja el tarball oficial de
#     esa versión exacta en vez de usar una imagen `node:24` que se mueve sola.
#
# La imagen final es `mcr.microsoft.com/dotnet/aspnet:10.0`, que es Ubuntu 24.04 y **ya trae
# `tzdata`**: `/usr/share/zoneinfo/America/Montevideo` existe en ella. No es un detalle cosmético.
# `RuleConfig` resuelve esa zona en su constructor estático y `Program.Main` valida las
# configuraciones de reglas antes de construir el host, así que una base sin husos horarios —una
# variante `alpine` o `-chiseled`, por ejemplo— no arranca, y falla en un lugar donde el error no se
# lee como lo que es. Queda dicho para quien cambie la base.

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0
ARG NODE_VERSION=24.20.0

# ---------------------------------------------------------------- Node 24.20.0, de la fuente

# Se baja el tarball oficial y no se usa `node:24-slim` porque esa etiqueta cambia de versión sin
# avisar, y `package.json` fija `"node": "24.20.0"`. `.tar.gz` y no `.tar.xz`: la imagen del SDK
# trae `tar` y `curl`, pero no `xz`.
FROM ${DOTNET_SDK_IMAGE} AS node-dist
ARG NODE_VERSION
ARG TARGETARCH
RUN set -eux; \
    case "${TARGETARCH}" in \
      amd64) node_arch=x64 ;; \
      arm64) node_arch=arm64 ;; \
      *) echo "arquitectura no contemplada: ${TARGETARCH}" >&2; exit 1 ;; \
    esac; \
    curl -fsSL -o /tmp/node.tar.gz \
      "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-${node_arch}.tar.gz"; \
    mkdir -p /opt/node; \
    tar -xzf /tmp/node.tar.gz -C /opt/node --strip-components=1; \
    rm -f /tmp/node.tar.gz; \
    test "v${NODE_VERSION}" = "$(/opt/node/bin/node --version)"

# ---------------------------------------------------------------- la API

# Se publica sobre el runtime de ASP.NET Core y **no** autocontenida ni recortada. El motivo es
# EF Core: el recorte quita por análisis estático lo que solo se alcanza por reflexión, que es
# exactamente como EF Core construye su modelo, y este repositorio compila con los analizadores
# encendidos y los warnings como errores. Cambiar eso para ahorrar imagen sería pagar riesgo de
# ejecución con moneda de disco. La imagen de runtime ya trae el framework compartido.
#
# Lo que sí se hace es **ReadyToRun**: precompilar la aplicación a código nativo para el destino, en
# vez de dejar que el JIT la compile en el primer arranque. No cambia una línea de código ni un
# comportamiento; cambia cuánto trabajo de CPU hay entre el `docker run` y el primer `200`. Con
# 0,1 vCPU eso deja de ser un detalle: medido en esta máquina, arrancar y migrar una base vacía
# tarda 103 s sin R2R, y el umbral que hereda esta tarea es 90 s. El costo es una compilación más
# larga y una imagen más grande, y los dos números están en el handoff.
FROM ${DOTNET_SDK_IMAGE} AS backend
ARG TARGETARCH
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY backend/src/Salvo.Domain/Salvo.Domain.csproj backend/src/Salvo.Domain/
COPY backend/src/Salvo.Domain/packages.lock.json backend/src/Salvo.Domain/
COPY backend/src/Salvo.Application/Salvo.Application.csproj backend/src/Salvo.Application/
COPY backend/src/Salvo.Application/packages.lock.json backend/src/Salvo.Application/
COPY backend/src/Salvo.Infrastructure/Salvo.Infrastructure.csproj backend/src/Salvo.Infrastructure/
COPY backend/src/Salvo.Infrastructure/packages.lock.json backend/src/Salvo.Infrastructure/
COPY backend/src/Salvo.Api/Salvo.Api.csproj backend/src/Salvo.Api/
COPY backend/src/Salvo.Api/packages.lock.json backend/src/Salvo.Api/
# `--locked-mode` por la misma razón que `scripts/check.sh`: un lockfile que no cuadra con el
# proyecto es un fallo, no algo que se resuelva solo bajando otra versión.
RUN dotnet restore backend/src/Salvo.Api/Salvo.Api.csproj --locked-mode
COPY backend/ backend/
# `--runtime` es obligatorio para ReadyToRun —hay que saber para qué máquina se precompila— y
# `--self-contained false` mantiene el framework compartido de la imagen de runtime, que es lo que
# evita cargar una copia entera de .NET dentro de la imagen. El restore se repite acotado al RID
# porque el anterior fue independiente de plataforma.
RUN dotnet publish backend/src/Salvo.Api/Salvo.Api.csproj \
      --configuration Release \
      --runtime "linux-${TARGETARCH}" \
      --self-contained false \
      -p:PublishReadyToRun=true \
      --output /app/api

# ---------------------------------------------------------------- la base, ya sembrada

# Sembrar y puntuar cuestan 24,68 s y 17,50 s a 0,1 vCPU, medidos en `E10A`. Hacerlo al arrancar
# pondría al primer visitante a esperar dos minutos frente a una consola vacía; hacerlo acá lo paga
# quien construye, una sola vez, y el arranque solo copia el archivo.
#
# Esta etapa **corre la API** para producir la base, en vez de escribir SQL a mano: el corpus, la
# corrida, el proveedor mock y la plantilla de explicación son los mismos casos de uso que la
# aplicación ejecuta, así que el archivo horneado no puede describir un estado que la aplicación no
# sepa producir. `scripts/hornear-base.sh` dice qué deja montado y afirma las cantidades.
#
# Se hace sobre el SDK y no sobre la imagen de runtime por una razón práctica: Node hace de cliente
# HTTP, y acá ya está a mano. Correr la API publicada para `linux-${TARGETARCH}` en esta etapa exige
# que la arquitectura de construcción y la de destino coincidan; una construcción cruzada la emula,
# que funciona y tarda más.
FROM ${DOTNET_SDK_IMAGE} AS seeded
COPY --from=node-dist /opt/node /opt/node
ENV PATH=/opt/node/bin:${PATH}
COPY --from=backend /app/api /app/api
COPY scripts/hornear-base.sh /usr/local/bin/hornear-base.sh
RUN chmod +x /usr/local/bin/hornear-base.sh \
    && /usr/local/bin/hornear-base.sh /seed/salvo.db /app/api

# ---------------------------------------------------------------- la consola

FROM ${DOTNET_SDK_IMAGE} AS frontend
COPY --from=node-dist /opt/node /opt/node
ENV PATH=/opt/node/bin:${PATH}
WORKDIR /web
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
# La URL de la API queda clavada en el build porque los componentes de servidor la leen del entorno
# del proceso; el contenedor la vuelve a pasar en ejecución y las dos coinciden.
ENV SALVO_API_BASE_URL=http://127.0.0.1:5100
ENV SALVO_BUILD_STANDALONE=1
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

# ---------------------------------------------------------------- la imagen que corre

FROM ${DOTNET_RUNTIME_IMAGE} AS final

COPY --from=node-dist /opt/node /opt/node
ENV PATH=/opt/node/bin:${PATH}

WORKDIR /app
COPY --from=backend /app/api ./api
# `server.js` mínimo y sus dependencias.
COPY --from=frontend /web/.next/standalone ./web
# `.next/static` va aparte: la documentación de Next 16 dice que la salida autocontenida no lo
# copia, porque en un despliegue con CDN lo sirve otro. Acá lo sirve `server.js`, así que tiene que
# estar donde él lo busca. Este proyecto no tiene `frontend/public`, así que no hay un tercero.
COPY --from=frontend /web/.next/static ./web/.next/static

COPY scripts/contenedor-entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# La base sembrada, tal como la dejó la etapa `seeded`. El punto de entrada la copia a `/data` en
# cada arranque, y **esa copia es el reinicio de la instancia**: no hay cirugía sobre un SQLite que
# la API tiene abierto, porque cuando se copia todavía no hay nadie que lo tenga abierto.
COPY --from=seeded /seed/salvo.db /app/seed/salvo.db

# La base de trabajo vive en un directorio propio para que un volumen se monte ahí sin tapar la
# aplicación. **Esta imagen es efímera por construcción**: lo que haya en `/data` se reemplaza en
# cada arranque, así que montar un volumen ahí no da persistencia.
RUN mkdir -p /data && chown -R app:app /data /app
USER app

ENV ASPNETCORE_URLS=http://127.0.0.1:5100 \
    SALVO_API_BASE_URL=http://127.0.0.1:5100 \
    ConnectionStrings__SalvoDb="Data Source=/data/salvo.db" \
    Database__MigrateOnStartup=true \
    DemoData__Enabled=true \
    DemoData__SeedEnabled=false \
    SALVO_BAKED_DB=/app/seed/salvo.db \
    SALVO_LANGUAGE=es \
    HOSTNAME=0.0.0.0 \
    PORT=3000 \
    NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

# `Database__MigrateOnStartup=true`: el contenedor arranca con un volumen vacío y nadie va a correr
# `dotnet ef database update` por él, porque la imagen no lleva el SDK. Está apagado en todos los
# demás modos de correr esta API, y `Program.MigrateIfAsked` explica por qué la diferencia se
# declara en vez de heredarse.
#
# `Database__MigrateOnStartup=true` **se conserva aunque la base venga horneada y migrada**, y no
# es redundante: es lo que hace que el caso malo sea benigno. Si algún día la copia de la base
# horneada no llegara, la API se encontraría con un archivo vacío; con la migración encendida crea
# el esquema y la consola dice que no hay pedidos, en vez de contestar «no such table» a todo. El
# punto de entrada, además, falla antes si el archivo horneado no está en la imagen.
#
# `DemoData__Enabled=true`: enciende las métricas de calidad y los disparadores del proveedor
# externo, que son la mitad del argumento del proyecto.
#
# `DemoData__SeedEnabled=false`: apaga **solo** la ruta de sembrado, y por eso el interruptor está
# partido en dos. Con la base horneada nadie necesita sembrar acá, y era la única ruta con la que un
# visitante podía dejar la consola inservible para el siguiente: cargar el corpus sobre una base que
# ya tiene esas referencias se rechaza, y cargarlo el doble de grande no. El reinicio acota ese daño
# en el tiempo; quitar la ruta hace que no ocurra.


EXPOSE 3000
ENTRYPOINT ["/app/entrypoint.sh"]
