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
- Modelo y esfuerzo acordados: **Sonnet 4.5 · `high`**. Son tres cadenas y el test dorado que las
  fija. No hay invariantes nuevas, ni migración, ni contrato, ni frontera que mover. Sería el primer
  trabajo del proyecto corrido con Sonnet: hasta acá todos fueron `Opus 5 · high`, y se deja anotado
  para que el precedente quede escrito y no se cite de memoria.
- Dependencias: ninguna. Etapa 7 ya integrada y verificada.

## Resultado esperado

El bloque de explicación se lee sin repeticiones y sin asperezas: el aviso posterior a generar dice
qué pasó en vez de repetir la leyenda que ya está en pantalla, la plantilla no escribe decimales que
valen cero, y las reglas «se disparan» en vez de «coincidir». Nada del motor, del grounding, del
ciclo de vida ni del contrato cambia.

## Contexto obligatorio

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

**2. La plantilla no escribe decimales que valen cero.** «56,0 veces» pasa a «56 veces»; «23,2
veces» queda como está. Es una condición sobre la parte fraccionaria, no un redondeo: una razón de
23,2 no puede convertirse en 23.

**3. Las reglas se disparan.** «Coincidieron 4 reglas» pasa a «Se dispararon 4 reglas», o a la forma
equivalente que suene mejor en el conjunto de la frase.

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
- [ ] Un caso con parte fraccionaria distinta de cero conserva su decimal. Si el corpus no tiene uno
      en el pedido del test dorado, agregar un caso que lo cubra.
- [ ] Ninguna cifra del texto quedó escrita en letras. Documentar cómo se comprobó.
- [ ] El resumen sigue pasando la validación de grounding en todas las alertas del corpus demo: el
      test que ya lo afirma sigue verde sin tocarlo.
- [ ] `/gate` en verde y `./scripts/smoke-ui.sh` verde.
- [ ] Handoff generado con `/handoff E7C-PULIDO-EXPLICACION`.

## Verificación y evidencia

| Comando/comprobación | Resultado esperado |
| --- | --- |
| `/brief-check Coordination/Tasks/E7C-PULIDO-EXPLICACION.md` | Brief válido |
| Test dorado actualizado | Texto exacto, con «56 veces» y «se dispararon» |
| Caso con decimal distinto de cero | Conserva su decimal |
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

## Detenerse y consultar si

- un retoque obliga a tocar el conjunto de hechos, el tokenizador o la validación;
- quitar el decimal hace que alguna cifra deje de estar fundamentada;
- hace falta salir de los paths autorizados;
- el test dorado no puede seguir fijando el texto exacto.

## Entrega requerida

- Resumen del resultado y archivos modificados.
- El texto del resumen antes y después, para el mismo pedido.
- Comandos y resultados exactos.
- Estado: `Lista para integrar | Parcial | Bloqueada`.
- Handoff en `Coordination/Handoffs/Claude.md`.
