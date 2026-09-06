# Salvo — Task brief `E7C-PULIDO-EXPLICACION`

## Identificación

- Work ID: `E7C-PULIDO-EXPLICACION`
- Etapa: 7
- Tipo: `implementación`
- Propietario: `Claude`
- Coordinador: Unai Arismendes
- Fecha: 2026-09-06
- Rama/worktree: `claude/e7c-pulido-explicacion`
- Commit base: `0e3faf1`, el `HEAD` de `main` que cierra la Etapa 7
- Modelo y esfuerzo acordados: **Opus 5 · `high`**. El coordinador recomendó `Sonnet 5 · high`
  —tres cadenas y un test dorado que ya existe, sin invariantes nuevas, sin migración, sin
  contrato— y el usuario optó por Opus, como en `E5C` y `E7B`. Sigue sin haber en el proyecto
  ningún trabajo corrido con Sonnet, y así queda escrito para que el precedente no se cite de
  memoria. (La recomendación decía «Sonnet 4.5», modelo que no existe en
  `ClaudeAgent/Claude-Model-Policy.md`; la fila real es `Sonnet 5`.)
- Dependencias: ninguna. Etapa 7 ya integrada y verificada.

## Resultado esperado

El bloque de explicación se lee sin repeticiones y sin asperezas: el aviso posterior a generar dice
qué pasó en vez de repetir la leyenda que ya está en pantalla, la plantilla no escribe decimales que
valen cero, y las reglas «se disparan» en vez de «coincidir». Nada del motor, del grounding, del
ciclo de vida ni del contrato cambia.

## Contexto obligatorio

- `DesignAgent/Salvo-Blueprint.md`, **§4.7 Explicabilidad**: es la sección canónica que sostiene la
  restricción de este brief —«las cifras escritas en letras no se validan, y se declara que no se
  validan»— y la que define las tres capas de grounding que acá no se tocan.
- `Coordination/Tasks/E7-DISENO.md` (v2), **D4** y **D5**: por qué el texto se verifica sobre la
  salida y qué se puede escribir en él.
- `Coordination/Handoffs/Claude.md`, entradas de `E7A` y `E7B`.
- `DesignAgent/Salvo-Progress.md`, checklist «Etapa 7 — Explicabilidad», ya cerrada salvo Anthropic.
- Código:
  - `frontend/src/app/alerts/[id]/explanation-action.ts:50-51`, el aviso a corregir;
  - `frontend/src/app/alerts/[id]/review-action.ts:74-75`, el molde de un aviso que dice qué pasó;
  - `frontend/src/app/alerts/[id]/explanation-block.tsx:120-124`, la leyenda que el aviso repite;
  - `backend/src/Salvo.Infrastructure/Explanations/DeterministicExplanationProvider.cs`, la plantilla;
  - `backend/src/Salvo.Domain/Explanations/SpanishNumberFormat.cs`, el formateo;
  - `backend/tests/Salvo.Api.IntegrationTests/ExplanationGoldenTests.cs`, el test que fija el texto.

**Antes de declarar pendiente cualquier cosa del estado canónico, verificarla contra el archivo.**

## Alcance

### Dentro

**1. El aviso posterior a generar dice qué pasó.** Hoy su cuerpo es «Cada cifra y cada regla del
texto se verificaron contra la evaluación antes de guardarlo», que es casi literalmente la leyenda
que `explanation-block.tsx` ya muestra bajo el resumen. Reescribirlo con el molde de
`review-action.ts:74-75`: qué quedó guardado y qué implica. La leyenda del bloque **no** se toca:
ahí la frase es correcta y es donde corresponde que viva.

**2. La plantilla no escribe decimales que valen cero.** Una razón de `15.0` se escribe «15 veces»;
una de `23.2` sigue siendo «23,2 veces». Es una condición sobre la parte fraccionaria, no un
redondeo: 23,2 no puede convertirse en 23.

Los ejemplos de este brief se tomaron de `ORD_900004`, que es una importación manual de la base
local y **no** sirve para el test dorado. En la fixture, la razón con parte fraccionaria cero la
tiene **`ORD_000171`** (`15.0`); el dorado actual usa `ORD_000011` (`23.2`, tres reglas), que cubre
el otro caso.

