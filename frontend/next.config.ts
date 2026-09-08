import type { NextConfig } from "next";

/**
 * La configuración de la consola, y lo que ya no está en ella.
 *
 * <h3>Por qué no hay un rewrite de `/api`</h3>
 *
 * Hasta la Etapa 10 este archivo reescribía `/api/:path*` hacia la API. Se borró, y conviene decir
 * qué hacía exactamente, porque el motivo por el que se fue no es el que uno supone.
 *
 * No era un puente que la consola usara: **no lo usaba nadie**. Toda lectura y toda escritura salen
 * de `server-client.ts` con URL absoluta desde el proceso de Node, porque una ruta relativa dentro
 * de un componente de servidor es un `TypeError`. El rewrite quedó de la Etapa 1, cuando una sonda
 * de salud del navegador lo atravesaba; esa sonda se borró en `E5C` y el rewrite le sobrevivió sin
 * que nada lo llamara.
 *
 * Lo que sí hacía era publicar la API entera. `source: "/api/:path*"` con destino `${api}/:path*`
 * es un proxy transparente: sin lista de rutas, sin método y sin cabecera. Y como **quita** el
 * prefijo, la ruta que llegaba no era la que uno prueba por reflejo: `/api/dashboard` iba a
 * `${api}/dashboard`, que no existe porque toda ruta de la API empieza con `/api/`. La que llegaba
 * era la del prefijo doblado, `/api/api/dashboard`. Medido antes de borrarlo, contra el puerto
 * público: el dashboard respondía 200, el documento OpenAPI entero se servía en
 * `/api/openapi/v1.json`, y `POST /api/api/demo-data/seed` sembraba 300 pedidos sin autenticación
 * de ninguna clase.
 *
 * Por eso el 404 de `/api/dashboard` nunca fue prueba de nada: da 404 con el rewrite y sin él.
 *
 * Sin este rewrite, que la API no sea alcanzable desde afuera pasa a ser cierto por topología: en
 * el contenedor escucha en `127.0.0.1` y nada la reexpone. Si algún día hace falta un puente —una
 * sonda externa que refleje a la API, por ejemplo— se acota a esa ruta y a ninguna más, con el
 * motivo escrito.
 */
const nextConfig: NextConfig = {
  experimental: {
    taint: true,
  },
};

export default nextConfig;