**3. Las reglas se disparan.** «Coincidieron N reglas» pasa a «Se dispararon N reglas», o a la forma
equivalente que suene mejor en el conjunto de la frase. En el dorado actual la frase es
«Coincidieron 3 reglas».

**El número sigue en dígitos, siempre.** Escribir «cuatro» se lee mejor y saca esa cifra de la
verificación en silencio, porque las cifras en letras no se validan y así está declarado. Ninguno de
los tres retoques puede convertir un dígito en palabra.

### Fuera

- El conjunto de hechos, el tokenizador, la regla de igualdad y las tres capas de grounding.
- El ciclo de vida, los endpoints, el contrato y el esquema.
- Los nombres de país por sus códigos —`ES` en vez de «España»—: es deseable y le toca a la
  candidata de internacionalización de la Etapa 8, junto con el resto del texto.
- `messages.ts`, `format.ts` y los rótulos de los códigos de fallo.
- Cualquier cambio en el motor o en `RuleConfig`.

### Paths autorizados

- `backend/src/Salvo.Infrastructure/Explanations/**`
- `backend/src/Salvo.Domain/Explanations/**`, solo si el formateo del decimal vive ahí
- `backend/tests/**`
- `frontend/src/app/alerts/[id]/explanation-action.ts` y sus tests
- `scripts/smoke-ui.sh`, solo si alguna comprobación fija una de las cadenas cambiadas

### Paths reservados por otros trabajos

- Ninguno.

## Acciones autorizadas

- Ediciones locales en los paths autorizados.
- Dependencias nuevas: **no**. Migraciones: **no**.
- Escrituras externas: ninguna. No `git push`, no PR.
- Commits locales en la rama: autorizados.

## Criterios de aceptación

- [ ] `/brief-check Coordination/Tasks/E7C-PULIDO-EXPLICACION.md` sin faltantes antes de empezar.
- [ ] El aviso posterior a generar no comparte ninguna oración con la leyenda del bloque.
- [ ] El test dorado se actualiza y sigue fijando el texto **exacto**, no una parte.
- [ ] **El test dorado fija dos textos exactos**, uno por cada rama del cambio: `ORD_000011`, cuya
      razón `23.2` conserva el decimal, y `ORD_000171`, cuya razón `15.0` pasa a escribirse sin él.
      El segundo es el que la modificación afecta y hoy no está cubierto.
- [ ] Ninguna cifra del texto quedó escrita en letras. Documentar cómo se comprobó.
- [ ] El resumen sigue pasando la validación de grounding en todas las alertas del corpus demo: el
      test que ya lo afirma sigue verde sin tocarlo.
- [ ] `/gate` en verde y `./scripts/smoke-ui.sh` verde.
- [ ] Handoff generado con `/handoff E7C-PULIDO-EXPLICACION`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E7C-PULIDO-EXPLICACION.md` | Brief válido |
| Test dorado, `ORD_000171` | Texto exacto; «15 veces» sin decimal, y «se dispararon» |
| Test dorado, `ORD_000011` | Texto exacto; «23,2 veces» conserva el decimal |
| Test de grounding sobre todo el corpus | Verde sin modificarlo |
| `/gate` | Compuerta full-stack verde |
| `./scripts/smoke-ui.sh` | Verde |
| `git status --porcelain` | Solo paths autorizados |
| `/handoff E7C-PULIDO-EXPLICACION` | Entrada agregada a `Coordination/Handoffs/Claude.md` |

## Decisiones delegadas

- La redacción exacta del aviso, respetando que diga qué pasó y no repita la leyenda.
- El verbo definitivo para las reglas, y si conviene retocar «el score se limita a 100» por una
  forma más natural en la misma oración.
- Dónde vive la condición sobre la parte fraccionaria: el formateador o la plantilla.
- Si los dos textos dorados van en el mismo test con dos casos o en dos tests hermanos, siempre que
  los dos fijen el texto completo y no un fragmento.

## Detenerse y consultar si

- un retoque obliga a tocar el conjunto de hechos, el tokenizador o la validación;
- quitar el decimal hace que alguna cifra deje de estar fundamentada;
- hace falta salir de los paths autorizados;
- el test dorado no puede seguir fijando el texto exacto;
- `ORD_000171` no resulta ser el caso de parte fraccionaria cero que este brief afirma que es.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- El texto del resumen antes y después, para el mismo pedido.
- Comandos y resultados exactos.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
